/*
The following example shows how to write to different memory banks.

Astro-8 has two memory banks:
    0 - Program
    1 - Graphics

In Yabal you can create a pointer that points to an address:

    var pointer = create_pointer(16382)

By default, the memory bank is 0. So this pointer points to address 16382 in the program memory.
If you want to point to the graphics memory, you can specify the memory bank as the second argument:

    var pointer = create_pointer(53546, 1)

Now the pointer points to address 53546 in the graphics memory.
*/

var chars = create_pointer(0xD12A, 1)
var message = "Hello world"

while(true) {
    for (int i = 0; i < sizeof(message); i++) {
        chars[i] = message[i]
    }
}