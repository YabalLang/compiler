using Yabal.Exceptions;
using Yabal.Instructions;

namespace Yabal.Ast;

public record ArrowFunctionExpression(
	SourceRange Range,
	List<Identifier> Identifiers,
	BlockStatement Body
) : Expression(Range), ITypeExpression
{
	private FunctionDeclarationStatement _declarationStatement = null!;
	private LanguageFunction? _typeFunctionType;

	public override void Initialize(YabalBuilder builder)
	{
	}

	public void Initialize(YabalBuilder builder, LanguageType type)
	{
		if (type.FunctionType is null)
		{
			throw new InvalidCodeException("Cannot create an arrow function with a non-function type", Range);
		}

		_typeFunctionType = type.FunctionType;
		_declarationStatement = new FunctionDeclarationStatement(
			Range,
			null,
			type.FunctionType.ReturnType,
			Identifiers.Zip(type.FunctionType.Parameters, (i, p) => new FunctionParameter(i, p, false)).ToList(),
			Body,
			false
		);

		_declarationStatement.Declare(builder);
		_declarationStatement.Initialize(builder);

		_declarationStatement.Function.MarkUsed();
	}

	public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
	{
		_declarationStatement.Build(builder);

		builder.SetA(_declarationStatement.Function.Label);
		pointer.StoreA(builder);
	}

	protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
	{
		_declarationStatement.Build(builder);

		builder.SetA(_declarationStatement.Function.Label);
	}

	public override bool OverwritesB => false;
	public override LanguageType Type => _typeFunctionType is null
		? LanguageType.Unknown
		: new LanguageType(StaticType.Function, FunctionType: _typeFunctionType);

	public override Expression CloneExpression()
	{
		return new ArrowFunctionExpression(Range, Identifiers, Body.Optimize())
		{
			_declarationStatement = _declarationStatement.CloneStatement(),
			_typeFunctionType = _typeFunctionType
		};
	}

	public override Expression Optimize(LanguageType? suggestedType)
	{
		return new ArrowFunctionExpression(Range, Identifiers, Body.Optimize())
		{
			_declarationStatement = _declarationStatement?.Optimize()!,
			_typeFunctionType = _typeFunctionType
		};
	}
}
