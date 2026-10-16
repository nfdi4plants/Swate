// Runs `electron-forge make` without the forge CLI wrapper. The wrapper's commander version
// exits 0 when make is killed by a signal (an out of memory abort, for example), which hid
// the failed macOS build of release 2.6.0 (#1421). Here a signal turns into exit code 1.
// Calling the make entry directly skips the CLI's Node version and package manager checks.
// Delete this file and go back to `electron-forge make` once the forge CLI exits non-zero on signals.
const { spawnSync } = require('node:child_process');

const makeEntry = require.resolve('@electron-forge/cli/dist/electron-forge-make.js');

const result = spawnSync(
  process.execPath,
  [...process.execArgv, makeEntry, ...process.argv.slice(2)],
  {
    stdio: 'inherit',
  },
);

if (result.error) {
  throw result.error;
}

if (result.signal) {
  console.error(`electron-forge make was killed by ${result.signal}`);
}

process.exit(result.status ?? 1);
