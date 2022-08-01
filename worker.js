import { run, asmLibraryArg, Module } from './Astro8.Wasm.js'

const screenSize = 64 * 64 * 4;

asmLibraryArg['UpdatePixels'] = (x, y, r, g, b) => {
    self.postMessage([x, y, r, g, b]);
};

Module.onRuntimeInitialized = _ => {
    const corertInit = Module.cwrap('CoreRT_StaticInitialization', 'number', []);
    corertInit();
    self.postMessage('ready');
};

run();

self.onmessage = function handleMessageFromMain(msg) {
    const array = new Uint8Array(msg.data);
    const runCode = Module.cwrap('RunCode', null, ['array', 'number', 'number']);
    runCode(array, array.length, 0);
};