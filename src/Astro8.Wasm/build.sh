touch Interop.cs
dotnet publish -c Release
$EMSDK/upstream/bin/wasm-opt -Oz -o bin/Release/net7.0/browser-wasm/native/Astro8.wasm bin/Release/net7.0/browser-wasm/native/Astro8.Wasm.wasm
cp bin/Release/net7.0/browser-wasm/native/Astro8.wasm ../Astro8.Editor/wwwroot/runtime/
cp bin/Release/net7.0/browser-wasm/native/Astro8.Wasm.js ../Astro8.Editor/wwwroot/runtime/