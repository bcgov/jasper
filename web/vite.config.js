import basicSsl from '@vitejs/plugin-basic-ssl';
import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';

const path = require('path');
const vueSrc = 'src';

export default defineConfig({
  base:
    process.env.NODE_ENV === 'production' ? '/S2I_INJECT_PUBLIC_PATH/' : '/',
  plugins: [
    vue({
      template: {
        compilerOptions: {
          compatConfig: {
            MODE: 3,
          },
        },
      },
    }),
    basicSsl(),
  ],
  resolve: {
    alias: {
      vue: '@vue/compat',
      '@': path.resolve(__dirname, vueSrc),
      '@assets': path.resolve(__dirname, vueSrc.concat('/assets')),
      '@components': path.resolve(__dirname, vueSrc.concat('/components')),
      '@router': path.resolve(__dirname, vueSrc.concat('/router')),
      '@services': path.resolve(__dirname, vueSrc.concat('/services')),
      '@store': path.resolve(__dirname, vueSrc.concat('/store')),
      '@styles': path.resolve(__dirname, vueSrc.concat('/styles')),
      '~@bcgov': path.resolve(__dirname, 'node_modules/@bcgov'),
      '~bootstrap': path.resolve(__dirname, 'node_modules/bootstrap'),
      '~bootstrap-vue': path.resolve(__dirname, 'node_modules/bootstrap-vue'),
    },
    extensions: ['.ts', '.vue', '.json', '.scss', '.svg', '.png', '.jpg'],
  },
  modules: [vueSrc],
  server: {
    host: '0.0.0.0',
    port: 1339,
    https: true,
    proxy: {
      '^/api': {
        target: 'http://api:5000',
        changeOrigin: true,
        headers: {
          Connection: 'keep-alive',
          'X-Forwarded-Host': 'localhost',
          'X-Forwarded-Port': '8080',
          'X-Base-Href': '/',
        },
      },
    },
  },
});
