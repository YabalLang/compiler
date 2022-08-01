# C# Astro-8 Emulator
This is a port of the [Astro-8 emulator by sam-astro](https://github.com/sam-astro/Astro8-Computer/tree/main/Astro8-Emulator).

## Features
Compared to the original emulator:

- ✅ Binary assembly file (`program_machine_code`)
- ❌ Assembly code
- ❌ Armstrong code

## WASM build
To build the WASM project:

1. Download [emsdk](https://emscripten.org/docs/getting_started/downloads.html)
2. Install version `2.0.23`: `./emsdk install 2.0.23`
3. Activate the version: `./emsdk activate 2.0.23 --permanent`