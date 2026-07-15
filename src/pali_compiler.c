#include "pali_internal.h"

#include <ctype.h>
#include <float.h>
#include <math.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct Parser {
    PaliLexer lexer;
    PaliToken current;
    PaliToken previous;
    PaliDocument *document;
    PaliError *error;
    int expression_depth;
    bool failed;
} Parser;

typedef struct DocumentCompiler {
    const PaliDocument *document;
    PaliProgram *program;
    PaliError *error;
    bool failed;
} DocumentCompiler;

typedef struct TextWriter {
    char *out;
    size_t capacity;
    size_t length;
    PaliError *error;
    bool failed;
} TextWriter;

static bool bounded_string(const char *text, size_t capacity) {
    return text != NULL && memchr(text, '\0', capacity) != NULL;
}

static bool identifier_is_valid(const char *text, size_t capacity) {
    if (!bounded_string(text, capacity) || text[0] == '\0') {
        return false;
    }
    const unsigned char first = (unsigned char)text[0];
    if (!isalpha(first) && first != (unsigned char)'_') {
        return false;
    }
    for (size_t index = 1; text[index] != '\0'; ++index) {
        const unsigned char character = (unsigned char)text[index];
        if (!isalnum(character) && character != (unsigned char)'_') {
            return false;
        }
    }
    return true;
}

static bool document_value_is_valid(PaliValue value) {
    switch (value.type) {
        case PALI_VALUE_NUMBER:
            return isfinite(value.as.number);
        case PALI_VALUE_BOOL:
            return true;
        case PALI_VALUE_TEXT:
            return bounded_string(value.as.text, sizeof(value.as.text)) &&
                   strchr(value.as.text, '\r') == NULL;
        default:
            return false;
    }
}

static bool document_fail(PaliError *error, const char *message) {
    error->line = 1;
    error->column = 1;
    (void)snprintf(error->message, sizeof(error->message), "%s", message);
    return false;
}

static bool document_structure_is_valid(const PaliDocument *document,
                                        PaliError *error) {
    uint16_t expression_references[PALI_MAX_EXPRESSIONS] = {0};
    uint16_t constant_references[PALI_MAX_CONSTANTS] = {0};
    uint16_t name_references[PALI_MAX_DOCUMENT_NAMES] = {0};

    for (uint16_t index = 0; index < document->expression_count; ++index) {
        const PaliExpression *expression = &document->expressions[index];
        switch ((PaliExpressionKind)expression->kind) {
            case PALI_EXPRESSION_LITERAL:
                if (expression->operand >= document->constant_count ||
                    expression->left != PALI_NODE_NONE ||
                    expression->right != PALI_NODE_NONE) {
                    return document_fail(
                        error, "literal expression is structurally invalid");
                }
                constant_references[expression->operand]++;
                break;
            case PALI_EXPRESSION_GET_SELF:
            case PALI_EXPRESSION_GET_ACTOR:
                if (expression->operand >= document->name_count ||
                    expression->left != PALI_NODE_NONE ||
                    expression->right != PALI_NODE_NONE) {
                    return document_fail(
                        error, "property expression is structurally invalid");
                }
                name_references[expression->operand]++;
                break;
            case PALI_EXPRESSION_NEGATE:
                if (expression->left >= index ||
                    expression->right != PALI_NODE_NONE ||
                    expression->operand != 0) {
                    return document_fail(
                        error, "unary expression is structurally invalid");
                }
                expression_references[expression->left]++;
                break;
            case PALI_EXPRESSION_ADD:
            case PALI_EXPRESSION_SUBTRACT:
            case PALI_EXPRESSION_MULTIPLY:
            case PALI_EXPRESSION_DIVIDE:
            case PALI_EXPRESSION_MIN:
            case PALI_EXPRESSION_MAX:
                if (expression->left >= index ||
                    expression->right >= index || expression->operand != 0) {
                    return document_fail(
                        error, "binary expression is structurally invalid");
                }
                expression_references[expression->left]++;
                expression_references[expression->right]++;
                break;
            default:
                return document_fail(error,
                                     "document has an unknown expression kind");
        }
    }

    for (uint16_t index = 0; index < document->statement_count; ++index) {
        const PaliStatement *statement = &document->statements[index];
        switch ((PaliStatementKind)statement->kind) {
            case PALI_STATEMENT_SET_SELF:
            case PALI_STATEMENT_SET_ACTOR:
                if (statement->name >= document->name_count ||
                    statement->expression >= document->expression_count) {
                    return document_fail(
                        error, "assignment statement is structurally invalid");
                }
                name_references[statement->name]++;
                expression_references[statement->expression]++;
                break;
            case PALI_STATEMENT_DESTROY_SELF:
                if (statement->name != PALI_NODE_NONE ||
                    statement->expression != PALI_NODE_NONE) {
                    return document_fail(
                        error, "destroy statement is structurally invalid");
                }
                break;
            case PALI_STATEMENT_MESSAGE:
                if (statement->name != PALI_NODE_NONE ||
                    statement->expression >= document->expression_count) {
                    return document_fail(
                        error, "message statement is structurally invalid");
                }
                expression_references[statement->expression]++;
                break;
            default:
                return document_fail(error,
                                     "document has an unknown statement kind");
        }
    }

    for (uint16_t index = 0; index < document->expression_count; ++index) {
        if (expression_references[index] != 1) {
            return document_fail(
                error,
                "expressions must form owned trees without orphans or sharing");
        }
    }
    for (uint16_t index = 0; index < document->constant_count; ++index) {
        if (constant_references[index] != 1) {
            return document_fail(error,
                                 "expression constant is unused or shared");
        }
    }
    for (uint16_t index = 0; index < document->name_count; ++index) {
        if (name_references[index] == 0) {
            return document_fail(error, "document property name is unused");
        }
    }
    return true;
}

static void fail_at(Parser *parser, const PaliToken *token,
                    const char *message) {
    if (parser->failed) {
        return;
    }
    parser->failed = true;
    parser->error->line = token->line;
    parser->error->column = token->column;
    (void)snprintf(parser->error->message, sizeof(parser->error->message),
                   "%s", message);
}

static bool enter_expression(Parser *parser, const PaliToken *site) {
    if (parser->expression_depth >= PALI_MAX_EXPRESSIONS) {
        fail_at(parser, site, "expression nesting exceeds the fixed limit");
        return false;
    }
    parser->expression_depth++;
    return true;
}

static void leave_expression(Parser *parser) {
    if (parser->expression_depth > 0) {
        parser->expression_depth--;
    }
}

static void advance_token(Parser *parser) {
    parser->previous = parser->current;
    parser->current = pali_lexer_next(&parser->lexer);
    if (parser->current.type == PALI_TOKEN_ERROR) {
        fail_at(parser, &parser->current, parser->current.decoded);
    }
}

static bool token_is_word(const PaliToken *token, const char *word) {
    const size_t length = strlen(word);
    return token->type == PALI_TOKEN_IDENTIFIER && token->length == length &&
           memcmp(token->start, word, length) == 0;
}

static void copy_token_text(char *out, size_t capacity,
                            const PaliToken *token) {
    size_t length = token->length;
    if (length >= capacity) {
        length = capacity - 1;
    }
    memcpy(out, token->start, length);
    out[length] = '\0';
}

static bool consume_type(Parser *parser, PaliTokenType type,
                         const char *message) {
    if (parser->current.type != type) {
        fail_at(parser, &parser->current, message);
        return false;
    }
    advance_token(parser);
    return true;
}

static bool consume_word(Parser *parser, const char *word,
                         const char *message) {
    if (!token_is_word(&parser->current, word)) {
        fail_at(parser, &parser->current, message);
        return false;
    }
    advance_token(parser);
    return true;
}

static void skip_newlines(Parser *parser) {
    while (parser->current.type == PALI_TOKEN_NEWLINE && !parser->failed) {
        advance_token(parser);
    }
}

static bool finish_line(Parser *parser) {
    if (parser->current.type == PALI_TOKEN_NEWLINE) {
        skip_newlines(parser);
        return true;
    }
    if (parser->current.type == PALI_TOKEN_EOF ||
        token_is_word(&parser->current, "end")) {
        return true;
    }
    fail_at(parser, &parser->current, "expected the end of the line");
    return false;
}

static double token_number(Parser *parser, const PaliToken *token) {
    char buffer[64];
    if (token->length >= sizeof(buffer)) {
        fail_at(parser, token, "numeric literal is too long");
        return 0.0;
    }
    memcpy(buffer, token->start, token->length);
    buffer[token->length] = '\0';
    char *end = NULL;
    const double value = strtod(buffer, &end);
    if (end == NULL || *end != '\0' || !isfinite(value)) {
        fail_at(parser, token, "invalid numeric literal");
        return 0.0;
    }
    return value;
}

static int document_add_constant(Parser *parser, PaliValue value,
                                 const PaliToken *site) {
    if (parser->document->constant_count >= PALI_MAX_CONSTANTS) {
        fail_at(parser, site, "too many expression constants");
        return 0;
    }
    const int index = (int)parser->document->constant_count;
    parser->document->constants[parser->document->constant_count++] = value;
    return index;
}

static int document_add_name(Parser *parser, const PaliToken *token) {
    if (token->length >= PALI_NAME_CAP) {
        fail_at(parser, token, "property name is too long");
        return 0;
    }
    char name[PALI_NAME_CAP];
    copy_token_text(name, sizeof(name), token);
    for (uint16_t index = 0; index < parser->document->name_count; ++index) {
        if (strcmp(parser->document->names[index], name) == 0) {
            return (int)index;
        }
    }
    if (parser->document->name_count >= PALI_MAX_DOCUMENT_NAMES) {
        fail_at(parser, token, "too many distinct property names");
        return 0;
    }
    const int index = (int)parser->document->name_count;
    (void)snprintf(parser->document->names[index], PALI_NAME_CAP, "%s", name);
    parser->document->name_count++;
    return index;
}

static uint8_t document_add_expression(Parser *parser,
                                       PaliExpression expression,
                                       const PaliToken *site) {
    if (parser->document->expression_count >= PALI_MAX_EXPRESSIONS) {
        fail_at(parser, site, "use handler has too many expressions");
        return PALI_NODE_NONE;
    }
    const uint8_t index = (uint8_t)parser->document->expression_count;
    parser->document->expressions[parser->document->expression_count++] =
        expression;
    return index;
}

static void document_add_statement(Parser *parser, PaliStatement statement,
                                   const PaliToken *site) {
    if (parser->document->statement_count >= PALI_MAX_STATEMENTS) {
        fail_at(parser, site, "use handler has too many statements");
        return;
    }
    parser->document->statements[parser->document->statement_count++] =
        statement;
}

static uint8_t parse_expression(Parser *parser);

static uint8_t literal_expression(Parser *parser, const PaliToken *site,
                                  PaliValue value) {
    PaliExpression expression;
    memset(&expression, 0, sizeof(expression));
    expression.kind = (uint8_t)PALI_EXPRESSION_LITERAL;
    expression.left = PALI_NODE_NONE;
    expression.right = PALI_NODE_NONE;
    expression.operand = (uint8_t)document_add_constant(parser, value, site);
    expression.line = (uint16_t)site->line;
    return document_add_expression(parser, expression, site);
}

static uint8_t binary_expression(Parser *parser, PaliExpressionKind kind,
                                 uint8_t left, uint8_t right,
                                 const PaliToken *site) {
    PaliExpression expression;
    memset(&expression, 0, sizeof(expression));
    expression.kind = (uint8_t)kind;
    expression.left = left;
    expression.right = right;
    expression.operand = 0;
    expression.line = (uint16_t)site->line;
    return document_add_expression(parser, expression, site);
}

static uint8_t parse_primary(Parser *parser) {
    const PaliToken site = parser->current;
    if (site.type == PALI_TOKEN_NUMBER) {
        advance_token(parser);
        return literal_expression(parser, &site,
                                  pali_number(token_number(parser, &site)));
    }
    if (site.type == PALI_TOKEN_STRING) {
        advance_token(parser);
        return literal_expression(parser, &site, pali_text(site.decoded));
    }
    if (token_is_word(&site, "true") || token_is_word(&site, "false")) {
        advance_token(parser);
        return literal_expression(
            parser, &site, pali_bool(token_is_word(&site, "true")));
    }
    if (site.type == PALI_TOKEN_LEFT_PAREN) {
        advance_token(parser);
        if (!enter_expression(parser, &site)) {
            return PALI_NODE_NONE;
        }
        const uint8_t expression = parse_expression(parser);
        leave_expression(parser);
        (void)consume_type(parser, PALI_TOKEN_RIGHT_PAREN,
                           "expected ')' after expression");
        return expression;
    }
    if (site.type != PALI_TOKEN_IDENTIFIER) {
        fail_at(parser, &site, "expected a value or property");
        return PALI_NODE_NONE;
    }

    char first[PALI_NAME_CAP];
    copy_token_text(first, sizeof(first), &site);
    advance_token(parser);
    if (parser->current.type == PALI_TOKEN_DOT) {
        PaliExpressionKind kind = PALI_EXPRESSION_GET_SELF;
        if (strcmp(first, "self") == 0) {
            kind = PALI_EXPRESSION_GET_SELF;
        } else if (strcmp(first, "actor") == 0) {
            kind = PALI_EXPRESSION_GET_ACTOR;
        } else {
            fail_at(parser, &site, "property root must be 'self' or 'actor'");
            return PALI_NODE_NONE;
        }
        advance_token(parser);
        const PaliToken name = parser->current;
        if (!consume_type(parser, PALI_TOKEN_IDENTIFIER,
                          "expected property name after '.'")) {
            return PALI_NODE_NONE;
        }
        PaliExpression expression;
        memset(&expression, 0, sizeof(expression));
        expression.kind = (uint8_t)kind;
        expression.left = PALI_NODE_NONE;
        expression.right = PALI_NODE_NONE;
        expression.operand = (uint8_t)document_add_name(parser, &name);
        expression.line = (uint16_t)site.line;
        return document_add_expression(parser, expression, &site);
    }
    if (parser->current.type == PALI_TOKEN_LEFT_PAREN) {
        PaliExpressionKind kind = PALI_EXPRESSION_MIN;
        if (strcmp(first, "min") == 0) {
            kind = PALI_EXPRESSION_MIN;
        } else if (strcmp(first, "max") == 0) {
            kind = PALI_EXPRESSION_MAX;
        } else {
            fail_at(parser, &site,
                    "only min(...) and max(...) return expression values");
            return PALI_NODE_NONE;
        }
        advance_token(parser);
        if (!enter_expression(parser, &site)) {
            return PALI_NODE_NONE;
        }
        const uint8_t left = parse_expression(parser);
        (void)consume_type(parser, PALI_TOKEN_COMMA,
                           "expected ',' between function arguments");
        const uint8_t right = parse_expression(parser);
        leave_expression(parser);
        (void)consume_type(parser, PALI_TOKEN_RIGHT_PAREN,
                           "expected ')' after function arguments");
        return binary_expression(parser, kind, left, right, &site);
    }

    fail_at(parser, &site, "bare names are not values");
    return PALI_NODE_NONE;
}

static uint8_t parse_unary(Parser *parser) {
    if (parser->current.type == PALI_TOKEN_MINUS) {
        const PaliToken site = parser->current;
        advance_token(parser);
        if (!enter_expression(parser, &site)) {
            return PALI_NODE_NONE;
        }
        PaliExpression expression;
        memset(&expression, 0, sizeof(expression));
        expression.kind = (uint8_t)PALI_EXPRESSION_NEGATE;
        expression.left = parse_unary(parser);
        leave_expression(parser);
        expression.right = PALI_NODE_NONE;
        expression.line = (uint16_t)site.line;
        return document_add_expression(parser, expression, &site);
    }
    return parse_primary(parser);
}

static uint8_t parse_factor(Parser *parser) {
    uint8_t left = parse_unary(parser);
    while (!parser->failed &&
           (parser->current.type == PALI_TOKEN_STAR ||
            parser->current.type == PALI_TOKEN_SLASH)) {
        const PaliToken operator_token = parser->current;
        advance_token(parser);
        const uint8_t right = parse_unary(parser);
        left = binary_expression(
            parser,
            operator_token.type == PALI_TOKEN_STAR
                ? PALI_EXPRESSION_MULTIPLY
                : PALI_EXPRESSION_DIVIDE,
            left, right, &operator_token);
    }
    return left;
}

static uint8_t parse_expression(Parser *parser) {
    uint8_t left = parse_factor(parser);
    while (!parser->failed &&
           (parser->current.type == PALI_TOKEN_PLUS ||
            parser->current.type == PALI_TOKEN_MINUS)) {
        const PaliToken operator_token = parser->current;
        advance_token(parser);
        const uint8_t right = parse_factor(parser);
        left = binary_expression(
            parser,
            operator_token.type == PALI_TOKEN_PLUS ? PALI_EXPRESSION_ADD
                                                   : PALI_EXPRESSION_SUBTRACT,
            left, right, &operator_token);
    }
    return left;
}

static bool parse_literal(Parser *parser, PaliValue *out) {
    bool negative = false;
    if (parser->current.type == PALI_TOKEN_MINUS) {
        negative = true;
        advance_token(parser);
    }
    const PaliToken token = parser->current;
    if (token.type == PALI_TOKEN_NUMBER) {
        advance_token(parser);
        *out = pali_number((negative ? -1.0 : 1.0) *
                           token_number(parser, &token));
        return !parser->failed;
    }
    if (negative) {
        fail_at(parser, &token, "'-' can only prefix a numeric property");
        return false;
    }
    if (token.type == PALI_TOKEN_STRING) {
        advance_token(parser);
        *out = pali_text(token.decoded);
        return true;
    }
    if (token_is_word(&token, "true") || token_is_word(&token, "false")) {
        advance_token(parser);
        *out = pali_bool(token_is_word(&token, "true"));
        return true;
    }
    fail_at(parser, &token,
            "prototype properties require a number, Boolean, or text literal");
    return false;
}

static void parse_property(Parser *parser) {
    if (parser->document->property_count >= PALI_MAX_PROPERTIES) {
        fail_at(parser, &parser->current, "too many prototype properties");
        return;
    }
    const PaliToken name = parser->current;
    if (!consume_type(parser, PALI_TOKEN_IDENTIFIER,
                      "expected a property name")) {
        return;
    }
    if (name.length >= PALI_NAME_CAP) {
        fail_at(parser, &name, "property name is too long");
        return;
    }
    (void)consume_type(parser, PALI_TOKEN_EQUAL,
                       "expected '=' after property name");
    PaliProperty property;
    memset(&property, 0, sizeof(property));
    copy_token_text(property.name, sizeof(property.name), &name);
    (void)parse_literal(parser, &property.value);
    for (uint16_t index = 0;
         !parser->failed && index < parser->document->property_count;
         ++index) {
        if (strcmp(parser->document->properties[index].name,
                   property.name) == 0) {
            fail_at(parser, &name, "prototype property is declared twice");
        }
    }
    if (!parser->failed) {
        parser->document->properties[parser->document->property_count++] =
            property;
    }
    (void)finish_line(parser);
}

static void parse_event_statement(Parser *parser) {
    const PaliToken first = parser->current;
    if (!consume_type(parser, PALI_TOKEN_IDENTIFIER,
                      "expected an assignment or host call")) {
        return;
    }
    char word[PALI_NAME_CAP];
    copy_token_text(word, sizeof(word), &first);

    if (parser->current.type == PALI_TOKEN_DOT) {
        PaliStatementKind kind = PALI_STATEMENT_SET_SELF;
        if (strcmp(word, "self") == 0) {
            kind = PALI_STATEMENT_SET_SELF;
        } else if (strcmp(word, "actor") == 0) {
            kind = PALI_STATEMENT_SET_ACTOR;
        } else {
            fail_at(parser, &first,
                    "assignment root must be 'self' or 'actor'");
            return;
        }
        advance_token(parser);
        const PaliToken property = parser->current;
        (void)consume_type(parser, PALI_TOKEN_IDENTIFIER,
                           "expected property name after '.'");
        const int name = document_add_name(parser, &property);
        (void)consume_type(parser, PALI_TOKEN_EQUAL,
                           "expected '=' in property assignment");
        const uint8_t expression = parse_expression(parser);
        PaliStatement statement;
        memset(&statement, 0, sizeof(statement));
        statement.kind = (uint8_t)kind;
        statement.name = (uint8_t)name;
        statement.expression = expression;
        statement.line = (uint16_t)first.line;
        document_add_statement(parser, statement, &first);
        (void)finish_line(parser);
        return;
    }

    if (parser->current.type != PALI_TOKEN_LEFT_PAREN) {
        fail_at(parser, &parser->current,
                "expected '.' for assignment or '(' for host call");
        return;
    }
    advance_token(parser);
    PaliStatement statement;
    memset(&statement, 0, sizeof(statement));
    statement.name = PALI_NODE_NONE;
    statement.expression = PALI_NODE_NONE;
    statement.line = (uint16_t)first.line;
    if (strcmp(word, "destroy") == 0) {
        (void)consume_word(parser, "self", "destroy only accepts self");
        (void)consume_type(parser, PALI_TOKEN_RIGHT_PAREN,
                           "expected ')' after destroy(self)");
        statement.kind = (uint8_t)PALI_STATEMENT_DESTROY_SELF;
    } else if (strcmp(word, "message") == 0) {
        statement.kind = (uint8_t)PALI_STATEMENT_MESSAGE;
        statement.expression = parse_expression(parser);
        (void)consume_type(parser, PALI_TOKEN_RIGHT_PAREN,
                           "expected ')' after message value");
    } else {
        fail_at(parser, &first,
                "host calls are limited to destroy(self) and message(value)");
    }
    document_add_statement(parser, statement, &first);
    (void)finish_line(parser);
}

static void parse_use_handler(Parser *parser) {
    const PaliToken on_token = parser->current;
    (void)consume_word(parser, "on", "expected 'on'");
    (void)consume_word(parser, "use", "only 'on use(actor)' is supported");
    (void)consume_type(parser, PALI_TOKEN_LEFT_PAREN,
                       "expected '(' after use");
    (void)consume_word(parser, "actor", "use handler parameter must be actor");
    (void)consume_type(parser, PALI_TOKEN_RIGHT_PAREN,
                       "expected ')' after actor");
    if (parser->document->has_use) {
        fail_at(parser, &on_token, "prototype already has a use handler");
        return;
    }
    parser->document->has_use = true;
    (void)finish_line(parser);
    while (!parser->failed && parser->current.type != PALI_TOKEN_EOF &&
           !token_is_word(&parser->current, "end")) {
        parse_event_statement(parser);
    }
    (void)consume_word(parser, "end", "expected 'end' after use handler");
    (void)finish_line(parser);
}

bool pali_parse_document(const char *source, PaliDocument *out,
                         PaliError *error) {
    if (out == NULL || error == NULL) {
        return false;
    }
    memset(out, 0, sizeof(*out));
    memset(error, 0, sizeof(*error));
    if (source == NULL) {
        error->line = 1;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message),
                       "source is null");
        return false;
    }
    if (!bounded_string(source, PALI_SOURCE_CAP)) {
        error->line = 1;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message),
                       "source exceeds the %d-byte limit or is not terminated",
                       PALI_SOURCE_CAP - 1);
        return false;
    }

    Parser parser;
    memset(&parser, 0, sizeof(parser));
    parser.document = out;
    parser.error = error;
    pali_lexer_init(&parser.lexer, source);
    advance_token(&parser);
    skip_newlines(&parser);
    (void)consume_word(&parser, "prototype",
                       "source must begin with 'prototype'");
    const PaliToken name = parser.current;
    (void)consume_type(&parser, PALI_TOKEN_IDENTIFIER,
                       "expected prototype name");
    if (!parser.failed && name.length >= PALI_NAME_CAP) {
        fail_at(&parser, &name, "prototype name is too long");
    }
    if (!parser.failed) {
        copy_token_text(out->prototype_name, sizeof(out->prototype_name), &name);
    }
    (void)finish_line(&parser);

    while (!parser.failed && parser.current.type != PALI_TOKEN_EOF &&
           !token_is_word(&parser.current, "end")) {
        if (token_is_word(&parser.current, "on")) {
            parse_use_handler(&parser);
        } else {
            parse_property(&parser);
        }
    }
    (void)consume_word(&parser, "end", "expected 'end' after prototype");
    skip_newlines(&parser);
    if (!parser.failed && parser.current.type != PALI_TOKEN_EOF) {
        fail_at(&parser, &parser.current,
                "unexpected source after prototype end");
    }
    return !parser.failed;
}

static void compile_fail(DocumentCompiler *compiler, uint16_t line,
                         const char *message) {
    if (compiler->failed) {
        return;
    }
    compiler->failed = true;
    compiler->error->line = (int)line;
    compiler->error->column = 1;
    (void)snprintf(compiler->error->message, sizeof(compiler->error->message),
                   "%s", message);
}

static int compile_add_constant(DocumentCompiler *compiler, PaliValue value,
                                uint16_t line) {
    if (compiler->program->constant_count >= PALI_MAX_CONSTANTS) {
        compile_fail(compiler, line, "too many constants in this prototype");
        return 0;
    }
    const int index = (int)compiler->program->constant_count;
    compiler->program->constants[compiler->program->constant_count++] = value;
    return index;
}

static void compile_emit(DocumentCompiler *compiler, PaliOp op, int operand,
                         uint16_t line) {
    if (compiler->failed) {
        return;
    }
    if (compiler->program->code_count >= PALI_MAX_CODE) {
        compile_fail(compiler, line, "use handler is too large");
        return;
    }
    PaliInstruction instruction;
    instruction.op = (uint8_t)op;
    instruction.operand = (uint8_t)operand;
    instruction.line = line;
    compiler->program->code[compiler->program->code_count++] = instruction;
}

static bool compile_expression(DocumentCompiler *compiler, uint8_t index,
                               int depth) {
    if (compiler->failed) {
        return false;
    }
    if (depth > PALI_MAX_EXPRESSIONS) {
        compile_fail(compiler, 0, "expression graph contains a cycle");
        return false;
    }
    if (index == PALI_NODE_NONE ||
        index >= compiler->document->expression_count) {
        compile_fail(compiler, 0, "expression references an invalid node");
        return false;
    }
    const PaliExpression *expression =
        &compiler->document->expressions[index];
    switch ((PaliExpressionKind)expression->kind) {
        case PALI_EXPRESSION_LITERAL:
            if (expression->operand >= compiler->document->constant_count) {
                compile_fail(compiler, expression->line,
                             "literal references an invalid constant");
                return false;
            }
            compile_emit(
                compiler, PALI_OP_CONSTANT,
                compile_add_constant(
                    compiler,
                    compiler->document->constants[expression->operand],
                    expression->line),
                expression->line);
            break;
        case PALI_EXPRESSION_GET_SELF:
        case PALI_EXPRESSION_GET_ACTOR:
            if (expression->operand >= compiler->document->name_count) {
                compile_fail(compiler, expression->line,
                             "property read references an invalid name");
                return false;
            }
            compile_emit(
                compiler,
                expression->kind == PALI_EXPRESSION_GET_SELF
                    ? PALI_OP_GET_SELF
                    : PALI_OP_GET_ACTOR,
                compile_add_constant(
                    compiler,
                    pali_text(compiler->document->names[expression->operand]),
                    expression->line),
                expression->line);
            break;
        case PALI_EXPRESSION_NEGATE:
            if (!compile_expression(compiler, expression->left, depth + 1)) {
                return false;
            }
            compile_emit(compiler, PALI_OP_NEGATE, 0, expression->line);
            break;
        case PALI_EXPRESSION_ADD:
        case PALI_EXPRESSION_SUBTRACT:
        case PALI_EXPRESSION_MULTIPLY:
        case PALI_EXPRESSION_DIVIDE:
        case PALI_EXPRESSION_MIN:
        case PALI_EXPRESSION_MAX: {
            if (!compile_expression(compiler, expression->left, depth + 1) ||
                !compile_expression(compiler, expression->right, depth + 1)) {
                return false;
            }
            PaliOp operation = PALI_OP_ADD;
            switch ((PaliExpressionKind)expression->kind) {
                case PALI_EXPRESSION_ADD:
                    operation = PALI_OP_ADD;
                    break;
                case PALI_EXPRESSION_SUBTRACT:
                    operation = PALI_OP_SUBTRACT;
                    break;
                case PALI_EXPRESSION_MULTIPLY:
                    operation = PALI_OP_MULTIPLY;
                    break;
                case PALI_EXPRESSION_DIVIDE:
                    operation = PALI_OP_DIVIDE;
                    break;
                case PALI_EXPRESSION_MIN:
                    operation = PALI_OP_MIN;
                    break;
                case PALI_EXPRESSION_MAX:
                    operation = PALI_OP_MAX;
                    break;
                default:
                    break;
            }
            compile_emit(compiler, operation, 0, expression->line);
            break;
        }
        default:
            compile_fail(compiler, expression->line,
                         "document contains an unknown expression kind");
            return false;
    }
    return !compiler->failed;
}

bool pali_compile_document(const PaliDocument *document, PaliProgram *out,
                           PaliError *error) {
    if (document == NULL || out == NULL || error == NULL) {
        return false;
    }
    memset(out, 0, sizeof(*out));
    memset(error, 0, sizeof(*error));
    if (!identifier_is_valid(document->prototype_name,
                             sizeof(document->prototype_name))) {
        error->line = 1;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message),
                       "document has no valid prototype name");
        return false;
    }
    if (document->property_count > PALI_MAX_PROPERTIES ||
        document->constant_count > PALI_MAX_CONSTANTS ||
        document->name_count > PALI_MAX_DOCUMENT_NAMES ||
        document->expression_count > PALI_MAX_EXPRESSIONS ||
        document->statement_count > PALI_MAX_STATEMENTS) {
        error->line = 1;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message),
                       "document exceeds a fixed PALI capacity");
        return false;
    }
    for (uint16_t index = 0; index < document->property_count; ++index) {
        const PaliProperty *property = &document->properties[index];
        if (!identifier_is_valid(property->name, sizeof(property->name)) ||
            strcmp(property->name, "on") == 0 ||
            strcmp(property->name, "end") == 0 ||
            !document_value_is_valid(property->value)) {
            error->line = 1;
            error->column = 1;
            (void)snprintf(error->message, sizeof(error->message),
                           "document contains an invalid property");
            return false;
        }
        for (uint16_t previous = 0; previous < index; ++previous) {
            if (strcmp(document->properties[previous].name,
                       property->name) == 0) {
                error->line = 1;
                error->column = 1;
                (void)snprintf(error->message, sizeof(error->message),
                               "document contains a duplicate property");
                return false;
            }
        }
    }
    for (uint16_t index = 0; index < document->name_count; ++index) {
        if (!identifier_is_valid(document->names[index],
                                 sizeof(document->names[index]))) {
            error->line = 1;
            error->column = 1;
            (void)snprintf(error->message, sizeof(error->message),
                           "document contains an invalid property name");
            return false;
        }
        for (uint16_t previous = 0; previous < index; ++previous) {
            if (strcmp(document->names[previous],
                       document->names[index]) == 0) {
                error->line = 1;
                error->column = 1;
                (void)snprintf(error->message, sizeof(error->message),
                               "document contains a duplicate property name");
                return false;
            }
        }
    }
    for (uint16_t index = 0; index < document->constant_count; ++index) {
        if (!document_value_is_valid(document->constants[index])) {
            error->line = 1;
            error->column = 1;
            (void)snprintf(error->message, sizeof(error->message),
                           "document contains an invalid expression constant");
            return false;
        }
    }
    if (!document->has_use &&
        (document->statement_count != 0 || document->expression_count != 0 ||
         document->constant_count != 0 || document->name_count != 0)) {
        error->line = 1;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message),
                       "Behavior nodes require an on use(actor) handler");
        return false;
    }
    if (!document_structure_is_valid(document, error)) {
        return false;
    }

    (void)snprintf(out->prototype_name, sizeof(out->prototype_name), "%s",
                   document->prototype_name);
    out->property_count = document->property_count;
    for (uint16_t index = 0; index < document->property_count; ++index) {
        out->properties[index] = document->properties[index];
    }

    DocumentCompiler compiler;
    memset(&compiler, 0, sizeof(compiler));
    compiler.document = document;
    compiler.program = out;
    compiler.error = error;
    out->has_use = document->has_use;
    for (uint16_t index = 0;
         index < document->statement_count && !compiler.failed; ++index) {
        const PaliStatement *statement = &document->statements[index];
        switch ((PaliStatementKind)statement->kind) {
            case PALI_STATEMENT_SET_SELF:
            case PALI_STATEMENT_SET_ACTOR:
                if (statement->name >= document->name_count) {
                    compile_fail(&compiler, statement->line,
                                 "assignment references an invalid name");
                    break;
                }
                (void)compile_expression(&compiler, statement->expression, 0);
                compile_emit(
                    &compiler,
                    statement->kind == PALI_STATEMENT_SET_SELF
                        ? PALI_OP_SET_SELF
                        : PALI_OP_SET_ACTOR,
                    compile_add_constant(
                        &compiler, pali_text(document->names[statement->name]),
                        statement->line),
                    statement->line);
                break;
            case PALI_STATEMENT_DESTROY_SELF:
                compile_emit(&compiler, PALI_OP_DESTROY, 0, statement->line);
                break;
            case PALI_STATEMENT_MESSAGE:
                (void)compile_expression(&compiler, statement->expression, 0);
                compile_emit(&compiler, PALI_OP_MESSAGE, 0, statement->line);
                break;
            default:
                compile_fail(&compiler, statement->line,
                             "document contains an unknown statement kind");
                break;
        }
    }
    if (document->has_use && !compiler.failed) {
        compile_emit(&compiler, PALI_OP_RETURN, 0, 1);
    }
    return !compiler.failed;
}

static void writer_fail(TextWriter *writer, const char *message) {
    if (writer->failed) {
        return;
    }
    writer->failed = true;
    writer->error->line = 1;
    writer->error->column = 1;
    (void)snprintf(writer->error->message, sizeof(writer->error->message),
                   "%s", message);
}

static void writer_append(TextWriter *writer, const char *format, ...) {
    if (writer->failed || writer->length >= writer->capacity) {
        return;
    }
    va_list arguments;
    va_start(arguments, format);
    const int written = vsnprintf(writer->out + writer->length,
                                  writer->capacity - writer->length, format,
                                  arguments);
    va_end(arguments);
    if (written < 0 || (size_t)written >= writer->capacity - writer->length) {
        writer_fail(writer, "formatted PALI exceeds its destination capacity");
        return;
    }
    writer->length += (size_t)written;
}

static void format_text(TextWriter *writer, const char *text) {
    writer_append(writer, "\"");
    for (size_t index = 0; text[index] != '\0' && !writer->failed; ++index) {
        if (text[index] == '\n') {
            writer_append(writer, "\\n");
        } else if (text[index] == '\\' || text[index] == '"') {
            writer_append(writer, "\\%c", text[index]);
        } else {
            writer_append(writer, "%c", text[index]);
        }
    }
    writer_append(writer, "\"");
}

static void format_value(TextWriter *writer, PaliValue value) {
    switch (value.type) {
        case PALI_VALUE_NUMBER:
            if (!isfinite(value.as.number)) {
                writer_fail(writer, "cannot format a non-finite number");
            } else {
                writer_append(writer, "%.*g", DBL_DECIMAL_DIG,
                              value.as.number);
            }
            break;
        case PALI_VALUE_BOOL:
            writer_append(writer, "%s", value.as.boolean ? "true" : "false");
            break;
        case PALI_VALUE_TEXT:
            format_text(writer, value.as.text);
            break;
        default:
            writer_fail(writer, "cannot format an unknown PALI value");
            break;
    }
}

static void format_expression(TextWriter *writer,
                              const PaliDocument *document, uint8_t index,
                              int depth) {
    if (writer->failed) {
        return;
    }
    if (depth > PALI_MAX_EXPRESSIONS) {
        writer_fail(writer, "expression graph contains a cycle");
        return;
    }
    if (index == PALI_NODE_NONE || index >= document->expression_count) {
        writer_fail(writer, "cannot format an invalid expression node");
        return;
    }
    const PaliExpression *expression = &document->expressions[index];
    switch ((PaliExpressionKind)expression->kind) {
        case PALI_EXPRESSION_LITERAL:
            if (expression->operand >= document->constant_count) {
                writer_fail(writer, "literal references an invalid constant");
                return;
            }
            format_value(writer, document->constants[expression->operand]);
            break;
        case PALI_EXPRESSION_GET_SELF:
        case PALI_EXPRESSION_GET_ACTOR:
            if (expression->operand >= document->name_count) {
                writer_fail(writer, "property read references an invalid name");
                return;
            }
            writer_append(writer, "%s.%s",
                          expression->kind == PALI_EXPRESSION_GET_SELF
                              ? "self"
                              : "actor",
                          document->names[expression->operand]);
            break;
        case PALI_EXPRESSION_NEGATE:
            writer_append(writer, "(-");
            format_expression(writer, document, expression->left, depth + 1);
            writer_append(writer, ")");
            break;
        case PALI_EXPRESSION_MIN:
        case PALI_EXPRESSION_MAX:
            writer_append(writer, "%s(",
                          expression->kind == PALI_EXPRESSION_MIN ? "min"
                                                                  : "max");
            format_expression(writer, document, expression->left, depth + 1);
            writer_append(writer, ", ");
            format_expression(writer, document, expression->right, depth + 1);
            writer_append(writer, ")");
            break;
        case PALI_EXPRESSION_ADD:
        case PALI_EXPRESSION_SUBTRACT:
        case PALI_EXPRESSION_MULTIPLY:
        case PALI_EXPRESSION_DIVIDE: {
            const char *operation = "+";
            if (expression->kind == PALI_EXPRESSION_SUBTRACT) {
                operation = "-";
            } else if (expression->kind == PALI_EXPRESSION_MULTIPLY) {
                operation = "*";
            } else if (expression->kind == PALI_EXPRESSION_DIVIDE) {
                operation = "/";
            }
            writer_append(writer, "(");
            format_expression(writer, document, expression->left, depth + 1);
            writer_append(writer, " %s ", operation);
            format_expression(writer, document, expression->right, depth + 1);
            writer_append(writer, ")");
            break;
        }
        default:
            writer_fail(writer, "cannot format an unknown expression kind");
            break;
    }
}

bool pali_format_document(const PaliDocument *document, char *out,
                          size_t capacity, PaliError *error) {
    if (document == NULL || out == NULL || capacity == 0 || error == NULL) {
        return false;
    }
    out[0] = '\0';
    memset(error, 0, sizeof(*error));
    PaliProgram validation;
    if (!pali_compile_document(document, &validation, error)) {
        return false;
    }
    const size_t bounded_capacity =
        capacity < PALI_SOURCE_CAP ? capacity : PALI_SOURCE_CAP;
    TextWriter writer = {out, bounded_capacity, 0, error, false};
    writer_append(&writer, "prototype %s\n", document->prototype_name);
    for (uint16_t index = 0; index < document->property_count; ++index) {
        writer_append(&writer, "    %s = ", document->properties[index].name);
        format_value(&writer, document->properties[index].value);
        writer_append(&writer, "\n");
    }
    if (document->has_use) {
        if (document->property_count > 0) {
            writer_append(&writer, "\n");
        }
        writer_append(&writer, "    on use(actor)\n");
        for (uint16_t index = 0; index < document->statement_count; ++index) {
            const PaliStatement *statement = &document->statements[index];
            writer_append(&writer, "        ");
            switch ((PaliStatementKind)statement->kind) {
                case PALI_STATEMENT_SET_SELF:
                case PALI_STATEMENT_SET_ACTOR:
                    if (statement->name >= document->name_count) {
                        writer_fail(&writer,
                                    "assignment references an invalid name");
                        break;
                    }
                    writer_append(
                        &writer, "%s.%s = ",
                        statement->kind == PALI_STATEMENT_SET_SELF ? "self"
                                                                  : "actor",
                        document->names[statement->name]);
                    format_expression(&writer, document, statement->expression,
                                      0);
                    break;
                case PALI_STATEMENT_DESTROY_SELF:
                    writer_append(&writer, "destroy(self)");
                    break;
                case PALI_STATEMENT_MESSAGE:
                    writer_append(&writer, "message(");
                    format_expression(&writer, document, statement->expression,
                                      0);
                    writer_append(&writer, ")");
                    break;
                default:
                    writer_fail(&writer,
                                "cannot format an unknown statement kind");
                    break;
            }
            writer_append(&writer, "\n");
        }
        writer_append(&writer, "    end\n");
    }
    writer_append(&writer, "end\n");
    return !writer.failed;
}

bool pali_compile(const char *source, PaliProgram *out, PaliError *error) {
    if (out == NULL || error == NULL) {
        return false;
    }
    PaliDocument document;
    if (!pali_parse_document(source, &document, error)) {
        memset(out, 0, sizeof(*out));
        return false;
    }
    return pali_compile_document(&document, out, error);
}
