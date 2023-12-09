import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import typescript from '@rollup/plugin-typescript';
import replace from '@rollup/plugin-replace';
import * as path from 'path';

const production = !process.env.ROLLUP_WATCH

export default [{
    input: 'src/extension.ts',
    output: {
        file: 'extension/index.js',
        format: 'cjs',
        external: [
            'vscode'
        ]
    },
    plugins: [
        replace({
            'process.env.NODE_ENV': JSON.stringify(production ? 'production' : 'dev'),
            'process.env.SERVER_PATH': JSON.stringify(path.join(__dirname, '..', 'src', 'Yabal.LanguageServer', 'bin', 'Debug', 'net8.0', 'Yabal.LanguageServer.exe'))
        }),
        typescript(),
        nodeResolve(),
        commonjs()
    ]
}]