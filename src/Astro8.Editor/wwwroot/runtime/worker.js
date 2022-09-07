var Module = {}

let compile, step, setExpansionPort;
let autoStep = true;

Module.instantiateWasm = async (info, receiveInstance) => {
    const mappings = {
        UpdateA: 'update_a',
        UpdateB: 'update_b',
        UpdateC: 'update_c',
        UpdateExpansionPort: 'update_expansion_port',
        UpdateBank: 'update_bank'
    };

    for (const [key, methodName] of Object.entries(mappings)) {
        info.env[key] = (value) => {
            self.postMessage([methodName, value]);
        };
    }

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

    self.postMessage(['update_counter', step(128)]);
    setImmediate(loop);
}

Module.onRuntimeInitialized = _ => {
    compile = Module.cwrap('Compile', null, ['array', 'number']);
    step = Module.cwrap('Step', "number", ['number']);
    setExpansionPort = Module.cwrap('SetExpansionPort', null, ['number']);

    const corertInit = Module.cwrap('CoreRT_StaticInitialization', 'number', []);
    corertInit();
    self.postMessage(['ready']);

    loop();
};

self.onmessage = function handleMessageFromMain(msg) {
    const [code, parameter] = msg.data;

    switch (code) {
        case 'program':
            console.time('Compiling program');
            try {
                const array = new Uint8Array(parameter);
                const runCode = Module.cwrap('Compile', null, ['array', 'number']);
                runCode(array, array.length);
            } finally {
                console.timeEnd('Compiling program');
            }
            break;
        case 'exp':
            setExpansionPort(parameter);
            break;
        case 'pause':
            autoStep = false;
            break;
        case 'resume':
            autoStep = true;
            loop();
            break;
        case 'step':
            self.postMessage(['update_counter', step(parameter ? parseInt(parameter) : 1)]);
            break;
    }
};

self.importScripts('./Astro8.Wasm.js');
