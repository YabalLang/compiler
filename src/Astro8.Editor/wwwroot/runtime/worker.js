var Module = {}

let compile, step, setExpansionPort;
let autoStep = true;
let ready = false;

Module.instantiateWasm = async (info, receiveInstance) => {
    info.env.UpdatePixel = (address, color) => {
        self.postMessage(['update_pixel', address, color]);
    };

    console.time('Loading WASM file');
    const response = await fetch('/runtime/Astro8.wasm', {
        credentials: 'same-origin'
    })

    const result = await WebAssembly.instantiateStreaming(response, info);
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
    if (!autoStep) {
        return;
    }

    step(64);
    setImmediate(loop);
}

Module.onRuntimeInitialized = _ => {
    compile = Module.cwrap('Compile', null, ['array', 'number']);
    step = Module.cwrap('Step', "number", ['number']);
    setExpansionPort = Module.cwrap('SetExpansionPort', null, ['number', 'number']);

    const corertInit = Module.cwrap('CoreRT_StaticInitialization', 'number', []);
    corertInit();

    ready = true;
    self.postMessage(['ready']);

    loop();
};

self.onmessage = function handleMessageFromMain(msg) {
    if (!ready) return;

    const [code, p1, p2] = msg.data;

    switch (code) {
        case 'program':
            console.time('Compiling program');
            try {
                const array = new Uint8Array(p1);
                const runCode = Module.cwrap('Compile', null, ['array', 'number']);
                runCode(array, array.length);
            } finally {
                console.timeEnd('Compiling program');
            }
            break;
        case 'exp':
            setExpansionPort(p1, p2);
            break;
        case 'pause':
            autoStep = false;
            break;
        case 'resume':
            autoStep = true;
            loop();
            break;
        case 'step':
            step(typeof p1 === 'number' ? p1 : 1);
            break;
    }
};

self.importScripts('./Astro8.Wasm.js');
