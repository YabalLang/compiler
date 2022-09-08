var chars = create_pointer(0xD12A, 1)
var value = 0;

while (true) {
    asm {
        RDEXP
        STA @value
    }

    chars[0] = value
}