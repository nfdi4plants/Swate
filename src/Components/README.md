# Components

## Local Dev

### Tests

`npm test`

### Playground

`npm start`

### Build

`npm run build`

### Release

#### .NET

1. Update version in `./src/Components.fsproj`
2. `npm run build:net` (creates tailwind style css and packs nuget package)
3. Upload nuget package from `./src/bin/Releases`

#### NPM

1. Update version in `./package.json`.
2. `npm run bundle` (transpiles with fable, creates tsc typed from jsdocs, and bundles with rollup)
3. `npm publish [--tag next]` (Use `--tag next` for prerelease)