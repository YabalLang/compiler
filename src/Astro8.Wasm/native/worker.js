import { run, asmLibraryArg, Module } from './Astro8.Wasm.js'

const screenSize = 64 * 64;

asmLibraryArg['UpdatePixels'] = (offset) => {
    console.log('Updating')

    const buffer = Module.asm.memory.buffer.slice(offset, offset + screenSize);

    self.postMessage(buffer, [buffer]);
};

Module.onRuntimeInitialized = _ => {
    const corertInit = Module.cwrap('CoreRT_StaticInitialization', 'number', []);
    corertInit();
    self.postMessage('ready');
};

run();

self.onmessage = function handleMessageFromMain(msg) {
    console.log('A')
    const runCode = Module.cwrap('RunCode', null, ['string']);
    runCode(msg.data);
};