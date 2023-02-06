namespace Yabal.Devices;

public readonly record struct CpuContext(int A, int B, int C, int Bus, int MemoryIndex, bool FlagA, bool FlagB, int ProgramCounter, int Bank)
{
    public void Save(BinaryWriter writer)
    {
        writer.Write(A);
        writer.Write(B);
        writer.Write(C);
        writer.Write(Bus);
        writer.Write(MemoryIndex);
        writer.Write(FlagA);
        writer.Write(FlagB);
        writer.Write(ProgramCounter);
        writer.Write(Bank);
    }

    public static CpuContext Load(BinaryReader reader)
    {
        return new CpuContext
        (
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadBoolean(),
            reader.ReadBoolean(),
            reader.ReadInt32(),
            reader.ReadInt32()
        );
    }
}
