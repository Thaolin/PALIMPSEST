#include "pali.h"

#include <math.h>
#include <stdio.h>
#include <string.h>

static bool bounded_text(const char *text, size_t capacity) {
    return text != NULL && memchr(text, '\0', capacity) != NULL;
}

static bool runtime_value_is_valid(PaliValue value) {
    switch (value.type) {
        case PALI_VALUE_NUMBER:
            return isfinite(value.as.number);
        case PALI_VALUE_BOOL:
            return true;
        case PALI_VALUE_TEXT:
            return bounded_text(value.as.text, sizeof(value.as.text));
        default:
            return false;
    }
}

PaliValue pali_number(double value) {
    PaliValue result;
    memset(&result, 0, sizeof(result));
    result.type = PALI_VALUE_NUMBER;
    result.as.number = value;
    return result;
}

PaliValue pali_bool(bool value) {
    PaliValue result;
    memset(&result, 0, sizeof(result));
    result.type = PALI_VALUE_BOOL;
    result.as.boolean = value;
    return result;
}

PaliValue pali_text(const char *value) {
    PaliValue result;
    memset(&result, 0, sizeof(result));
    result.type = PALI_VALUE_TEXT;
    (void)snprintf(result.as.text, sizeof(result.as.text), "%.79s",
                   value != NULL ? value : "");
    return result;
}

bool pali_value_equal(PaliValue left, PaliValue right) {
    if (left.type != right.type) {
        return false;
    }
    switch (left.type) {
        case PALI_VALUE_NIL:
            return true;
        case PALI_VALUE_NUMBER:
            return left.as.number == right.as.number;
        case PALI_VALUE_BOOL:
            return left.as.boolean == right.as.boolean;
        case PALI_VALUE_TEXT: {
            const bool left_bounded =
                memchr(left.as.text, '\0', sizeof(left.as.text)) != NULL;
            const bool right_bounded =
                memchr(right.as.text, '\0', sizeof(right.as.text)) != NULL;
            return left_bounded && right_bounded &&
                   strcmp(left.as.text, right.as.text) == 0;
        }
        default:
            return false;
    }
}

const PaliValue *pali_program_property(const PaliProgram *program,
                                       const char *name) {
    if (program == NULL || name == NULL ||
        program->property_count > PALI_MAX_PROPERTIES) {
        return NULL;
    }
    for (uint16_t index = 0; index < program->property_count; ++index) {
        if (!bounded_text(program->properties[index].name,
                          sizeof(program->properties[index].name))) {
            return NULL;
        }
        if (strcmp(program->properties[index].name, name) == 0) {
            return &program->properties[index].value;
        }
    }
    return NULL;
}

const PaliValue *pali_document_property(const PaliDocument *document,
                                        const char *name) {
    if (document == NULL || name == NULL ||
        document->property_count > PALI_MAX_PROPERTIES) {
        return NULL;
    }
    for (uint16_t index = 0; index < document->property_count; ++index) {
        if (!bounded_text(document->properties[index].name,
                          sizeof(document->properties[index].name))) {
            return NULL;
        }
        if (strcmp(document->properties[index].name, name) == 0) {
            return &document->properties[index].value;
        }
    }
    return NULL;
}

static bool runtime_error(PaliError *error, uint16_t line,
                          const char *message) {
    if (error != NULL) {
        error->line = (int)line;
        error->column = 1;
        (void)snprintf(error->message, sizeof(error->message), "%s", message);
    }
    return false;
}

static bool program_is_valid(const PaliProgram *program, PaliError *error) {
    if (program->property_count > PALI_MAX_PROPERTIES ||
        program->constant_count > PALI_MAX_CONSTANTS ||
        program->code_count > PALI_MAX_CODE) {
        return runtime_error(error, 0,
                             "PALI program exceeds a fixed capacity");
    }
    for (uint16_t index = 0; index < program->property_count; ++index) {
        if (!bounded_text(program->properties[index].name,
                          sizeof(program->properties[index].name)) ||
            program->properties[index].name[0] == '\0' ||
            !runtime_value_is_valid(program->properties[index].value)) {
            return runtime_error(error, 0,
                                 "PALI program has an invalid property");
        }
    }
    for (uint16_t index = 0; index < program->constant_count; ++index) {
        if (!runtime_value_is_valid(program->constants[index])) {
            return runtime_error(error, 0,
                                 "PALI program has an invalid constant");
        }
    }
    return true;
}

static bool push(PaliValue *stack, int *count, PaliValue value,
                 PaliError *error, uint16_t line) {
    if (*count >= PALI_MAX_STACK) {
        return runtime_error(error, line, "PALI stack limit exceeded");
    }
    stack[(*count)++] = value;
    return true;
}

static bool pop(PaliValue *stack, int *count, PaliValue *out,
                PaliError *error, uint16_t line) {
    if (*count <= 0) {
        return runtime_error(error, line, "PALI stack underflow");
    }
    *out = stack[--(*count)];
    return true;
}

static bool binary_number(PaliValue *stack, int *count, PaliOp op,
                          PaliError *error, uint16_t line) {
    PaliValue right;
    PaliValue left;
    if (!pop(stack, count, &right, error, line) ||
        !pop(stack, count, &left, error, line)) {
        return false;
    }
    if (left.type != PALI_VALUE_NUMBER || right.type != PALI_VALUE_NUMBER) {
        return runtime_error(error, line, "arithmetic requires numeric values");
    }
    double value = 0.0;
    switch (op) {
        case PALI_OP_ADD:
            value = left.as.number + right.as.number;
            break;
        case PALI_OP_SUBTRACT:
            value = left.as.number - right.as.number;
            break;
        case PALI_OP_MULTIPLY:
            value = left.as.number * right.as.number;
            break;
        case PALI_OP_DIVIDE:
            if (right.as.number == 0.0) {
                return runtime_error(error, line, "division by zero");
            }
            value = left.as.number / right.as.number;
            break;
        case PALI_OP_MIN:
            value = fmin(left.as.number, right.as.number);
            break;
        case PALI_OP_MAX:
            value = fmax(left.as.number, right.as.number);
            break;
        default:
            return runtime_error(error, line, "invalid numeric instruction");
    }
    if (!isfinite(value)) {
        return runtime_error(error, line, "arithmetic produced a non-finite value");
    }
    return push(stack, count, pali_number(value), error, line);
}

bool pali_run_use(const PaliProgram *program, PaliHost *host, int budget,
                  PaliError *error) {
    if (error != NULL) {
        memset(error, 0, sizeof(*error));
    }
    if (program == NULL || host == NULL || host->get_property == NULL ||
        host->set_property == NULL || host->call == NULL) {
        return runtime_error(error, 0, "PALI host binding is incomplete");
    }
    if (!program_is_valid(program, error)) {
        return false;
    }
    if (!program->has_use) {
        return true;
    }
    if (budget <= 0) {
        budget = PALI_DEFAULT_BUDGET;
    }

    PaliValue stack[PALI_MAX_STACK];
    int stack_count = 0;
    for (uint16_t ip = 0; ip < program->code_count; ++ip) {
        const PaliInstruction instruction = program->code[ip];
        if (--budget < 0) {
            return runtime_error(error, instruction.line,
                                 "execution budget exhausted");
        }
        if (instruction.operand >= program->constant_count &&
            (instruction.op == PALI_OP_CONSTANT ||
             instruction.op == PALI_OP_GET_SELF ||
             instruction.op == PALI_OP_GET_ACTOR ||
             instruction.op == PALI_OP_SET_SELF ||
             instruction.op == PALI_OP_SET_ACTOR)) {
            return runtime_error(error, instruction.line,
                                 "invalid constant reference");
        }

        PaliValue value;
        PaliValue argument;
        const PaliValue *name;
        switch ((PaliOp)instruction.op) {
            case PALI_OP_CONSTANT:
                if (!push(stack, &stack_count,
                          program->constants[instruction.operand], error,
                          instruction.line)) {
                    return false;
                }
                break;
            case PALI_OP_GET_SELF:
            case PALI_OP_GET_ACTOR:
                name = &program->constants[instruction.operand];
                if (name->type != PALI_VALUE_TEXT) {
                    return runtime_error(error, instruction.line,
                                         "property name is not text");
                }
                if (!host->get_property(host->user,
                                        instruction.op == PALI_OP_GET_SELF
                                            ? PALI_TARGET_SELF
                                            : PALI_TARGET_ACTOR,
                                        name->as.text, &value, error)) {
                    if (error != NULL && error->line == 0) {
                        error->line = (int)instruction.line;
                    }
                    return false;
                }
                if (!push(stack, &stack_count, value, error, instruction.line)) {
                    return false;
                }
                break;
            case PALI_OP_SET_SELF:
            case PALI_OP_SET_ACTOR:
                name = &program->constants[instruction.operand];
                if (!pop(stack, &stack_count, &value, error, instruction.line)) {
                    return false;
                }
                if (name->type != PALI_VALUE_TEXT ||
                    !host->set_property(host->user,
                                        instruction.op == PALI_OP_SET_SELF
                                            ? PALI_TARGET_SELF
                                            : PALI_TARGET_ACTOR,
                                        name->as.text, value, error)) {
                    if (error != NULL && error->line == 0) {
                        error->line = (int)instruction.line;
                    }
                    return false;
                }
                break;
            case PALI_OP_ADD:
            case PALI_OP_SUBTRACT:
            case PALI_OP_MULTIPLY:
            case PALI_OP_DIVIDE:
            case PALI_OP_MIN:
            case PALI_OP_MAX:
                if (!binary_number(stack, &stack_count,
                                   (PaliOp)instruction.op, error,
                                   instruction.line)) {
                    return false;
                }
                break;
            case PALI_OP_NEGATE:
                if (!pop(stack, &stack_count, &value, error, instruction.line)) {
                    return false;
                }
                if (value.type != PALI_VALUE_NUMBER) {
                    return runtime_error(error, instruction.line,
                                         "negation requires a number");
                }
                value.as.number = -value.as.number;
                if (!push(stack, &stack_count, value, error, instruction.line)) {
                    return false;
                }
                break;
            case PALI_OP_DESTROY:
                memset(&argument, 0, sizeof(argument));
                if (!host->call(host->user, PALI_HOST_DESTROY, &argument,
                                error)) {
                    if (error != NULL && error->line == 0) {
                        error->line = (int)instruction.line;
                    }
                    return false;
                }
                break;
            case PALI_OP_MESSAGE:
                if (!pop(stack, &stack_count, &argument, error,
                         instruction.line)) {
                    return false;
                }
                if (!host->call(host->user, PALI_HOST_MESSAGE, &argument,
                                error)) {
                    if (error != NULL && error->line == 0) {
                        error->line = (int)instruction.line;
                    }
                    return false;
                }
                break;
            case PALI_OP_POP:
                if (!pop(stack, &stack_count, &value, error, instruction.line)) {
                    return false;
                }
                break;
            case PALI_OP_RETURN:
                return true;
            default:
                return runtime_error(error, instruction.line,
                                     "unknown PALI instruction");
        }
    }
    return true;
}
