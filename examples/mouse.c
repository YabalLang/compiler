var screen = create_pointer(53871, 1);
var screen_width = 108;

inline int mask(int n) {
    return (1 << n) - 1;
}

inline int get_color(int r, int g, int b) {
    return (r / 8 << 10) + (g / 8 << 5) + (b / 8);
}

while (true) {
    int value = asm { RDEXP 1 };
    var x = value >> 7 & mask(7);
    var y = value & mask(7);
    var mouseLeft = value >> 14 & 1;
    var mouseRight = value >> 15 & 1;
    var offset = y * screen_width + x;

    if (mouseLeft == 1) {
        screen[offset] = get_color(255, 255, 255);
    } else if (mouseRight == 1) {
        screen[offset] = get_color(0, 0, 0);
    }
}