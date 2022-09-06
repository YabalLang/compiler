using Astro8.Yabal.Ast;

namespace Astro8;

public class ErrorMessages
{
    public const string ArrayOnlyIntegerKey = "Only integers can be used as array keys";
    public const string InvalidArrayAccess = "Array access expression can only be used on arrays";
    public const string BinaryInstructionRequiresLabel = "Binary requires an argument to jump to";
    public const string InvalidAssignmentTarget = "You can only assign to variables and array elements";
    public const string CallExpressionCalleeMustBeIdentifier = "Callee must be an identifier";
    public const string ArgumentMustBeInteger = "Argument must be an integer";
    public const string SizeOfExpressionMustBeConstant = "size_of can only be used with constant values";
    public const string CannotUpdateNonInteger = "Cannot update a non-integer value";
    public const string InvalidComparison = "Cannot use this operator for comparison";
    public const string BreakOutsideLoop = "Cannot continue outside of a loop";
    public const string ContinueOutsideLoop = "Cannot continue outside of a loop";
    public const string ReturnOutsideFunction = "Return statement outside of function";
    public const string VariableTypeNotSpecified = "Variable type not specified";
    public const string ValueIsNotAnArray = "Value is not an array";
    public const string MemberAccessOnNonStruct = "Member access can only be used on structs";

    public static string MemberNotFound(string name)
        => $"Member '{name}' not found";

    public static string ExpectedBoolean(LanguageType type)
        => $"Expected boolean, got {type}";

    public static string UnknownInstruction(string name)
        => $"Unknown instruction: {name}";

    public static string InvalidType(LanguageType typeLeft, LanguageType typeRight)
        => $"Cannot convert {typeLeft} to {typeRight}";

    public static string UndefinedVariable(string name)
        => $"Variable {name} was not found";

    public static string ConstantVariable(string name)
        => $"Variable {name} is constant and cannot be assigned";

    public static string CallExpressionArgumentCountMismatch(string name, int parametersCount, int argumentsCount)
        => $"Function {name} expects {parametersCount} arguments, but {argumentsCount} were provided";

    public static string ArgumentTypeMismatch(int index, string name, LanguageType parameterType, LanguageType argumentType)
        => $"Argument {index} of function {name} is of type {parameterType}, but expected {argumentType}";

    public static string InvalidCharacter(char value)
        => $"Character {value} is not valid";

    public static string InvalidCharacterInString(char c, string value)
        => $"Character {c} is not valid in constant string '{value}'";

    public static string SwitchCaseTypeMismatch(LanguageType caseType, LanguageType switchType)
        => $"Case value type ({caseType}) does not match value type ({switchType})";

    public static string SwitchReturnTypeMismatch(LanguageType returnType, LanguageType switchType)
        => $"Return value type ({returnType}) does not match with previous return value type ({switchType})";

    public static string UnaryOperatorNotApplicableToType(UnaryOperator @operator, LanguageType valueType)
        => $"Cannot use '{@operator}' operator on type '{valueType}'";

    public static string DuplicateLabel(string name)
        => $"Label {name} already exists";

    public static string UndefinedLabel(string name)
        => $"Label {name} was not found";
}
