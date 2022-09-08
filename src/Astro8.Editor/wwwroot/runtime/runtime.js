window.emulator = (function() {
    const emulator = {};

    emulator.init = function(id, control) {
        const canvas = document.getElementById(id);
        const ctx = canvas.getContext('2d');
        let worker;

        // Program
        emulator.execute = async function(data) {
            if (worker) {
                worker.postMessage(['pause']);
                worker.terminate();
            }

            /** @type {ArrayBuffer} **/ const buffer = await data.arrayBuffer();

            console.debug('Sending program to worker');
            worker = new Worker('/runtime/worker.js');
            worker.addEventListener("message", handle);
            worker.addEventListener("message", (e) => {
                if (e.data[0] === 'ready') {
                    worker.postMessage(['program', buffer], [buffer]);
                }
            });
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

        function handle(msg) {
            const [code, p1, p2] = msg.data;

            if (typeof p1 !== 'number') {
                return
            }

            switch (code) {
                case 'update_pixel':
                    if (typeof p2 === 'number') {
                        const offset = p1 * 4;

                        data[offset] = bitRange(p2, 10, 5) * 8;
                        data[offset + 1] = bitRange(p2, 5, 5) * 8;
                        data[offset + 2] = bitRange(p2, 0, 5) * 8;
                        data[offset + 3] = 255;

                        screenChanged = true;
                    }
                    break;
            }
        }

        // Keyboard
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
            worker?.postMessage(['exp', 0, code]);
        });

        window.addEventListener('keyup', function(e) {
            if (lastKey !== e.key) {
                return
            }

            console.debug('Sending expansion value', 168, 'to worker');
            worker?.postMessage(['exp', 0, 168]);
            lastKey = null;
        });

        // Mouse
        let mouse = 0b0000000000000000;

        canvas.addEventListener('mousemove', (e) => {
            const scale = parseInt(getComputedStyle(canvas).getPropertyValue('--scale'));
            const x = Math.floor(e.offsetX / scale);
            const y = Math.floor(e.offsetY / scale);

            mouse = ((x & 0b1111111) << 7) | (y & 0b1111111) | (mouse & 0b1100000000000000);
            console.log(mouse.toString(2))
            worker?.postMessage(['exp', 1, mouse]);
        })

        canvas.addEventListener('mousedown', (e) => {
            if (e.button === 0) {
                mouse |= 0b0100000000000000;
            } else if (e.button === 2) {
                mouse |= 0b1000000000000000;
            }

            worker?.postMessage(['exp', 1, mouse]);
        })

        canvas.addEventListener('mouseup', (e) => {
            if (e.button === 0) {
                mouse &= ~0b0100000000000000;
            } else if (e.button === 2) {
                mouse &= ~0b1000000000000000;
            }
        });

        emulator.enableInput = function(value) {
            // inputEnabled = value;
        };
    }

    return emulator;
}());
