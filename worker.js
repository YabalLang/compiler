var Module = {}

let compile, step, setExpansionPort;

Module.instantiateWasm = async (info, receiveInstance) => {
    info.env.UpdatePixel = (x, y, r, g, b) => {
        self.postMessage([x, y, r, g, b]);
    };

    const response = await fetch('./Astro8.wasm', { credentials: 'same-origin' })
    var result = await WebAssembly.instantiateStreaming(response, info);

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
        const array = new Uint8Array(parameter);
        const runCode = Module.cwrap('Compile', null, ['array', 'number']);
        runCode(array, array.length);
    } else if (code === 'key') {
        setExpansionPort(parameter);
    }
};

self.importScripts('./Astro8.Wasm.js');
