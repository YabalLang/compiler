using Yabal.Exceptions;
using Yabal.Instructions;

namespace Yabal.Ast;

public record ArrowFunctionExpression(
	SourceRange Range,
	List<Identifier> Identifiers,
	BlockStatement Body
) : Expression(Range), ITypeExpression, IBankSource, IExpressionToB
{
	private bool _didBuild = false;
	private FunctionDeclarationStatement _declarationStatement = null!;
	private LanguageFunction? _typeFunctionType;

	public Function Function => _declarationStatement.Function;

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

	private void Build(YabalBuilder builder)
	{
		if (_didBuild)
		{
			return;
		}

		_declarationStatement.Build(builder);
		_didBuild = true;
	}

	public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
	{
		Build(builder);

		builder.SetA(_declarationStatement.Function.Label);
		builder.StoreA(pointer);
	}

	protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
	{
		Build(builder);

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
			_typeFunctionType = _typeFunctionType,
			_didBuild = _didBuild
		};
	}

	public override Expression Optimize(LanguageType? suggestedType)
	{
		return new ArrowFunctionExpression(Range, Identifiers, Body.Optimize())
		{
			_declarationStatement = _declarationStatement?.Optimize()!,
			_typeFunctionType = _typeFunctionType,
			_didBuild = _didBuild
		};
	}

	int IBankSource.Bank => 0;

	public void BuildExpressionToB(YabalBuilder builder)
	{
		Build(builder);

		builder.SetB(_declarationStatement.Function.Label);
	}

	bool IExpressionToB.OverwritesA => false;
}
