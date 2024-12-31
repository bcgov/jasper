import Vue from '@vitejs/plugin-vue';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const basePath = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
    plugins: [Vue()],
    resolve: {
        extensions: ['.ts', '.vue']
    },
    test: {
        alias: [
            { find: 'SRC', replacement: resolve(basePath, './src') },
            { find: 'CMP', replacement: resolve(basePath, './src/components') }
        ],
        css: true,
        environment: 'happy-dom',
        globals: true,
        logHeapUsage: true,
        reporters: ['default', 'junit'],
        outputFile: './coverage/junit.xml',
        mockReset: true
    }
});