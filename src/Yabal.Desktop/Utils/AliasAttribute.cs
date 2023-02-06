namespace Yabal.Utils;

public class AliasAttribute : Attribute
{
    public AliasAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }

    public string[] Aliases { get; }
}
