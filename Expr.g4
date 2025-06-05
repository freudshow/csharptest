grammar Expr;

prog: expr EOF;

expr: orExpr;

orExpr: andExpr (OR andExpr)*;

andExpr: bitOrExpr (AND bitOrExpr)*;

bitOrExpr: bitXorExpr (PIPE bitXorExpr)*;

bitXorExpr: bitAndExpr (CARET bitAndExpr)*;

bitAndExpr: equalityExpr (AMP equalityExpr)*;

equalityExpr: relationalExpr (EQ relationalExpr)*;

relationalExpr: shiftExpr ((GT | GTE | LT | LTE) shiftExpr)*;

shiftExpr: additiveExpr ((LSHIFT | RSHIFT) additiveExpr)*;

additiveExpr: multiplicativeExpr ((PLUS | MINUS) multiplicativeExpr)*;

multiplicativeExpr: unaryExpr ((MULT | DIV) unaryExpr)*;

unaryExpr: (NOT | TILDE | MINUS) unaryExpr | primaryExpr;

primaryExpr: NUMBER | RT_MARKER | LPAREN expr RPAREN | functionCall;

functionCall: (SIN | COS | EXP) LPAREN expr RPAREN;

// Lexer rules
SIN: 'sin';
COS: 'cos';
EXP: 'exp';
PLUS: '+';
MINUS: '-';
MULT: '*';
DIV: '/';
NOT: '!';
AND: '&&';
OR: '||';
GT: '>';
GTE: '>=';
LT: '<';
LTE: '<=';
EQ: '==';
AMP: '&';
PIPE: '|';
CARET: '^';
TILDE: '~';
LSHIFT: '<<';
RSHIFT: '>>';
LPAREN: '(';
RPAREN: ')';
NUMBER: [0-9]+ ('.' [0-9]*)? | '.' [0-9]+;
RT_MARKER: '#' [0-9]+;
WS: [ \t\r\n]+ -> skip;