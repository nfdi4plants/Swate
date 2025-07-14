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

0. Release normally and copy `<new_version>` from console output.
1. `npm version <new_version> --no-git-tag-version`
2. `npm run build` (transpiles with fable, creates tsc types, and bundles with rollup)
3. (Due to [#701](https://github.com/nfdi4plants/Swate/issues/701)) replace `@layer base` with `@layer swt-base` in `dist/swate-components.css`
4. `npm publish --access public --tag next` (Use `--tag next` for prerelease)

#### Publish detailed

1. go to main branch
2. ensure latest commit is locally pulled
3. Open powershell / cmd in swate root
4. ./build.cmd release pre
5. Wait for version editting. Write beta.XX where XX is the current beta version
6. Wait for tests
7. Confirm force push to nightly
8. Wait a few minutes and check if gh-actions for release finished successfully: https://github.com/nfdi4plants/Swate/actions
9. When the release is finished go to the main page of swate (https://github.com/nfdi4plants/Swate), and click on Releases on the left side (https://github.com/nfdi4plants/Swate/releases)
10. Select the tag of the new release and click "Generate release notes", then click on "Publish release" 