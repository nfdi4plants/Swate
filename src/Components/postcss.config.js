console.log('PostCSS config loaded');

module.exports = {
  plugins: {
    tailwindcss: require('./tailwind.config.js'),
    autoprefixer: {},
  },
}
