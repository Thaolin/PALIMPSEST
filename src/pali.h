#ifndef PALIMPSEST_PALI_H
#define PALIMPSEST_PALI_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

/* Hard bounds are part of the host/fiction trust boundary. */
#define PALI_NAME_CAP 24
#define PALI_TEXT_CAP 80
#define PALI_ERROR_CAP 160
#define PALI_SOURCE_CAP 4096
#define PALI_MAX_PROPERTIES 16
#define PALI_MAX_CONSTANTS 32
#define PALI_MAX_CODE 128
#define PALI_MAX_STACK 48
#define PALI_MAX_DOCUMENT_NAMES 32
#define PALI_MAX_EXPRESSIONS 64
#define PALI_MAX_STATEMENTS 16
#define PALI_NODE_NONE UINT8_MAX
#define PALI_DEFAULT_BUDGET 256

typedef enum PaliValueType {
    PALI_VALUE_NIL = 0,
    PALI_VALUE_NUMBER,
    PALI_VALUE_BOOL,
    PALI_VALUE_TEXT
} PaliValueType;

typedef struct PaliValue {
    PaliValueType type;
    union {
        double number;
        bool boolean;
        char text[PALI_TEXT_CAP];
    } as;
} PaliValue;

typedef struct PaliProperty {
    char name[PALI_NAME_CAP];
    PaliValue value;
} PaliProperty;

typedef enum PaliExpressionKind {
    PALI_EXPRESSION_LITERAL = 0,
    PALI_EXPRESSION_GET_SELF,
    PALI_EXPRESSION_GET_ACTOR,
    PALI_EXPRESSION_ADD,
    PALI_EXPRESSION_SUBTRACT,
    PALI_EXPRESSION_MULTIPLY,
    PALI_EXPRESSION_DIVIDE,
    PALI_EXPRESSION_NEGATE,
    PALI_EXPRESSION_MIN,
    PALI_EXPRESSION_MAX
} PaliExpressionKind;

typedef struct PaliExpression {
    uint8_t kind;
    uint8_t left;
    uint8_t right;
    uint8_t operand;
    uint16_t line;
} PaliExpression;

typedef enum PaliStatementKind {
    PALI_STATEMENT_SET_SELF = 0,
    PALI_STATEMENT_SET_ACTOR,
    PALI_STATEMENT_DESTROY_SELF,
    PALI_STATEMENT_MESSAGE
} PaliStatementKind;

typedef struct PaliStatement {
    uint8_t kind;
    uint8_t name;
    uint8_t expression;
    uint8_t reserved;
    uint16_t line;
} PaliStatement;

typedef struct PaliDocument {
    char prototype_name[PALI_NAME_CAP];
    PaliProperty properties[PALI_MAX_PROPERTIES];
    PaliValue constants[PALI_MAX_CONSTANTS];
    char names[PALI_MAX_DOCUMENT_NAMES][PALI_NAME_CAP];
    PaliExpression expressions[PALI_MAX_EXPRESSIONS];
    PaliStatement statements[PALI_MAX_STATEMENTS];
    uint16_t property_count;
    uint16_t constant_count;
    uint16_t name_count;
    uint16_t expression_count;
    uint16_t statement_count;
    bool has_use;
} PaliDocument;

typedef struct PaliError {
    int line;
    int column;
    char message[PALI_ERROR_CAP];
} PaliError;

typedef enum PaliOp {
    PALI_OP_CONSTANT = 0,
    PALI_OP_GET_SELF,
    PALI_OP_GET_ACTOR,
    PALI_OP_SET_SELF,
    PALI_OP_SET_ACTOR,
    PALI_OP_ADD,
    PALI_OP_SUBTRACT,
    PALI_OP_MULTIPLY,
    PALI_OP_DIVIDE,
    PALI_OP_NEGATE,
    PALI_OP_MIN,
    PALI_OP_MAX,
    PALI_OP_DESTROY,
    PALI_OP_MESSAGE,
    PALI_OP_POP,
    PALI_OP_RETURN
} PaliOp;

typedef struct PaliInstruction {
    uint8_t op;
    uint8_t operand;
    uint16_t line;
} PaliInstruction;

typedef struct PaliProgram {
    char prototype_name[PALI_NAME_CAP];
    PaliProperty properties[PALI_MAX_PROPERTIES];
    PaliValue constants[PALI_MAX_CONSTANTS];
    PaliInstruction code[PALI_MAX_CODE];
    uint16_t property_count;
    uint16_t constant_count;
    uint16_t code_count;
    bool has_use;
} PaliProgram;

typedef enum PaliTarget {
    PALI_TARGET_SELF = 0,
    PALI_TARGET_ACTOR
} PaliTarget;

typedef enum PaliHostCall {
    PALI_HOST_DESTROY = 0,
    PALI_HOST_MESSAGE
} PaliHostCall;

typedef struct PaliHost {
    void *user;
    bool (*get_property)(void *user, PaliTarget target, const char *name,
                         PaliValue *out, PaliError *error);
    bool (*set_property)(void *user, PaliTarget target, const char *name,
                         PaliValue value, PaliError *error);
    bool (*call)(void *user, PaliHostCall call, const PaliValue *argument,
                 PaliError *error);
} PaliHost;

PaliValue pali_number(double value);
PaliValue pali_bool(bool value);
PaliValue pali_text(const char *value);
bool pali_value_equal(PaliValue left, PaliValue right);

bool pali_compile(const char *source, PaliProgram *out, PaliError *error);
bool pali_parse_document(const char *source, PaliDocument *out,
                         PaliError *error);
bool pali_compile_document(const PaliDocument *document, PaliProgram *out,
                           PaliError *error);
bool pali_format_document(const PaliDocument *document, char *out,
                          size_t capacity, PaliError *error);
bool pali_run_use(const PaliProgram *program, PaliHost *host, int budget,
                  PaliError *error);
const PaliValue *pali_program_property(const PaliProgram *program,
                                       const char *name);
const PaliValue *pali_document_property(const PaliDocument *document,
                                        const char *name);

#endif
