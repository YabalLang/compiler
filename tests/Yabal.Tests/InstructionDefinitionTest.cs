using System.Text;
using Yabal.Instructions;

namespace Yabal.Tests;

[UsesVerify]
public class InstructionDefinitionTest
{
    [Fact]
    public Task Parse()
    {
        var definitions = Instruction.Parse(new[]
        {
            "fetch( 0=cr,aw & 1=rm,iw,ce & 2=ei", // Fetch
            "ain( 2=aw,ir & 3=wa,rm & 4=ei", // LoadA
        });

        var settings = new VerifySettings();
        settings.IgnoreMember<MicroInstruction>(e => e.Value);
        settings.IgnoreInstance<bool>(b => !b);
        return Verify(definitions, settings);
    }
}
