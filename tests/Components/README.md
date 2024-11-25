These tests are run using vitest with react component test library.

The components tested can be found in `/src/Components`.

The tests are run using the command `npm run test`, which will start both a fable transpilation watcher on the files in `/src/Components` as well as a vitest watcher on the files in `/tests/Components`.

The React components are transpiled to `/tests/Components/transpiled`, which is **not tracked with git**.