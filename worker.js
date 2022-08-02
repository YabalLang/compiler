var Module = {}

let compile, step, setExpansionPort;

Module.instantiateWasm = async (info, receiveInstance) => {
    info.env.UpdatePixel = (address, color) => {
        self.postMessage([address, color]);
    };

    console.time('Loading WASM file');
    const response = await fetch('./Astro8.wasm', {
        credentials: 'same-origin'
    })
    var result = await WebAssembly.instantiateStreaming(response, info);
    console.timeEnd('Loading WASM file');

    receiveInstance(result.instance);
};

const setImmediate = (function() {
    const {port1, port2} = new MessageChannel();
    const queue = [];

    port1.onmessage = function() {
        const callback = queue.shift();
        callback();
    };

    return function(callback) {
        port2.postMessage(null);
        queue.push(callback);
    };
})();

function loop() {
    step(128);
    setImmediate(loop);
}

Module.onRuntimeInitialized = _ => {
    compile = Module.cwrap('Compile', null, ['array', 'number']);
    step = Module.cwrap('Step', null, ['number']);
    setExpansionPort = Module.cwrap('SetExpansionPort', null, ['number']);

    const corertInit = Module.cwrap('CoreRT_StaticInitialization', 'number', []);
    corertInit();
    self.postMessage('ready');

    loop();
};

self.onmessage = function handleMessageFromMain(msg) {
    const [code, parameter] = msg.data;

    if (code === 'program') {
        console.time('Compiling program');
        const array = new Uint8Array(parameter);
        const runCode = Module.cwrap('Compile', null, ['array', 'number']);
        runCode(array, array.length);
        console.timeEnd('Compiling program');
    } else if (code === 'key') {
        setExpansionPort(parameter);
    }
};

self.importScripts('./Astro8.Wasm.js');
