// eval.c
// gcc eval.c -lm -o eval
// Recursive-descent parser + evaluator following specified grammar and precedence

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <math.h>
#include <stdint.h>

typedef enum {
    T_NUM, T_HASH, T_IDENT,
    T_PLUS, T_MINUS, T_MUL, T_DIV,
    T_LP, T_RP,
    T_NOT, T_NEQ, T_ANDAND, T_OROR,
    T_GT, T_GTE, T_LT, T_LTE, T_EQ,
    T_AMP, T_PIPE, T_CARET, T_TILDE,
    T_LSHIFT, T_RSHIFT,
    T_ASSIGN,
    T_EOF, T_INVALID
} TokenType;

typedef struct {
    TokenType type;
    char *text; // for HASH or IDENT or number string
    double num;
    int pos; // start index in input
} Token;

// Simple dynamic token list
typedef struct { Token *arr; int sz; int cap; int idx; } TokenList;

static void tlist_init(TokenList *t) { t->sz = 0; t->cap = 16; t->arr = malloc(sizeof(Token)*t->cap); t->idx = 0; }
static void tlist_push(TokenList *t, Token tk) { if (t->sz==t->cap) { t->cap*=2; t->arr=realloc(t->arr,sizeof(Token)*t->cap); } t->arr[t->sz++]=tk; }
static Token tlist_peek(TokenList *t) { if (t->idx < t->sz) return t->arr[t->idx]; Token eof = {T_EOF,NULL,0}; return eof; }
static Token tlist_next(TokenList *t) { if (t->idx < t->sz) return t->arr[t->idx++]; Token eof = {T_EOF,NULL,0}; return eof; }
static void tlist_free(TokenList *t) { for (int i=0;i<t->sz;i++) if (t->arr[i].text) free(t->arr[i].text); free(t->arr); }

// RT map (simple vector)
typedef struct { int id; double val; } RtEntry;
typedef struct { RtEntry *arr; int sz; int cap; } RtMap;
static void rt_init(RtMap *m) { m->sz=0; m->cap=16; m->arr=malloc(sizeof(RtEntry)*m->cap); }
static void rt_set(RtMap *m, int id, double v) { for (int i=0;i<m->sz;i++) if (m->arr[i].id==id) { m->arr[i].val=v; return; } if (m->sz==m->cap) { m->cap*=2; m->arr=realloc(m->arr,sizeof(RtEntry)*m->cap); } m->arr[m->sz].id=id; m->arr[m->sz].val=v; m->sz++; }
static double rt_get(RtMap *m, int id) { for (int i=0;i<m->sz;i++) if (m->arr[i].id==id) return m->arr[i].val; return 0.0; }
static void rt_free(RtMap *m) { free(m->arr); }

// Tokenizer
static void tokenize(const char *s, TokenList *out) {
    int i=0; int n=(int)strlen(s);
    while (1) {
        while (i<n && isspace((unsigned char)s[i])) i++;
        if (i>=n) { Token t={T_EOF,NULL,0}; tlist_push(out,t); break; }
        char c = s[i];
        // multi-char tokens
        if (c=='&' && i+1<n && s[i+1]=='&') { Token t={T_ANDAND,strdup("&&"),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='|' && i+1<n && s[i+1]=='|') { Token t={T_OROR,strdup("||"),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='<' && i+1<n && s[i+1]=='<') { Token t={T_LSHIFT,strdup("<<"),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='>' && i+1<n && s[i+1]=='>') { Token t={T_RSHIFT,strdup(">>"),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='>' && i+1<n && s[i+1]=='=') { Token t={T_GTE,strdup(">="),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='<' && i+1<n && s[i+1]=='=') { Token t={T_LTE,strdup("<="),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='!' && i+1<n && s[i+1]=='=') { Token t={T_NEQ,strdup("!="),0,i}; tlist_push(out,t); i+=2; continue; }
        if (c=='=' && i+1<n && s[i+1]=='=') { Token t={T_EQ,strdup("=="),0,i}; tlist_push(out,t); i+=2; continue; }
        // numbers
        if (isdigit((unsigned char)c) || c=='.') {
            int start=i; while (i<n && (isdigit((unsigned char)s[i]) || s[i]=='.')) i++; int len = i-start; char *txt = strndup(s+start,len); double v = strtod(txt,NULL); Token t={T_NUM,txt,v}; tlist_push(out,t); continue; }
        if (c=='#') {
            i++; int start=i; while (i<n && isdigit((unsigned char)s[i])) i++; if (start==i) { Token t={T_INVALID,NULL,0}; tlist_push(out,t); break; } int len=i-start; char *txt = strndup(s+start,len); Token t={T_HASH,txt,0}; tlist_push(out,t); continue; }
        if (isalpha((unsigned char)c)) {
            int start=i; while (i<n && isalpha((unsigned char)s[i])) i++; int len=i-start; char *txt=strndup(s+start,len); Token t={T_IDENT,txt,0}; tlist_push(out,t); continue; }
        // single char
        switch (c) {
            case '+': { Token t={T_PLUS,strdup("+"),0,i}; tlist_push(out,t); i++; break; }
            case '-': { Token t={T_MINUS,strdup("-"),0,i}; tlist_push(out,t); i++; break; }
            case '*': { Token t={T_MUL,strdup("*"),0,i}; tlist_push(out,t); i++; break; }
            case '/': { Token t={T_DIV,strdup("/"),0,i}; tlist_push(out,t); i++; break; }
            case '(':{ Token t={T_LP,strdup("("),0,i}; tlist_push(out,t); i++; break; }
            case ')':{ Token t={T_RP,strdup(")"),0,i}; tlist_push(out,t); i++; break; }
            case '!':{ Token t={T_NOT,strdup("!"),0,i}; tlist_push(out,t); i++; break; }
            case '>':{ Token t={T_GT,strdup(">"),0,i}; tlist_push(out,t); i++; break; }
            case '<':{ Token t={T_LT,strdup("<"),0,i}; tlist_push(out,t); i++; break; }
            case '&':{ Token t={T_AMP,strdup("&"),0,i}; tlist_push(out,t); i++; break; }
            case '|':{ Token t={T_PIPE,strdup("|"),0,i}; tlist_push(out,t); i++; break; }
            case '^':{ Token t={T_CARET,strdup("^"),0,i}; tlist_push(out,t); i++; break; }
            case '~':{ Token t={T_TILDE,strdup("~"),0,i}; tlist_push(out,t); i++; break; }
            case '=':{ Token t={T_ASSIGN,strdup("="),0,i}; tlist_push(out,t); i++; break; }
            default: { Token t={T_INVALID,NULL,0}; tlist_push(out,t); i++; break; }
        }
    }
}

// Parser forward declarations
static double parse_assign(TokenList *toks, RtMap *rt);
static double parse_logical_or(TokenList *toks, RtMap *rt);
static double parse_logical_and(TokenList *toks, RtMap *rt);
static double parse_bitor(TokenList *toks, RtMap *rt);
static double parse_bitxor(TokenList *toks, RtMap *rt);
static double parse_bitand(TokenList *toks, RtMap *rt);
static double parse_equality(TokenList *toks, RtMap *rt);
static double parse_relational(TokenList *toks, RtMap *rt);
static double parse_shift(TokenList *toks, RtMap *rt);
static double parse_add(TokenList *toks, RtMap *rt);
static double parse_multiply(TokenList *toks, RtMap *rt);
static double parse_unary(TokenList *toks, RtMap *rt);
static double parse_power(TokenList *toks, RtMap *rt);
static double parse_primary(TokenList *toks, RtMap *rt);

// helpers
static int match(TokenList *t, TokenType ty) { if (tlist_peek(t).type==ty) { tlist_next(t); return 1; } return 0; }

// ParseAssign: assignment right-assoc; only HASH allowed as left-value
static double parse_assign(TokenList *toks, RtMap *rt) {
    Token cur = tlist_peek(toks);
    if (cur.type==T_HASH) {
        // lookahead
        if (toks->idx+1 < toks->sz && toks->arr[toks->idx+1].type==T_ASSIGN) {
            Token h = tlist_next(toks); // consume HASH
            tlist_next(toks); // consume ASSIGN
            double rhs = parse_assign(toks, rt);
            int id = atoi(h.text);
            rt_set(rt, id, rhs);
            return rhs;
        }
    }
    return parse_logical_or(toks, rt);
}

static double parse_logical_or(TokenList *toks, RtMap *rt) {
    double left = parse_logical_and(toks, rt);
    while (match(toks, T_OROR)) {
        if (left != 0.0) {
            // still parse RHS to consume tokens
            parse_logical_and(toks, rt);
            left = 1.0;
        } else {
            double r = parse_logical_and(toks, rt);
            left = (r != 0.0) ? 1.0 : 0.0;
        }
    }
    return left;
}

static double parse_logical_and(TokenList *toks, RtMap *rt) {
    double left = parse_bitor(toks, rt);
    while (match(toks, T_ANDAND)) {
        if (left == 0.0) {
            parse_bitor(toks, rt);
            left = 0.0;
        } else {
            double r = parse_bitor(toks, rt);
            left = (r != 0.0) ? 1.0 : 0.0;
        }
    }
    return left;
}

static double parse_bitor(TokenList *toks, RtMap *rt) {
    double left = parse_bitxor(toks, rt);
    while (match(toks, T_PIPE)) {
        double r = parse_bitxor(toks, rt);
        left = (double)((long)left | (long)r);
    }
    return left;
}

static double parse_bitxor(TokenList *toks, RtMap *rt) {
    double left = parse_bitand(toks, rt);
    while (match(toks, T_CARET)) {
        double r = parse_bitand(toks, rt);
        left = (double)((long)left ^ (long)r);
    }
    return left;
}

static double parse_bitand(TokenList *toks, RtMap *rt) {
    double left = parse_equality(toks, rt);
    while (match(toks, T_AMP)) {
        double r = parse_equality(toks, rt);
        left = (double)((long)left & (long)r);
    }
    return left;
}

static double parse_equality(TokenList *toks, RtMap *rt) {
    double left = parse_relational(toks, rt);
    while (1) {
        if (match(toks, T_EQ)) { double r = parse_relational(toks, rt); left = left == r ? 1.0 : 0.0; }
        else if (match(toks, T_NEQ)) { double r = parse_relational(toks, rt); left = left != r ? 1.0 : 0.0; }
        else break;
    }
    return left;
}

static double parse_relational(TokenList *toks, RtMap *rt) {
    double left = parse_shift(toks, rt);
    while (1) {
        if (match(toks, T_GT)) { double r = parse_shift(toks, rt); left = left > r ? 1.0 : 0.0; }
        else if (match(toks, T_GTE)) { double r = parse_shift(toks, rt); left = left >= r ? 1.0 : 0.0; }
        else if (match(toks, T_LT)) { double r = parse_shift(toks, rt); left = left < r ? 1.0 : 0.0; }
        else if (match(toks, T_LTE)) { double r = parse_shift(toks, rt); left = left <= r ? 1.0 : 0.0; }
        else break;
    }
    return left;
}

static double parse_shift(TokenList *toks, RtMap *rt) {
    double left = parse_add(toks, rt);
    while (1) {
        if (match(toks, T_LSHIFT)) { double r = parse_add(toks, rt); left = (double)((long)left << (int)r); }
        else if (match(toks, T_RSHIFT)) { double r = parse_add(toks, rt); left = (double)((long)left >> (int)r); }
        else break;
    }
    return left;
}

static double parse_add(TokenList *toks, RtMap *rt) {
    double left = parse_multiply(toks, rt);
    while (1) {
        if (match(toks, T_PLUS)) { double r = parse_multiply(toks, rt); left += r; }
        else if (match(toks, T_MINUS)) { double r = parse_multiply(toks, rt); left -= r; }
        else break;
    }
    return left;
}

static double parse_multiply(TokenList *toks, RtMap *rt) {
    double left = parse_unary(toks, rt);
    while (1) {
        if (match(toks, T_MUL)) { double r = parse_unary(toks, rt); left *= r; }
        else if (match(toks, T_DIV)) { double r = parse_unary(toks, rt); if (r==0.0) { fprintf(stderr,"Division by zero\n"); exit(1); } left /= r; }
        else break;
    }
    return left;
}

static double parse_unary(TokenList *toks, RtMap *rt) {
    if (match(toks, T_NOT)) { double v = parse_unary(toks, rt); return v != 0.0 ? 0.0 : 1.0; }
    if (match(toks, T_TILDE)) { double v = parse_unary(toks, rt); return (double)(~((long)v)); }
    if (match(toks, T_MINUS)) { double v = parse_unary(toks, rt); return -v; }
    return parse_power(toks, rt);
}

// power handles exp (highest), sin/cos (next), or primary
static double parse_power(TokenList *toks, RtMap *rt) {
    Token cur = tlist_peek(toks);
    if (cur.type==T_IDENT && strcmp(cur.text, "exp")==0) {
        tlist_next(toks); // consume 'exp'
        if (!match(toks, T_LP)) { fprintf(stderr,"Expected ( after exp\n"); exit(1); }
        double a = parse_assign(toks, rt);
        if (!match(toks, T_RP)) { fprintf(stderr,"Expected ) after exp\n"); exit(1); }
        return exp(a);
    }
    if (cur.type==T_IDENT && (strcmp(cur.text, "sin")==0 || strcmp(cur.text, "cos")==0)) {
        tlist_next(toks);
        if (!match(toks, T_LP)) { fprintf(stderr,"Expected ( after %s\n", cur.text); exit(1); }
        double a = parse_assign(toks, rt);
        if (!match(toks, T_RP)) { fprintf(stderr,"Expected ) after %s\n", cur.text); exit(1); }
        if (strcmp(cur.text, "sin")==0) return sin(a);
        return cos(a);
    }
    return parse_primary(toks, rt);
}

static double parse_primary(TokenList *toks, RtMap *rt) {
    Token t = tlist_peek(toks);
    if (t.type==T_NUM) { Token tk = tlist_next(toks); return tk.num; }
    if (t.type==T_HASH) { Token tk = tlist_next(toks); int id = atoi(tk.text); return rt_get(rt, id); }
    if (t.type==T_IDENT) {
        // allow function names handled in parse_power; unknown ident error
        fprintf(stderr,"Unexpected identifier: %s\n", t.text); exit(1);
    }
    if (match(toks, T_LP)) {
        double v = parse_assign(toks, rt);
        if (!match(toks, T_RP)) { fprintf(stderr,"Expected )\n"); exit(1); }
        return v;
    }
    fprintf(stderr,"Unexpected token in primary\n"); exit(1);
}

int main(void) {
    char buf[4096];
    RtMap rt; rt_init(&rt);
    printf("expr> ");
    while (fgets(buf, sizeof(buf), stdin)) {
        if (buf[0]=='\n' || buf[0]==0) break;
        TokenList toks; tlist_init(&toks);
        tokenize(buf, &toks);
        // check invalid
        int invalid = 0;
        for (int i=0;i<toks.sz;i++) if (toks.arr[i].type==T_INVALID) { invalid=1; break; }
        if (invalid) { fprintf(stderr,"Invalid character in input\n"); tlist_free(&toks); printf("expr> "); continue; }
        // parse
        // reset index
        toks.idx = 0;
        // parse statement via parse_assign entry
        double result = 0.0;
        // detect assignment at top-level: if HASH ASSIGN ... then parse_assign will do assignment
        result = parse_assign(&toks, &rt);
        Token after = tlist_peek(&toks);
        if (after.type != T_EOF) { fprintf(stderr,"Syntax error: unexpected token\n"); }
        else {
            printf("%g\n", result);
        }
        tlist_free(&toks);
        printf("expr> ");
    }
    rt_free(&rt);
    return 0;
}
