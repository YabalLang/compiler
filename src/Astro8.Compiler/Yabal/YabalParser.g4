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
	: Incr expression												# IncrementLeftExpression
	| expression Incr												# IncrementRightExpression
	| Decr expression												# DecrementLeftExpression
	| expression Decr												# DecrementRightExpression
	| expression inlineSwitch                                       # SwitchExpression
	| expression {noNewLine()}? OpenBrace expressionList CloseBrace	# CallExpression
	| expression NotEquals expression								# NotEqualExpression
	| expression AddEqual expression								# PlusEqualExpression
	| expression SubEqual expression								# SubEqualExpression
	| expression MulEqual expression								# MulEqualExpression
	| expression DivEqual expression								# DivEqualExpression
    | expression (Div|Mul) expression								# DivMulBinaryExpression
    | expression (Add|Sub) expression								# PlusSubBinaryExpression
	| expression Less expression									# LessExpression
	| expression LessEqual expression								# LessEqualExpression
	| expression Greater expression									# GreaterExpression
	| expression GreaterEqual expression							# GreaterEqualExpression
	| expression Equals expression						            # EqualExpression
	| expression And expression								        # AndExpression
	| expression Or expression								        # OrExpression
	| identifierName Assign expression                              # AssignExpression
	| Throw expression												# ThrowExpression
	| OpenBrace expression CloseBrace								# ExpressionExpression
    | string                                                        # StringExpression
	| integer                       								# IntegerExpression
	| boolean                       								# BooleanExpression
	| identifierName										        # IdentifierExpression
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
	| forStatement
	| ifStatement
	| continueStatement
	| breakStatement
	| expressionStatement
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

// If-statement
ifStatement
	: If OpenBrace expression CloseBrace blockStatement elseIfStatement* elseStatement? End
	;

elseIfStatement
	: Else If OpenBrace expression CloseBrace blockStatement
	;

elseStatement
	: Else blockStatement
	;

// For-statement
forStatement
	: For OpenBrace expression SemiColon expression SemiColon expression CloseBrace blockStatement
	;

// Utils
expressionList
    : (expression (Comma expression)*)?
    ;

blockStatement
	: OpenCurly (statement (eos statement)*)? CloseCurly
	;

// Function
functionBody
	: blockStatement
	| Arrow expression
	;

// Variable
variableDeclaration
    : type identifierName (Assign expression)?                      # DefaultVariableDeclaration
    | Var identifierName Assign expression                          # AutoVariableDeclaration
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
