var screen = create_pointer(53871, 1);
var screen_width = 108;

inline int mask(int n) {
    return (1 << n) - 1;
}

inline int get_color(int r, int g, int b) {
    return (r / 8 << 10) + (g / 8 << 5) + (b / 8);
}

while (true) {
    MouseInput input = asm { RDEXP 1 };

    input.x = 10;

    var offset = input.y * screen_width + input.x;

    if (input.left == 1) {
        screen[offset] = get_color(255, 255, 255);
    } else if (input.right == 1) {
        screen[offset] = get_color(0, 0, 0);
    }
}

struct MouseInput {
    int y : 7;
    int x : 7;
    int left : 1;
    int right : 1;
};