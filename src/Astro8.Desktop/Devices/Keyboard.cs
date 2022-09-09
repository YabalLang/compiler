namespace Astro8.Devices;

public static class Keyboard
{
	public static readonly IReadOnlyDictionary<int, int> Table = new Dictionary<int, int>
	{
		// Special characters
		[44] = 0,  // space -> blank
		[58] = 1,  // f1 -> smaller solid square
		[59] = 2,  // f2 -> full solid square
		[87] = 3,  // num+ -> +
		[86] = 4,  // num- -> -
		[85] = 5,  // num* -> *
		[84] = 6,  // num/ -> /
		[60] = 7,  // f3 -> full hollow square
		[45] = 8,  // _ -> _
		[80] = 9,  // l-arr -> <
		[79] = 10,  // r-arr -> >
		[82] = 71,  // u-arr -> u-arr
		[81] = 72,  // d-arr -> d-arr
		[49] = 11,  // | -> vertical line |
		[66] = 12,  // f9 -> horizontal line --

		// Letters
		[4] = 13,  // a -> a
		[5] = 14,  // b -> b
		[6] = 15,  // c -> c
		[7] = 16,  // d -> d
		[8] = 17,  // e -> e
		[9] = 18,  // f -> f
		[10] = 19,  // g -> g
		[11] = 20,  // h -> h
		[12] = 21,  // i -> i
		[13] = 22,  // j -> j
		[14] = 23,  // k -> k
		[15] = 24,  // l -> l
		[16] = 25,  // m -> m
		[17] = 26,  // n -> n
		[18] = 27,  // o -> o
		[19] = 28,  // p -> p
		[20] = 29,  // q -> q
		[21] = 30,  // r -> r
		[22] = 31,  // s -> s
		[23] = 32,  // t -> t
		[24] = 33,  // u -> u
		[25] = 34,  // v -> v
		[26] = 35,  // w -> w
		[27] = 36,  // x -> x
		[28] = 37,  // y -> y
		[29] = 38,  // z -> z

		// Numbers
		[39] = 39,  // 0 -> 0
		[30] = 40,  // 1 -> 1
		[31] = 41,  // 2 -> 2
		[32] = 42,  // 3 -> 3
		[33] = 43,  // 4 -> 4
		[34] = 44,  // 5 -> 5
		[35] = 45,  // 6 -> 6
		[36] = 46,  // 7 -> 7
		[37] = 47,  // 8 -> 8
		[38] = 48,  // 9 -> 9

		[42] = 70, // backspace -> backspace
	};
}
