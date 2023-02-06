using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTests;
using VerifyXunit;

namespace Yabal.SourceGenerator.Tests;

[UsesVerify]
public class GenerateTest
{
    [Fact]
    public Task GenerateInstructions()
    {
        VerifySourceGenerators.Enable();

        var compilation = CSharpCompilation.Create("name");
        var generator = new InstructionGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }
}
