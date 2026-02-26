import basicSsl from '@vitejs/plugin-basic-ssl';
import vue from '@vitejs/plugin-vue';
import Components from 'unplugin-vue-components/vite';
import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import svgLoader from 'vite-svg-loader';

const path = require('path');
const vueSrc = 'src';

export default defineConfig(({ mode }) => {
  const isProd = Boolean(process.env.VITE_ENV?.includes('prod'));

  // Base plugins array
  const plugins = [vue(), svgLoader(), Components({}), basicSsl()];

  // Conditionally static copy prod-only files
  if (isProd) {
    plugins.push(
      viteStaticCopy({
        targets: [
          {
            src: path.resolve(__dirname, 'snowplow.js'),
            dest: '.',
          },
        ],
      }),
      // Inject snowplow script tag in HTML for prod builds
      {
        name: 'inject-snowplow',
        transformIndexHtml(html) {
          return html.replace(
            '</head>',
            '    <script src="/snowplow.js" charset="UTF-8"></script>\n  </head>'
          );
        },
      }
    );
  }

  return {
    base:
      process.env.NODE_ENV === 'production' ? '/S2I_INJECT_PUBLIC_PATH/' : '/',
    plugins,
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
      },
    },
  };
});
