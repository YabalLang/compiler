set EMSDK=C:\Sources\emsdk

touch Interop.cs
dotnet publish -c Release
%EMSDK%/upstream/bin/wasm-opt -Oz -o bin/Release/net8.0/browser-wasm/native/Yabal.wasm bin/Release/net8.0/browser-wasm/native/Yabal.Wasm.wasm

xcopy /Y /Q bin\Release\net8.0\browser-wasm\native\Yabal.wasm C:\Sources\yabal.dev\public\runtime\Yabal.Wasm.wasm
xcopy /Y /Q bin\Release\net8.0\browser-wasm\native\Yabal.Wasm.js C:\Sources\yabal.dev\public\runtime\Yabal.Wasm.js