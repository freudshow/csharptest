grammar Expr;

prog: stmt EOF;

stmt: assignStmt | expr;

assignStmt: RT_MARKER ASSIGN expr;

expr
    : LPAREN expr RPAREN                    // 括号
    | functionCall                          // 函数调用
    | unaryOp expr                          // 一元运算符
    | expr multOp expr                      // 乘除
    | expr addOp expr                       // 加减
    | expr shiftOp expr                     // 位移
    | expr compareOp expr                   // 比较
    | expr equalOp expr                     // 相等
    | expr bitAndOp expr                    // 位与
    | expr bitXorOp expr                    // 位异或
    | expr bitOrOp expr                     // 位或
    | expr logicAndOp expr                  // 逻辑与
    | expr logicOrOp expr                   // 逻辑或
    | atom                                  // 原子表达式
    ;

atom: NUMBER | RT_MARKER;

functionCall: funcName LPAREN expr RPAREN;

// 运算符规则
unaryOp: NOT | TILDE;
multOp: MULT | DIV;
addOp: PLUS | MINUS;
shiftOp: LSHIFT | RSHIFT;
compareOp: GT | GTE | LT | LTE;
equalOp: EQ;
bitAndOp: AMP;
bitXorOp: CARET;
bitOrOp: PIPE;
logicAndOp: AND;
logicOrOp: OR;
funcName: SIN | COS | EXP;

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
ASSIGN: '=';
WS: [ \t\r\n]+ -> skip;
ERROR_CHAR: . {throw new Exception($"非法字符:{Text}");};
