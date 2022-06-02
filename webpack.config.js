// Template for webpack.config.js in Fable projects
// Find latest version in https://github.com/fable-compiler/webpack-config-template

// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options

var path = require('path');
var webpack = require('webpack');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require('mini-css-extract-plugin')

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
            target: 'http://localhost:5000',
            secure: false,
            changeOrigin: true,
            ignorePath: false
        },
        '/test': {
            target: 'http://localhost:5000',
            secure: false,
            changeOrigin: true,
            ignorePath: false
        },
        // redirect websocket requests that start with /socket/ to the server on the port 8085
        '/socket/**': {
            target: 'http://localhost:5000',
            ws: true,
            secure: false,
            changeOrigin: true,
            ignorePath: false
        }
    },
    babel: {
        presets: [
            ['@babel/preset-env', {
                modules: false,
                // This adds polyfills when needed. Requires core-js dependency.
                // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
                // Note that you still need to add custom polyfills if necessary (e.g. whatwg-fetch)
                useBuiltIns: 'usage',
                corejs: 3
            }]
        ],
    }
}

// If we're running the webpack-dev-server, assume we're in development mode
var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
var environment = isProduction ? 'production' : 'development';
process.env.NODE_ENV = environment;
console.log('Bundling for ' + environment + '...');

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
var commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: resolve(CONFIG.indexHtmlTemplate)
    })
];

module.exports = {
    // In development, split the JavaScript and CSS files in order to
    // have a faster HMR support. In production bundle styles together
    // with the code because the MiniCssExtractPlugin will extract the
    // CSS in a separate files.
    entry: isProduction ? {
        app: [resolve(CONFIG.fsharpEntry), resolve(CONFIG.cssEntry)]
    } : {
        app: resolve(CONFIG.fsharpEntry),
        style: resolve(CONFIG.cssEntry)
    },
    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: resolve(CONFIG.outputDir),
        publicPath: '/',
        filename: isProduction ? '[name].[fullhash].js' : '[name].js'
    },
    mode: isProduction ? 'production' : 'development',
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
    plugins: isProduction ?
        commonPlugins.concat([
            new MiniCssExtractPlugin({ filename: 'style.[name].[fullhash].css' }),
            new CopyWebpackPlugin({ patterns: [{ from: resolve(CONFIG.assetsDir) }] }),
        ])
        : commonPlugins,
    externals: {
        officejs: 'Office.js',
    },
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false
    },
    // Configuration for webpack-dev-server
    devServer: {
        // Necessary when using non-hash client-side routing
        // This assumes the index.html is accessible from server root
        // For more info, see https://webpack.js.org/configuration/dev-server/#devserverhistoryapifallback
        historyApiFallback: true,
        host: '0.0.0.0',
        port: CONFIG.devServerPort,
        server: {
            type: 'https',
            options: {
                key: "C:/Users/Kevin/.office-addin-dev-certs/localhost.key",
                cert: "C:/Users/Kevin/.office-addin-dev-certs/localhost.crt",
                ca: "C:/Users/Kevin/.office-addin-dev-certs/ca.crt"
            },
        },
        proxy: CONFIG.devServerProxy,
        devMiddleware: {
            publicPath: CONFIG.publicPath
        },
        static: {
            directory: resolve(CONFIG.assetsDir),
            publicPath: '/'
        }
    },
    // - sass-loaders: transforms SASS/SCSS into JS
    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
            {
                test: /\.m?js$/,
                exclude: /node_modules/,
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
};
function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
