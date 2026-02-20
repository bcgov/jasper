import basicSsl from '@vitejs/plugin-basic-ssl';
import vue from '@vitejs/plugin-vue';
import Components from 'unplugin-vue-components/vite';
import { defineConfig } from 'vite';
import svgLoader from 'vite-svg-loader';

const path = require('path');
const vueSrc = 'src';
const wsProxyOrigin =
  process.env.VITE_WS_PROXY_ORIGIN || 'https://localhost:8080';
const wsProxyInsecure = process.env.VITE_WS_PROXY_INSECURE === 'true';

console.log('[vite] VITE_WS_PROXY_ORIGIN:', wsProxyOrigin);
console.log('[vite] VITE_WS_PROXY_INSECURE:', wsProxyInsecure);

export default defineConfig({
  base:
    process.env.NODE_ENV === 'production' ? '/S2I_INJECT_PUBLIC_PATH/' : '/',
  plugins: [vue(), svgLoader(), Components({}), basicSsl()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, vueSrc),
      '@assets': path.resolve(__dirname, vueSrc.concat('/assets')),
      '@components': path.resolve(__dirname, vueSrc.concat('/components')),
      '@router': path.resolve(__dirname, vueSrc.concat('/router')),
      '@services': path.resolve(__dirname, vueSrc.concat('/services')),
      '@store': path.resolve(__dirname, vueSrc.concat('/store')),
      '@styles': path.resolve(__dirname, vueSrc.concat('/styles')),
      '~bootstrap': path.resolve(__dirname, 'node_modules/bootstrap'),
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
      '/hubs': {
        target: 'http://api:5000',
        changeOrigin: true,
        ws: true,
        secure: !wsProxyInsecure,
        headers: {
          'X-Forwarded-Host': 'localhost',
          'X-Forwarded-Port': '8080',
          'X-Base-Href': '/',
          Origin: wsProxyOrigin,
        },
        configure: (proxy) => {
          proxy.on('proxyReqWs', (proxyReq) => {
            proxyReq.setHeader('Origin', wsProxyOrigin);
          });
        },
      },
    },
  },
});
