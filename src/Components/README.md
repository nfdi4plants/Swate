# Components

## Local Dev

### Tests

`npm test`

### Playground

`npm start`

### Run Storybook

`npm run storybook`

### Build Storybook

`npm run build:storybook`

### Build Npm Package

`npm run build`

### Build .NET Package

`npm run build:net`

### Release

#### .NET

1. Update version in `./src/Components.fsproj`
2. `npm run build:net` (creates tailwind style css and packs nuget package)
3. Upload nuget package from `./src/bin/Releases`

#### NPM

1. `npm version <new_version>`
2. `npm run build` (transpiles with fable, creates tsc types, and bundles with rollup)
3. (Due to [#701](https://github.com/nfdi4plants/Swate/issues/701)) replace `@layer base` with `@layer swt-base` in `dist/swate-components.css`
4. `npm publish --access public [--tag next]` (Use `--tag next` for prerelease)