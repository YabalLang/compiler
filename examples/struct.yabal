var chars = create_pointer(0xD12A, 1);
var items = create_pointer<Item>(0x3FFE);

items[0] = { x: 0, y: 0, character: '1' };
items[1] = { x: 17, y: 0, character: '2' };
items[2] = { x: 0, y: 17, character: '3' };
items[3] = { x: 17, y: 17, character: '4' };

while (true) {
    for (var i = 0; i < 4; i++) {
        var item = items[i];

        chars[item.y * 18 + item.x] = item.character;
    }
}

struct Item {
    int x
    int y
    int character
}