// In most cases, you'll only need to edit the CONFIG/TEST_CONFIG objects
// CONFIG is the configuration used to run the application.
// TEST_CONFIG is the configuration used to run tests.
// If you need better fine-tuning of Webpack options check the buildConfig function.
var CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: './src/Client/index.html',
    fsharpEntry: './src/Client/output/Client.fs.js',
    cssEntry: './src/Client/style.scss',
    outputDir: './deploy/public',
    assetsDir: './src/Client/public',
    devServerPort: 3000,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        // redirect requests that start with /api/ to the server on port 3000
        '/api/**': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || '5000'),
            secure: false,
            changeOrigin: true,
            ignorePath: false
        },
        '/test': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || '5000'),
            secure: false,
            changeOrigin: true,
            ignorePath: false
        },
        // redirect websocket requests that start with /socket/ to the server on the port 8085
        '/socket/**': {
            target: 'http://localhost:' + (process.env.SERVER_PROXY_PORT || '5000'),
            ws: true,
            secure: false,
            changeOrigin: true,
            ignorePath: false
        }
    },
    babel: {
        presets: [
            [
                '@babel/preset-env',
                {
                    modules: false,
                    // This adds polyfills when needed. Requires core-js dependency.
                    // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
                    // Note that you still need to add custom polyfills if necessary (e.g. whatwg-fetch)
                    useBuiltIns: 'usage',
                    corejs: 3
                    //targets: api.caller(caller => caller && caller.target === "node")
                    //    ? { node: "current" }
                    //    : { chrome: "58", ie: "11" }
                }
            ]
        ],
    }
}

var TEST_CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: 'tests/Client/index.html',
    fsharpEntry: 'tests/Client/output/Client.Tests.fs.js',
    outputDir: 'tests/Client',
    assetsDir: 'tests/Client',
    devServerPort: 8081,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: undefined,
    babel: undefined,
    cssEntry: undefined,
}

var path = require('path');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require('mini-css-extract-plugin')

module.exports = function (env, arg) {

    // Mode is passed as a flag to npm run. see the docs for more details on flags https://webpack.js.org/api/cli/#flags
    const mode = arg.mode ?? 'development';
    // environment variables docs: https://webpack.js.org/api/cli/#environment-options
    const config = env.test ? TEST_CONFIG : CONFIG;
    const isProduction = mode === 'production';

    console.log(`Bundling for ${env.test ? 'test' : 'run'} - ${mode} ...`);

    return {
        // In development, split the JavaScript and CSS files in order to
        // have a faster HMR support. In production bundle styles together
        // with the code because the MiniCssExtractPlugin will extract the
        // CSS in a separate files.
        entry: isProduction ? {
            app: [resolve(config.fsharpEntry), resolve(config.cssEntry)]
        } : {
                app: resolve(config.fsharpEntry),
                style: resolve(config.cssEntry)
        },
        // Add a hash to the output file name in production
        // to prevent browser caching if code changes
        output: {
            path: resolve(config.outputDir),
            publicPath: '/',
            filename: isProduction ? '[name].[contenthash].js' : '[name].js'
        },
        mode: mode,
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        optimization: {
            runtimeChunk: "single",
            moduleIds: 'deterministic',
            // Split the code coming from npm packages into a different file.
            // 3rd party dependencies change less often, let the browser cache them.
            splitChunks: {
                chunks: 'all'
            },
        },
        target: ["web", "es5"],
        // Besides the HtmlPlugin, we use the following plugins:
        // PRODUCTION
        //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
        //          To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production
        //      - CopyWebpackPlugin: Copies static assets to output directory
        // DEVELOPMENT
        //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
        plugins: [
            // ONLY PRODUCTION
            // MiniCssExtractPlugin: Extracts CSS from bundle to a different file
            // To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production
            isProduction && new MiniCssExtractPlugin({ filename: 'style.[name].[contenthash].css' }),
            // CopyWebpackPlugin: Copies static assets to output directory
            isProduction && new CopyWebpackPlugin({
                patterns: [
                    { from: resolve(config.assetsDir) },
                    { from: resolve('./src/Client/docs'), to: 'docs' },
                ]
            }),

            // PRODUCTION AND DEVELOPMENT
            // HtmlWebpackPlugin allows us to use a template for the index.html page
            // and automatically injects <script> or <link> tags for generated bundles.
            new HtmlWebpackPlugin({ filename: 'index.html', template: resolve(config.indexHtmlTemplate) })
        ].filter(Boolean),
        externals: {
            officejs: 'Office.js',
        },
        resolve: {
            // See https://github.com/fable-compiler/Fable/issues/1490
            symlinks: false
        },
        // Configuration for webpack-dev-server
        devServer: {
            static: [
                {
                    directory: resolve(config.assetsDir),
                    publicPath: '/'
                },
                {
                    directory: resolve('./src/Client/docs'),
                    publicPath: '/docs'
                },
            ],
            // Necessary when using non-hash client-side routing
            // This assumes the index.html is accessible from server root
            // For more info, see https://webpack.js.org/configuration/dev-server/#devserverhistoryapifallback
            host: '0.0.0.0',
            port: config.devServerPort,
            proxy: config.devServerProxy,
            hot: true,
            historyApiFallback: true,
            server: {
                type: 'https',
                options: {
                    key: "C:/Users/Kevin/.office-addin-dev-certs/localhost.key",
                    cert: "C:/Users/Kevin/.office-addin-dev-certs/localhost.crt",
                    ca: "C:/Users/Kevin/.office-addin-dev-certs/ca.crt"
                    //key: "{USERFOLDER}/.office-addin-dev-certs/localhost.key",
                    //cert: "{USERFOLDER}/.office-addin-dev-certs/localhost.crt",
                    //ca: "{USERFOLDER}/.office-addin-dev-certs/ca.crt"
                },
            },
        },
        // - sass-loaders: transforms SASS/SCSS into JS
        // - babel-loader: transforms JS to old syntax (compatible with old browsers)
        // - file-loader: Moves files referenced in the code (fonts, images) into output folder
        module: {
            rules: [
                {
                    test: /\.m?js$/,
                    exclude: /(node_modules|bower_components)/,
                    use: {
                        loader: 'babel-loader',
                        options: CONFIG.babel
                    },
                },
                {
                    test: /\.(sass|scss|css)$/,
                    use: [
                        isProduction
                            ? MiniCssExtractPlugin.loader
                            : 'style-loader',
                        'css-loader',
                        {
                            loader: 'sass-loader',
                            options: { implementation: require('sass') }
                        }
                    ],
                },
                {
                    test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                    use: ['file-loader']
                },
                {
                    test: /\.js$/,
                    enforce: "pre",
                    use: ['source-map-loader'],
                }
            ]
        }
    }
};
function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
