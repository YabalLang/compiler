namespace Yabal.Ast;

public record ConstantMemoryExpression(SourceRange Range, LanguageType Type, int[] Data) : Expression(Range)
{
	public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
	{
		for (var i = 0; i < Data.Length; i++)
		{
			builder.SetA(Data[i]);
			builder.SetComment($"constant value: {Type} offset {i}");
			builder.StoreA(pointer.Add(i));
		}
	}

	protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
	{
		builder.SetA(Data[0]);
	}

	public override bool OverwritesB => false;

	public override LanguageType Type { get; } = Type;

	public override Expression CloneExpression()
	{
		return new ConstantMemoryExpression(Range, Type, Data);
	}
}
