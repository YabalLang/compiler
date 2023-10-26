set EMSDK=C:\Sources\emsdk

touch Interop.cs
dotnet publish -c Release
%EMSDK%/upstream/bin/wasm-opt -Oz -o bin/Release/net8.0/browser-wasm/native/Yabal.wasm bin/Release/net8.0/browser-wasm/native/Yabal.Wasm.wasm
