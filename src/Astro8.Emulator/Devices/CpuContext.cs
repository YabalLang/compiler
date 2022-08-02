namespace Astro8.Devices;

public record struct CpuContext
{
    public int A;
    public int B;
    public int C;
    public int Bus;
    public int MemoryIndex;
    public bool FlagA;
    public bool FlagB;
    public int ProgramCounter;

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
    }

    public static CpuContext Load(BinaryReader reader)
    {
        return new CpuContext
        {
            A = reader.ReadInt32(),
            B = reader.ReadInt32(),
            C = reader.ReadInt32(),
            Bus = reader.ReadInt32(),
            MemoryIndex = reader.ReadInt32(),
            FlagA = reader.ReadBoolean(),
            FlagB = reader.ReadBoolean(),
            ProgramCounter = reader.ReadInt32()
        };
    }
}
