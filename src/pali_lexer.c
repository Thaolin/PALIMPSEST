#include "pali_internal.h"

#include <ctype.h>
#include <stdio.h>
#include <string.h>

static PaliToken token_make(PaliTokenType type, const char *start, size_t length,
                            int line, int column) {
    PaliToken token;
    memset(&token, 0, sizeof(token));
    token.type = type;
    token.start = start;
    token.length = length;
    token.line = line;
    token.column = column;
    return token;
}

static PaliToken token_error(const char *start, int line, int column,
                             const char *message) {
    PaliToken token = token_make(PALI_TOKEN_ERROR, start, 1, line, column);
    (void)snprintf(token.decoded, sizeof(token.decoded), "%s", message);
    return token;
}

void pali_lexer_init(PaliLexer *lexer, const char *source) {
    lexer->source = source != NULL ? source : "";
    lexer->position = 0;
    lexer->line = 1;
    lexer->column = 1;
}

static char peek(const PaliLexer *lexer) {
    return lexer->source[lexer->position];
}

static char peek_next(const PaliLexer *lexer) {
    char current = peek(lexer);
    return current == '\0' ? '\0' : lexer->source[lexer->position + 1];
}

static char take(PaliLexer *lexer) {
    char value = lexer->source[lexer->position];
    if (value != '\0') {
        lexer->position++;
        lexer->column++;
    }
    return value;
}

static void skip_horizontal_space_and_comments(PaliLexer *lexer) {
    for (;;) {
        while (peek(lexer) == ' ' || peek(lexer) == '\t' ||
               peek(lexer) == '\v' || peek(lexer) == '\f') {
            (void)take(lexer);
        }
        if (peek(lexer) == '-' && peek_next(lexer) == '-') {
            while (peek(lexer) != '\0' && peek(lexer) != '\n' &&
                   peek(lexer) != '\r') {
                (void)take(lexer);
            }
            continue;
        }
        break;
    }
}

static PaliToken lex_identifier(PaliLexer *lexer, const char *start,
                                size_t start_pos, int line, int column) {
    while (isalnum((unsigned char)peek(lexer)) || peek(lexer) == '_') {
        (void)take(lexer);
    }
    return token_make(PALI_TOKEN_IDENTIFIER, start,
                      lexer->position - start_pos, line, column);
}

static PaliToken lex_number(PaliLexer *lexer, const char *start,
                            size_t start_pos, int line, int column) {
    while (isdigit((unsigned char)peek(lexer))) {
        (void)take(lexer);
    }
    if (peek(lexer) == '.' && isdigit((unsigned char)peek_next(lexer))) {
        (void)take(lexer);
        while (isdigit((unsigned char)peek(lexer))) {
            (void)take(lexer);
        }
    }
    if (peek(lexer) == 'e' || peek(lexer) == 'E') {
        (void)take(lexer);
        if (peek(lexer) == '+' || peek(lexer) == '-') {
            (void)take(lexer);
        }
        if (!isdigit((unsigned char)peek(lexer))) {
            return token_error(start, line, column,
                               "numeric exponent requires digits");
        }
        while (isdigit((unsigned char)peek(lexer))) {
            (void)take(lexer);
        }
    }
    return token_make(PALI_TOKEN_NUMBER, start, lexer->position - start_pos,
                      line, column);
}

static PaliToken lex_string(PaliLexer *lexer, const char *start, int line,
                            int column) {
    PaliToken token = token_make(PALI_TOKEN_STRING, start, 0, line, column);
    size_t written = 0;
    (void)take(lexer); /* opening quote */
    while (peek(lexer) != '"') {
        char value = take(lexer);
        if (value == '\0' || value == '\n' || value == '\r') {
            return token_error(start, line, column, "unterminated text literal");
        }
        if (value == '\\') {
            value = take(lexer);
            if (value == 'n') {
                value = '\n';
            } else if (value != '\\' && value != '"') {
                return token_error(start, line, column,
                                   "supported escapes are \\n, \\\\ and \\\"");
            }
        }
        if (written + 1 >= sizeof(token.decoded)) {
            return token_error(start, line, column, "text literal is too long");
        }
        token.decoded[written++] = value;
    }
    (void)take(lexer); /* closing quote */
    token.decoded[written] = '\0';
    token.length = lexer->position - (size_t)(start - lexer->source);
    return token;
}

PaliToken pali_lexer_next(PaliLexer *lexer) {
    skip_horizontal_space_and_comments(lexer);

    const size_t start_pos = lexer->position;
    const char *start = lexer->source + start_pos;
    const int line = lexer->line;
    const int column = lexer->column;
    char value = peek(lexer);

    if (value == '\0') {
        return token_make(PALI_TOKEN_EOF, start, 0, line, column);
    }
    if (value == '\r' || value == '\n') {
        if (value == '\r') {
            (void)take(lexer);
            if (peek(lexer) == '\n') {
                (void)take(lexer);
            }
        } else {
            (void)take(lexer);
        }
        lexer->line++;
        lexer->column = 1;
        return token_make(PALI_TOKEN_NEWLINE, start,
                          lexer->position - start_pos, line, column);
    }
    if (isalpha((unsigned char)value) || value == '_') {
        (void)take(lexer);
        return lex_identifier(lexer, start, start_pos, line, column);
    }
    if (isdigit((unsigned char)value)) {
        (void)take(lexer);
        return lex_number(lexer, start, start_pos, line, column);
    }
    if (value == '"') {
        return lex_string(lexer, start, line, column);
    }

    (void)take(lexer);
    switch (value) {
        case '(':
            return token_make(PALI_TOKEN_LEFT_PAREN, start, 1, line, column);
        case ')':
            return token_make(PALI_TOKEN_RIGHT_PAREN, start, 1, line, column);
        case '.':
            return token_make(PALI_TOKEN_DOT, start, 1, line, column);
        case ',':
            return token_make(PALI_TOKEN_COMMA, start, 1, line, column);
        case '=':
            return token_make(PALI_TOKEN_EQUAL, start, 1, line, column);
        case '+':
            return token_make(PALI_TOKEN_PLUS, start, 1, line, column);
        case '-':
            return token_make(PALI_TOKEN_MINUS, start, 1, line, column);
        case '*':
            return token_make(PALI_TOKEN_STAR, start, 1, line, column);
        case '/':
            return token_make(PALI_TOKEN_SLASH, start, 1, line, column);
        default:
            return token_error(start, line, column, "unexpected character");
    }
}
