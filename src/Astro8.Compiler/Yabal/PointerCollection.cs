using System.Collections;
using Astro8.Yabal.Ast;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class PointerCollection : IEnumerable<InstructionPointer>
{
    private readonly Dictionary<int, List<InstructionPointer>> _pointersBySize = new();
    private readonly string _name;
    private int _counter;

    public PointerCollection(string name)
    {
        _name = name;
    }

    public int Count => _pointersBySize.Sum(i => i.Value.Count);

    public InstructionPointer Get(int index, int size)
    {
        if (!_pointersBySize.TryGetValue(size, out var pointers))
        {
            pointers = new List<InstructionPointer>();
            _pointersBySize[size] = pointers;
        }

        if (index < pointers.Count)
        {
            return pointers[index];
        }

        var pointer = new InstructionPointer($"[{_name}:{_counter++}]", size, true);
        pointers.Add(pointer);
        return pointer;
    }

    public InstructionPointer GetNext(int size)
    {
        var index = _pointersBySize.TryGetValue(size, out var pointers) ? pointers.Count : 0;
        return Get(index, size);
    }

    public InstructionPointer GetNext(BlockStack block, int size)
    {
        return Get(block.GetNextOffset(size, block.IsGlobal), size);
    }

    public InstructionPointer GetNext(BlockStack block, LanguageType type)
    {
        return GetNext(block, type.Size);
    }

    public IEnumerator<InstructionPointer> GetEnumerator()
    {
        return _pointersBySize.SelectMany(i => i.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
