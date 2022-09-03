using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public class AddressOffset : IAddress
{
    private readonly IAddress _address;
    private readonly int _offset;

    public AddressOffset(IAddress address, int offset)
    {
        _address = address;
        _offset = offset;
    }

    public Either<int, InstructionPointer>? Get(YabalBuilder builder)
    {
        return null;
    }

    public int? Length => _address.Length;

    public int? GetValue(int offset)
    {
        return _address.GetValue(offset + _offset);
    }
}
