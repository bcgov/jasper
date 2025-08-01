import basicSsl from '@vitejs/plugin-basic-ssl';
import vue from '@vitejs/plugin-vue';
import { BootstrapVueNextResolver } from 'bootstrap-vue-next';
import Components from 'unplugin-vue-components/vite';
import { defineConfig } from 'vite';
import svgLoader from 'vite-svg-loader';

const path = require('path');
const vueSrc = 'src';

export default defineConfig({
  base:
    process.env.NODE_ENV === 'production' ? '/S2I_INJECT_PUBLIC_PATH/' : '/',
  plugins: [
    vue(),
    svgLoader(),
    Components({
      resolvers: [
        BootstrapVueNextResolver({
          aliases: {
            BInput: 'b-input',
            BCard: 'b-card',
            BCardTitle: 'b-card-title',
            BRow: 'b-row',
            BFormInput: 'b-form-input',
            BCardText: 'b-card-text',
            BFormSelect: 'b-form-select',
            BButton: 'b-button',
            BCol: 'b-col',
            BModal: 'b-modal',
            BOverlay: 'b-overlay',
            BNavbarNav: 'b-navbar-nav',
            BNavbar: 'b-navbar',
            BFormGroup: 'b-form-group',
            BInputGroup: 'b-input-group',
            BNavText: 'b-nav-text',
            BTableSimple: 'b-table-simple',
            BTable: 'b-table',
          },
        }),
      ],
    }),
    basicSsl(),
  ],
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
      '~bootstrap-vue-next': path.resolve(
        __dirname,
        'node_modules/bootstrap-vue-next'
      ),
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
  optimizeDeps: {
    exclude: ['bootstrap-vue-next'],
  },
});
