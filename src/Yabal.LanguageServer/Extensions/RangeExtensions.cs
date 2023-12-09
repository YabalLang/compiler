using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Yabal.Ast;
using Yabal.Visitor;

namespace Yabal.LanguageServer;

public static class RangeExtensions
{
    public static Range ToRange(this SourceRange sourceRange)
    {
        return new Range(
            sourceRange.StartLine - 1,
            sourceRange.StartColumn,
            sourceRange.EndLine - 1,
            sourceRange.EndColumn
        );
    }

    public static bool IsInRange(this SourceRange sourceRange, Position position)
    {
        return sourceRange.IsInRange(position.Line + 1, position.Character);
    }

    public static (Identifier?, Variable?) Find(this IEnumerable<Variable> variables, Position position)
    {
        foreach (var variable in variables)
        {
            if (variable.Identifier.Range.IsInRange(position))
            {
                return (variable.Identifier, variable);
            }

            foreach (var identifier in variable.References)
            {
                if (identifier.Range.IsInRange(position))
                {
                    return (identifier, variable);
                }
            }
        }

        return default;
    }

    public static (Identifier?, Function?) Find(this IEnumerable<Function> functions, Position position)
    {
        foreach (var function in functions)
        {
            if (function.Name is not FunctionIdentifier { Identifier: {} name })
            {
                continue;
            }

            if (name.Range.IsInRange(position))
            {
                return (name, function);
            }

            foreach (var identifier in function.References)
            {
                if (identifier.Range.IsInRange(position))
                {
                    return (identifier, function);
                }
            }
        }

        return default;
    }

    public static (Identifier?, IVariable?) Find(this YabalBuilder builder, Position position)
    {
        (var identifier, IVariable? variable) = builder.Variables.Find(position);

        if (identifier != null && variable != null)
        {
            return (identifier, variable);
        }

        (identifier, variable) = builder.Functions.Find(position);

        return (identifier, variable);
    }
}
