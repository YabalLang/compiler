namespace Astro8.Devices;

public static class Keyboard
{
    private static readonly int[] ConversionTable = new int[512];

    static Keyboard()
    {
        Array.Fill(ConversionTable, 168);

        // Special characters
		ConversionTable[44] = 0;	// space -> blank
		ConversionTable[58] = 1;	// f1 -> smaller solid square
		ConversionTable[59] = 2;	// f2 -> full solid square
		ConversionTable[87] = 3;	// num+ -> +
		ConversionTable[86] = 4;	// num- -> -
		ConversionTable[85] = 5;	// num* -> *
		ConversionTable[84] = 6;	// num/ -> /
		ConversionTable[60] = 7;	// f3 -> full hollow square
		ConversionTable[45] = 8;	// _ -> _
		ConversionTable[80] = 9;	// l-arr -> <
		ConversionTable[79] = 10;	// r-arr -> >
		ConversionTable[82] = 71;	// u-arr -> u-arr
		ConversionTable[81] = 72;	// d-arr -> d-arr
		ConversionTable[49] = 11;	// | -> vertical line |
		ConversionTable[66] = 12;	// f9 -> horizontal line --

		// Letters
		ConversionTable[4] = 13;	// a -> a
		ConversionTable[5] = 14;	// b -> b
		ConversionTable[6] = 15;	// c -> c
		ConversionTable[7] = 16;	// d -> d
		ConversionTable[8] = 17;	// e -> e
		ConversionTable[9] = 18;	// f -> f
		ConversionTable[10] = 19;	// g -> g
		ConversionTable[11] = 20;	// h -> h
		ConversionTable[12] = 21;	// i -> i
		ConversionTable[13] = 22;	// j -> j
		ConversionTable[14] = 23;	// k -> k
		ConversionTable[15] = 24;	// l -> l
		ConversionTable[16] = 25;	// m -> m
		ConversionTable[17] = 26;	// n -> n
		ConversionTable[18] = 27;	// o -> o
		ConversionTable[19] = 28;	// p -> p
		ConversionTable[20] = 29;	// q -> q
		ConversionTable[21] = 30;	// r -> r
		ConversionTable[22] = 31;	// s -> s
		ConversionTable[23] = 32;	// t -> t
		ConversionTable[24] = 33;	// u -> u
		ConversionTable[25] = 34;	// v -> v
		ConversionTable[26] = 35;	// w -> w
		ConversionTable[27] = 36;	// x -> x
		ConversionTable[28] = 37;	// y -> y
		ConversionTable[29] = 38;	// z -> z

		// Numbers
		ConversionTable[39] = 39;	// 0 -> 0
		ConversionTable[30] = 40;	// 1 -> 1
		ConversionTable[31] = 41;	// 2 -> 2
		ConversionTable[32] = 42;	// 3 -> 3
		ConversionTable[33] = 43;	// 4 -> 4
		ConversionTable[34] = 44;	// 5 -> 5
		ConversionTable[35] = 45;	// 6 -> 6
		ConversionTable[36] = 46;	// 7 -> 7
		ConversionTable[37] = 47;	// 8 -> 8
		ConversionTable[38] = 48;	// 9 -> 9

		ConversionTable[42] = 70;	// backspace -> backspace
    }

    public static int ConvertAsciiToSdcii(int key)
	{
		return ConversionTable[key];
	}
}
