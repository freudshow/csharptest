grammar Expr;

prog: stmt EOF;

stmt: assignStmt | expr;

assignStmt: RT_MARKER ASSIGN expr;

expr
    : LPAREN expr RPAREN                    // ����
    | functionCall                          // ��������
    | unaryOp expr                          // һԪ�����
    | expr multOp expr                      // �˳�
    | expr addOp expr                       // �Ӽ�
    | expr shiftOp expr                     // λ��
    | expr compareOp expr                   // �Ƚ�
    | expr equalOp expr                     // ���
    | expr bitAndOp expr                    // λ��
    | expr bitXorOp expr                    // λ���
    | expr bitOrOp expr                     // λ��
    | expr logicAndOp expr                  // �߼���
    | expr logicOrOp expr                   // �߼���
    | atom                                  // ԭ�ӱ��ʽ
    ;

atom: NUMBER | RT_MARKER;

functionCall: funcName LPAREN expr RPAREN;

// ���������
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
ERROR_CHAR: . {throw new Exception($"�Ƿ��ַ�:{Text}");};
