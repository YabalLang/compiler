# Astro-8
This repository houses two Astro-8 projects:
1. A port of [Astro-8 Emulator by sam-astro](https://github.com/sam-astro/Astro8-Computer/tree/main/Astro8-Emulator) in C#.
2. Custom language "Yabal" that compiles into valid Astro-8 assembly/binary.

## Development
### WASM build
To build the project to WASM, we use `Microsoft.DotNet.ILCompiler.LLVM`. This is **NOT** the fancy shining NativeAOT compiler in .NET 7. This uses the LLVM compiler and is experimental. More information can be found in repository dotnet/runtimelab (branch: [feature/NativeAOT-LLVM](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT-LLVM#readme)).

When developing the emulator, do **NOT** use interfaces or abstract classes. These are broken in the LLVM compiler. This is why I'm using generic classes.
### Building
To build the WASM project, follow these steps:
1. Clone [emsdk](https://github.com/emscripten-core/emsdk): `git clone https://github.com/emscripten-core/emsdk.git && cd emsdk`
2. Install the latest version: `./emsdk install latest`
3. Activate the version: `./emsdk activate latest --permanent`
4. Go to the WASM project: `cd src/Astro8.Wasm`.
5. Build the project: `sh build.sh`  
   **Note:** If you get an error that a .HTML file was not found then the `EMSDK` enviroment variable was not found. Restart the console or set the `EMSDK` manually to the `emsdk` directory you cloned.
6. The output can be found in the directory `src/Astro8.Wasm/bin/Release/net7.0/browser-wasm/native`

In the directory there are 2 files:

1. `Astro8.Wasm.wasm` - Compile result by NativeAOT LLVM.
2. `Astro8.wasm` - Optimized WASM file by `wasm-opt`.

When deploying, always use `Astro8.wasm`.  
When debugging, use `Astro8.Wasm.wasm` since this file contains all the stack traces.