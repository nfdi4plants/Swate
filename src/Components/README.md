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

1. Update version in `./package.json`.
2. `npm run bundle` (transpiles with fable, creates tsc typed from jsdocs, and bundles with rollup)
3. `npm publish [--tag next]` (Use `--tag next` for prerelease)