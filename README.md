# Astro-8
This repository houses two Astro-8 projects:
1. A port of [Astro-8 Emulator by sam-astro](https://github.com/sam-astro/Astro8-Computer/tree/main/Astro8-Emulator) in C#.
2. Custom language "Yabal" that compiles into valid Astro-8 assembly/binary.

## Yabal
Yabal is a custom language that compiles into valid Astro-8 assembly.

For examples of Yabal, see the [examples](examples) folder.

### Features
- ✅ Variables
- ✅ Functions (with parameters and call stack)
- ✅ If statements
- ✅ Comparison operators (`==`, `!=`, `>`, `<`, `>=`, `<=`)
- ✅ Arithmetic operators (`+`, `-`, `*`, `/`, `%`)
- ✅ Bitwise operators (`&`, `|`, `^`, `~`, `<<`, `>>`)
- ✅ Logical operators (`&&`, `||`, `!`)
- ✅ Comments
- ✅ Create pointers (`create_pointer`)
- ✅ Inline assembly
- ✅ For and while loops
- ✅ Structs
- ✅ Include files
- 🚧 Chars and strings
- ❌ Arrays
- ❌ Classes

### Usage
Download the latest release from the [releases page](https://github.com/GerardSmit/Astro8/releases).

When running Linux, install `libsdl2-2.0-0`:

```bash
# Linux
sudo apt-get install libsdl2-2.0-0
```

To view all the commands with a description run `astro --help`.

#### Run
To run a file with the C# emulator run the following command:

```bash
# Windows
astro run source.yabal

# Linux
./astro run source.yabal
```

#### Compile
To compile a file to assembly run the following command:

```bash
# Windows
astro build source.yabal

# Linux
./astro build source.yabal
```

It is possible to change the output file with `-f <flag|extension>` to the following formats:

| Flag | Name | Extension |
| --- | --- | --- |
| `a` | Assembly _(default)_ | `asm` |
| `c` | Assembly with comments | `asmc` |
| `h` | Astro-8 Emulator HEX file | `aexe` |
| `l` | Logisim Evolution HEX file | `hex` |

For example:

```bash
astro build -f h source.yabal # Compile aexe 
astro build -f ha source.yabal # Compile aexe and asm

astro build -f aexe source.yabal # Compile aexe 
astro build -f aexe,asm source.yabal # Compile aexe and asm
```
