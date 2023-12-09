parser grammar YabalParser;

options { tokenVocab = YabalLexer; }

program
    : (statement (eos statement)*)? SemiColon* EOF
    ;

rawType
    : Int            # IntType
    | Bool           # BoolType
    | identifierName # StructType
    ;

typeList
    : (type (Comma type)*)?
    ;

type
    : rawType                                           # DefaultType
    | type OpenCloseBracket                             # ArrayType
    | Func Less (typeList Comma)? returnType Greater    # FunctionType
    ;

returnType
    : type  # DefaultReturnType
    | Void  # VoidReturnType
    ;

expression
	: createPointer                                                     # CreatePointerExpression
	| StackAlloc type OpenBracket expression CloseBracket               # StackAllocExpression
	| IncludeBytes expression                                           # IncludeBytesExpression
	| IncludeImage expression                                           # IncludeImageExpression
	| IncludeFont expression                                            # IncludeFontExpression
	| SizeOf expression                                                 # SizeOfExpression
	| Ref expression                                                    # RefExpression
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
	| expression (Less Less|Greater Greater) expression					# ShiftExpression
	| expression (GreaterEqual|Greater|LessEqual|Less) expression		# ComparisonExpression
	| expression (Equals|NotEquals) expression						    # EqualExpression
	| expression And expression									        # AndExpression
	| expression Or expression									        # OrExpression
	| expression Xor expression									        # XorExpression
	| expression AndAlso expression								        # AndAlsoExpression
	| expression OrElse expression								        # OrElseExpression
	| expression QuestionMark expression Colon expression               # TernaryExpression
	| expression Assign expression                                      # AssignExpression
	| Asm OpenCurly asmItems CloseCurly                                 # AsmExpression
	| Throw expression												    # ThrowExpression
	| OpenBrace type CloseBrace expression                              # CastExpression
	| OpenBrace expression CloseBrace								    # ExpressionExpression
	| arrowFunction                                                     # ArrowFunctionExpression
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
    : namespaceStatement
    | useStatement
    | importStatement
    | variableDeclaration
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
    : Struct identifierName OpenCurly (structItem (eos structItem)*)? SemiColon* CloseCurly
    ;

structItem
    : structField
    | structFunction
    ;

structFunction
    : Static? returnType identifierName functionParameterList functionBody
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

asmEos
    : AsmLineTerminator
    | SemiColon+
    ;

asmItems
    : asmEos* (asmStatementItem (asmEos+ asmStatementItem)*)? asmEos*
    ;

asmStatementItem
    : asmIdentifier asmArgument? asmArgument?   # AsmInstruction
    | AsmHere asmArgument?                      # AsmRawValue
    | asmIdentifier AsmColon                    # AsmLabel
    | AsmComment                                # AsmComment
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
	: While OpenBrace expression CloseBrace blockOrSingleStatement
	;

importStatement
    : Import string
    ;

namespaceStatement
    : Namespace identifierName (Dot identifierName)* blockStatement?
    ;

useStatement
    : Use identifierName (Dot identifierName)*
    ;

// If-statement
ifStatement
	: If OpenBrace expression CloseBrace blockOrSingleStatement elseIfStatement* elseStatement?
	;

elseIfStatement
	: Else If OpenBrace expression CloseBrace blockOrSingleStatement
	;

elseStatement
	: Else blockOrSingleStatement
	;

// For-statement
forStatement
	: For OpenBrace forInit? SemiColon expression SemiColon statement? CloseBrace blockOrSingleStatement
	;

forInit
    : statement
    ;

// Utils
expressionList
    : (expression (Comma expression)*)?
    ;

blockOrSingleStatement
    : blockStatement
    | statement
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
    : Const? type identifierName (Assign (expression|initStruct))?                          # DefaultVariableDeclaration
    | Const? Var identifierName Assign expression                                           # AutoVariableDeclaration
    | Inline? returnType (identifierName|operatorName) functionParameterList functionBody   # FunctionDeclaration
    | Inline? Operator type functionParameterList functionBody                              # OperatorFunctionDeclaration
    ;

arrowFunction
    : OpenBrace (identifierName (Comma identifierName)*)? CloseBrace Arrow blockStatement
    | OpenBrace (identifierName (Comma identifierName)*)? CloseBrace Arrow expression
    ;

operatorName
    : Operator (Add|Sub|Div|Mul|Mod|ShiftLeft|ShiftRight|GreaterEqual|Greater|LessEqual|Less|Equals|NotEquals|And|Or|Xor|AndAlso|OrElse)
    ;

// Identifier
identifierName
	: Identifier
	| Underscore
	| Func
	;

functionParameterList
    : OpenBrace (functionParameter (Comma functionParameter)*)? CloseBrace
    ;

functionParameter
	: Ref? type identifierName (Assign expression)?
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
    : SemiColon+
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
