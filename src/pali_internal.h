#ifndef PALIMPSEST_PALI_INTERNAL_H
#define PALIMPSEST_PALI_INTERNAL_H

#include "pali.h"

typedef enum PaliTokenType {
    PALI_TOKEN_EOF = 0,
    PALI_TOKEN_NEWLINE,
    PALI_TOKEN_IDENTIFIER,
    PALI_TOKEN_NUMBER,
    PALI_TOKEN_STRING,
    PALI_TOKEN_LEFT_PAREN,
    PALI_TOKEN_RIGHT_PAREN,
    PALI_TOKEN_DOT,
    PALI_TOKEN_COMMA,
    PALI_TOKEN_EQUAL,
    PALI_TOKEN_PLUS,
    PALI_TOKEN_MINUS,
    PALI_TOKEN_STAR,
    PALI_TOKEN_SLASH,
    PALI_TOKEN_ERROR
} PaliTokenType;

typedef struct PaliToken {
    PaliTokenType type;
    const char *start;
    size_t length;
    int line;
    int column;
    char decoded[PALI_TEXT_CAP];
} PaliToken;

typedef struct PaliLexer {
    const char *source;
    size_t position;
    int line;
    int column;
} PaliLexer;

void pali_lexer_init(PaliLexer *lexer, const char *source);
PaliToken pali_lexer_next(PaliLexer *lexer);

#endif
