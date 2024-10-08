const path = require("path");
const vueSrc = "src";

//I noticed when we don't have publicPath set to /, it becomes problematic using vue-cli-tools - it doesn't seem to respect history paths.
//EX. jasper/civil-files/5555 wont work when you navigate directly to it with vue-cli-tools versus NGINX it works fine.
//When deployed over NGINX this problem seems to go away. So I've left it as / for now in local development environments.
module.exports = {
	publicPath:  process.env.NODE_ENV == 'production' ? '/S2I_INJECT_PUBLIC_PATH/' : '/',
	//chainWebpack: config => config.optimization.minimize(false), Disable minification.
	configureWebpack: {
		devServer: {
			historyApiFallback: true,
			host: '0.0.0.0',
			port: 1339,
			https: true,
			watchOptions: {
				ignored: /node_modules/,
				aggregateTimeout: 300,
				poll: 1000,
			},
			proxy: {
				//This is for WEB_BASE_HREF = '/' specifically.
				//If having problems connecting, try adding: netsh http add iplisten 127.0.0.1
				'^/api': {
					target: "http://api:5000",
					headers: {
						Connection: 'keep-alive',
						'X-Forwarded-Host': 'localhost',
						'X-Forwarded-Port': '8080',
						'X-Base-Href': '/'
					},
					changeOrigin: true
				}
			}
		},
		resolve: {
			modules: [vueSrc],
			alias: {
				"@": path.resolve(__dirname, vueSrc),
				"@assets": path.resolve(__dirname, vueSrc.concat("/assets")),
				"@components": path.resolve(__dirname, vueSrc.concat("/components")),
				"@router": path.resolve(__dirname, vueSrc.concat("/router")),
				"@store": path.resolve(__dirname, vueSrc.concat("/store")),
				"@styles": path.resolve(__dirname, vueSrc.concat("/styles"))
			},
			extensions: ['.ts', '.vue', '.json', '.scss', '.svg', '.png', '.jpg']
		}
	}
};
