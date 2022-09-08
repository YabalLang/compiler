parser grammar YabalParser;

options { tokenVocab = YabalLexer; }

program
    : (statement (eos statement)*)? eos? EOF
    ;

rawType
    : Int            # IntType
    | Bool           # BoolType
    | identifierName # StructType
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
	: createPointer                                                     # CreatePointerExpression
	| IncludeBytes expression                                           # IncludeBytesExpression
	| IncludeImage expression                                           # IncludeImageExpression
	| SizeOf expression                                                 # SizeOfExpression
	| expression Dot identifierName                                     # MemberExpression
	| Incr expression												    # IncrementLeftExpression
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
	| OpenBrace expression CloseBrace								    # ExpressionExpression
    | string                                                            # StringExpression
    | char                                                              # CharExpression
	| integer                       								    # IntegerExpression
	| boolean                       								    # BooleanExpression
	| identifierName										            # IdentifierExpression
	| initStruct                                                        # InitStructExpression
    ;

initStructItem
    : (identifierName Colon)? expression
    ;

initStruct
    : type? OpenCurly (initStructItem (Comma initStructItem)*)? CloseCurly
    ;

createPointer
    : CreatePointer (Less type Greater)? OpenBrace expression (Comma integer)? CloseBrace
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
    | structDeclaration
	| returnStatement
	| whileStatement
	| forStatement
	| ifStatement
	| continueStatement
	| breakStatement
	| expressionStatement
	| gotoStatement
	| labelStatement
    ;

structDeclaration
    : Struct identifierName OpenCurly (structItem (eos structItem)*)? eos? CloseCurly
    ;

structItem
    : structField
    | structFunction
    ;

structFunction
    : returnType identifierName functionParameterList functionBody
    ;

structField
    : type identifierName (Colon integer)?
    ;

labelStatement
    : identifierName Colon
    ;

gotoStatement
    : Goto identifierName
    ;

asmItems
    : (asmStatementItem (eos asmStatementItem)*)? eos?
    ;

asmStatementItem
    : asmIdentifier {noNewLine()}? asmArgument?      # AsmInstruction
    | AsmHere {noNewLine()}? asmArgument?            # AsmRawValue
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
	| statement
	;

// Function
functionBody
	: blockStatement
	| Arrow expression
	;

// Variable
variableDeclaration
    : Const? type identifierName (Assign (expression|initStruct))?              # DefaultVariableDeclaration
    | Const? Var identifierName Assign expression                               # AutoVariableDeclaration
    | Inline?  returnType identifierName functionParameterList functionBody     # FunctionDeclaration
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
