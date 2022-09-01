parser grammar YabalParser;

options { tokenVocab = YabalLexer; }

program
    : (statement (eos statement)*)? eos? EOF
    ;

rawType
    : Int           # IntType
    | Bool          # BoolType
    | Identifier    # ClassType
    ;

type
    : rawType                           # DefaultType
    | type OpenBracket CloseBracket     # ArrayType
    ;

returnType
    : type  # DefaultReturnType
    | Void  # VoidReturnType
    ;

expression
	: Incr expression												    # IncrementLeftExpression
	| expression Incr												    # IncrementRightExpression
	| Decr expression												    # DecrementLeftExpression
	| expression Decr												    # DecrementRightExpression
	| Sub expression												    # MinusExpression
	| Not expression												    # NotExpression
	| Exclamation expression										    # NegateExpression
	| expression inlineSwitch                                           # SwitchExpression
	| expression {noNewLine()}? OpenBrace expressionList CloseBrace	    # CallExpression
	| expression {noNewLine()}? OpenBracket expression CloseBracket	    # ArrayAccessExpression
	| expression NotEquals expression								    # NotEqualExpression
	| expression AddEqual expression								    # PlusEqualExpression
	| expression SubEqual expression								    # SubEqualExpression
	| expression MulEqual expression								    # MulEqualExpression
	| expression DivEqual expression								    # DivEqualExpression
    | expression (Div|Mul|Mod) expression							    # DivMulModBinaryExpression
    | expression (Add|Sub) expression								    # PlusSubBinaryExpression
	| expression (ShiftLeft|ShiftRight) expression						# ShiftExpression
	| expression (GreaterEqual|Greater|LessEqual|Less) expression		# ComparisonExpression
	| expression (Equals|NotEquals) expression						    # EqualExpression
	| expression And expression									        # AndExpression
	| expression Or expression									        # OrExpression
	| expression AndAlso expression								        # AndAlsoExpression
	| expression OrElse expression								        # OrElseExpression
	| expression QuestionMark expression Colon expression               # TernaryExpression
	| expression Assign expression                                      # AssignExpression
	| Asm OpenCurly asmItems CloseCurly                                 # AsmExpression
	| Throw expression												    # ThrowExpression
	| CreatePointer expression                                          # CreatePointerExpression
	| SizeOf expression                                                 # SizeOfExpression
	| OpenBrace expression CloseBrace								    # ExpressionExpression
    | string                                                            # StringExpression
    | char                                                              # CharExpression
	| integer                       								    # IntegerExpression
	| boolean                       								    # BooleanExpression
	| identifierName										            # IdentifierExpression
    ;

inlineSwitch
    : Switch OpenCurly (inlineSwitchItem (Comma inlineSwitchItem)* Comma?)? CloseCurly
    ;

inlineSwitchItem
    : underscore Arrow expression
    | expressionList Arrow expression
    ;

underscore
    : Underscore
    ;

// Statements
statement
    : variableDeclaration
	| returnStatement
	| whileStatement
	| forStatement
	| ifStatement
	| continueStatement
	| breakStatement
	| expressionStatement
    ;

asmItems
    : (asmStatementItem (eos asmStatementItem)*)? eos?
    ;

asmStatementItem
    : asmIdentifier {noNewLine()}? asmArgument?      # AsmInstruction
    | asmIdentifier {noNewLine()}? AsmColon          # AsmLabel
    ;

asmArgument
    : AsmAddress asmIdentifier     # AsmAddress
    | IntegerLiteral               # AsmInteger
    | asmIdentifier                # AsmLabelReference
    ;

asmIdentifier
    : Identifier
    ;

continueStatement
    : Continue
    ;

breakStatement
    : Break
    ;

returnStatement
    : Return expression?
    ;

expressionStatement
	: expression
	;

whileStatement
	: While OpenBrace expression CloseBrace blockStatement
	;

// If-statement
ifStatement
	: If OpenBrace expression CloseBrace blockStatement elseIfStatement* elseStatement?
	;

elseIfStatement
	: Else If OpenBrace expression CloseBrace blockStatement
	;

elseStatement
	: Else blockStatement
	;

// For-statement
forStatement
	: For OpenBrace forInit? SemiColon expression SemiColon statement? CloseBrace blockStatement
	;

forInit
    : statement
    ;

// Utils
expressionList
    : (expression (Comma expression)*)?
    ;

blockStatement
	: OpenCurly (statement (eos statement)*)? eos? CloseCurly
	;

// Function
functionBody
	: blockStatement
	| Arrow expression
	;

// Variable
variableDeclaration
    : Const? type identifierName (Assign expression)?               # DefaultVariableDeclaration
    | Const? Var identifierName Assign expression                   # AutoVariableDeclaration
    | returnType identifierName functionParameterList functionBody  # FunctionDeclaration
    ;

// Identifier
identifierName
	: Identifier
	| Underscore
	;

functionParameterList
    : OpenBrace (functionParameter (Comma functionParameter)*)? CloseBrace
    ;

functionParameter
	: type identifierName (Assign expression)?
	;

// Literal
boolean
    : True      # True
    | False     # False
    ;

integer
	: (Add|Sub)? IntegerLiteral
	;

eos
    : SemiColon
    | {lineTerminatorAhead()}?
    ;

string
    : StringStart stringPart* StringEnd
    ;

stringPart
	: StringValue
	| StringEscape
	;

char
    : CharStart charValue CharEnd
    ;

charValue
	: CharEscape
	| CharValue
	;
