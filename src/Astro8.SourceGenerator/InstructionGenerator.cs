using System.Text;
using Astro8.Instructions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace Astro8.SourceGenerator;

public record InstructionStepModel(
    int Id,
    MicroInstruction MicroInstruction,
    bool HasFlags = false,
    bool FlagA = false,
    bool FlagB = false
);

public class InstructionModel
{
    public InstructionModel(Instruction instruction)
    {
        Id = instruction.Id;
        Name = instruction.Name;
        IsSimple = true;

        var stepCount = instruction.MicroInstructions.Length / 4;

        for (var i = 2; i < stepCount; i++)
        {
            var microInstructions = instruction.MicroInstructions
                .Skip(i * 4)
                .Take(4)
                .ToArray();

            if (microInstructions.Distinct().Count() == 1)
            {
                var microInstruction = microInstructions[0];

                if (microInstruction.Value == 0)
                {
                    continue;
                }

                if (microInstruction.IsEI)
                {
                    break;
                }

                Steps.Add(new InstructionStepModel(i, microInstruction));
            }
            else
            {
                for (var j = 0; j < microInstructions.Length; j++)
                {
                    var microInstruction = microInstructions[j];

                    if (microInstruction == null)
                    {
                        continue;
                    }

                    Steps.Add(new InstructionStepModel(
                        i,
                        microInstruction,
                        true,
                        j is 0b10 or 0b11,
                        j is 0b01 or 0b11
                    ));
                }
            }
        }
    }

    public int Id { get; }

    public string Name { get; }

    public bool IsSimple { get; set; }

    public List<InstructionStepModel> Steps { get; } = new();
}

public record Model(
    IReadOnlyList<InstructionModel> Instructions
);

[Generator]
public class InstructionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        const string file = "template.scriban";

        var model = new Model(
            Instruction.Default.Select(i => new InstructionModel(i))
                .ToList()
        );
        var template = Template.Parse(EmbeddedResource.GetContent(file), file);
        var output = template.Render(model, member => member.Name);

        context.AddSource("CpuInstructions", SourceText.From(output, Encoding.UTF8));
    }
}
