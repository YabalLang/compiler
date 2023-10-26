namespace Yabal.Devices;

public sealed class DebugDevice<THandler> : MemoryDevice
	where THandler : Handler
{
	private readonly THandler _handler;
	private readonly int[] _buffer = new int[16];
	private int _line;
	private int _column;
	private int _size;

	public DebugDevice(int bank, int address, THandler handler)
		: base(address, length: 16)
	{
		_handler = handler;
		Bank = bank;
	}

	public int Bank { get; set; }

	public override void Write(int address, int value)
	{
		switch (address)
		{
			case DebugOffset.Flush:
				_handler.ShowVariable(_line, _column, _buffer.AsSpan(0, _size));
				break;
			case DebugOffset.Line:
				_line = value;
				break;
			case DebugOffset.Column:
				_column = value;
				break;
			case DebugOffset.Size:
				_size = value;
				break;
			default:
				_buffer[address - DebugOffset.Value] = value;
				break;
		}
	}
}
