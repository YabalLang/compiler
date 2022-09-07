window.emulator = (function() {
    const emulator = {};

    emulator.init = function(id, control) {
        const canvas = document.getElementById(id);
        const ctx = canvas.getContext('2d');
        const worker = new Worker('/runtime/worker.js');

        // Program
        emulator.execute = async function(data) {
            /** @type {ArrayBuffer} **/ const buffer = await data.arrayBuffer();

            console.debug('Sending program to worker');
            worker.postMessage(['program', buffer], [buffer]);
        };

        // Screen
        const imageData = ctx.createImageData(108, 108);
        const data = imageData.data;
        let screenChanged = false;

        function bitRange(value, offset, n)
        {
            value >>= offset;
            const mask = (1 << n) - 1;
            return value & mask;
        }

        function updateScreen() {
            if (screenChanged) {
                ctx.putImageData(imageData, 0, 0);
                screenChanged = false;
            }

            requestAnimationFrame(updateScreen);
        }

        requestAnimationFrame(updateScreen);

        let a = 0;
        let b = 0;
        let c = 0;
        let expansionPort = 0;
        let bank = 0;
        let counter = 0;
        let updateValue = false;

        setInterval(function() {
            if (!updateValue) {
                return;
            }

            updateValue = false;
            control.invokeMethodAsync('UpdateValue', a, b, c, expansionPort, counter, bank);
        }, 100);

        worker.addEventListener("message", function(msg) {
            const [code, ...parameter] = msg.data;

            switch (code) {
                case 'update_a':
                    a = parameter[0];
                    updateValue = true;
                    break;
                case 'update_b':
                    b = parameter[0];
                    updateValue = true;
                    break;
                case 'update_c':
                    c = parameter[0];
                    updateValue = true;
                    break;
                case 'update_expansion_port':
                    expansionPort = parameter[0];
                    updateValue = true;
                    break;
                case 'update_bank':
                    bank = parameter[0];
                    updateValue = true;
                    break;
                case 'update_counter':
                    counter = isNaN(parameter[0]) ? 0 : parameter[0];
                    updateValue = true;
                    break;
                case 'update_pixel':
                    const [address, color] = parameter;
                    const offset = address * 4;

                    data[offset] = bitRange(color, 10, 5) * 8;
                    data[offset + 1] = bitRange(color, 5, 5) * 8;
                    data[offset + 2] = bitRange(color, 0, 5) * 8;
                    data[offset + 3] = 255;

                    screenChanged = true;
                    break;
            }
        });

        // Input
        const keyMapping = {
            Space: 0,
            F1: 1,
            F2: 2,
            NumpadAdd: 3,
            NumpadSubtract: 4,
            NumpadMultiply: 5,
            NumpadDivide: 6,
            F3: 7,
            '_': 8,
            ArrowLeft: 9,
            ArrowRight: 10,
            ArrowUp: 71,
            ArrowDown: 72,
            '|': 11,
            KeyA: 13,
            KeyB: 14,
            KeyC: 15,
            KeyD: 16,
            KeyE: 17,
            KeyF: 18,
            KeyG: 19,
            KeyH: 20,
            KeyI: 21,
            KeyJ: 22,
            KeyK: 23,
            KeyL: 24,
            KeyM: 25,
            KeyN: 26,
            KeyO: 27,
            KeyP: 28,
            KeyQ: 29,
            KeyR: 30,
            KeyS: 31,
            KeyT: 32,
            KeyU: 33,
            KeyV: 34,
            KeyW: 35,
            KeyX: 36,
            KeyY: 37,
            KeyZ: 38,
            '0': 39,
            '1': 40,
            '2': 41,
            '3': 42,
            '4': 43,
            '5': 44,
            '6': 45,
            '7': 46,
            '8': 47,
            '9': 48,
            Backspace: 70
        }

        let lastKey = null;
        let inputEnabled = true;

        window.addEventListener('keydown', function(e) {
            if (!inputEnabled) {
                return;
            }

            lastKey = e.key;

            let code = keyMapping[e.key] ?? keyMapping[e.code] ?? 168;
            console.debug('Sending expansion value', code, 'to worker');
            worker.postMessage(['exp', code]);
        });

        window.addEventListener('keyup', function(e) {
            if (lastKey !== e.key) {
                return
            }

            console.debug('Sending expansion value', 168, 'to worker');
            worker.postMessage(['exp', 168]);
            lastKey = null;
        });

        emulator.enableInput = function(value) {
            // inputEnabled = value;
        };
    }

    return emulator;
}());
