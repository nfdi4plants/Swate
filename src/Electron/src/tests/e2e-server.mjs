import { spawn } from 'node:child_process';
import fs from 'node:fs';
import http from 'node:http';
import os from 'node:os';
import path from 'node:path';

const helperPort = Number(process.env.SWATE_ELECTRON_E2E_HELPER_PORT ?? 39001);
const debugPort = Number(process.env.SWATE_ELECTRON_E2E_DEBUG_PORT ?? 9223);
const resultsDir = path.resolve('src/tests/test-results');
const metadataPath = path.join(resultsDir, 'e2e-env.json');
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'swate-electron-e2e-'));
const tempHomeDir = path.join(tempRoot, 'home');
const tempArcParentDir = path.join(tempRoot, 'arcs');

fs.mkdirSync(tempHomeDir, { recursive: true });
fs.mkdirSync(tempArcParentDir, { recursive: true });
fs.mkdirSync(resultsDir, { recursive: true });

let status = 'starting';
let errorMessage = '';
let forgeProcess = undefined;

const metadata = {
  helperPort,
  debugPort,
  tempRoot,
  tempHomeDir,
  tempArcParentDir,
  fableBuildStartMs: 0,
  fableBuildEndMs: 0,
  forgeStartMs: 0,
  readyAtMs: 0,
};

function writeMetadata() {
  fs.writeFileSync(metadataPath, `${JSON.stringify(metadata, null, 2)}\n`, 'utf8');
}

function sendJson(response, statusCode, payload) {
  response.writeHead(statusCode, { 'content-type': 'application/json' });
  response.end(JSON.stringify(payload));
}

const server = http.createServer((request, response) => {
  if (request.url === '/health') {
    if (status === 'ready') {
      sendJson(response, 200, { status, metadataPath });
    } else {
      sendJson(response, 503, { status, error: errorMessage });
    }
    return;
  }

  sendJson(response, 404, { status: 'not-found' });
});

server.listen(helperPort, '127.0.0.1');
writeMetadata();

function shouldUseShell() {
  return process.platform === 'win32';
}

function runCommand(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      stdio: 'inherit',
      shell: shouldUseShell(),
      ...options,
    });

    child.on('error', reject);
    child.on('exit', (code, signal) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error(`${command} ${args.join(' ')} exited with code ${code ?? 'null'} signal ${signal ?? 'null'}`));
      }
    });
  });
}

function failFast(message) {
  status = 'failed';
  errorMessage = message;
  setTimeout(() => process.exit(1), 100);
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitForDebugEndpoint() {
  const deadline = Date.now() + 120000;
  const endpoint = `http://127.0.0.1:${debugPort}/json/version`;

  while (Date.now() < deadline) {
    try {
      const response = await fetch(endpoint);
      if (response.ok) {
        return;
      }
    } catch {
      // Keep polling until Electron exposes the CDP endpoint.
    }

    await wait(500);
  }

  throw new Error(`Timed out waiting for Electron CDP endpoint at ${endpoint}`);
}

function killForgeProcess() {
  if (!forgeProcess || forgeProcess.killed) {
    return;
  }

  if (process.platform === 'win32') {
    spawn('taskkill', ['/pid', String(forgeProcess.pid), '/T', '/F'], { stdio: 'ignore' });
  } else {
    try {
      process.kill(-forgeProcess.pid, 'SIGTERM');
    } catch {
      forgeProcess.kill('SIGTERM');
    }
  }
}

function cleanup() {
  killForgeProcess();
  server.close();

  try {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  } catch {
    // Best-effort cleanup only.
  }
}

process.on('SIGINT', () => {
  cleanup();
  process.exit(130);
});

process.on('SIGTERM', () => {
  cleanup();
  process.exit(143);
});

process.on('exit', cleanup);

async function start() {
  try {
    status = 'building-fable';
    metadata.fableBuildStartMs = Date.now();
    writeMetadata();

    fs.rmSync(path.resolve('src/fable_output'), { recursive: true, force: true });
    await runCommand('npm', ['run', 'fable']);

    metadata.fableBuildEndMs = Date.now();
    metadata.forgeStartMs = Date.now();
    writeMetadata();

    status = 'starting-forge';
    forgeProcess = spawn(
      'npx',
      [
        'electron-forge',
        'start',
        '--',
        `--remote-debugging-port=${debugPort}`,
        `--user-data-dir=${tempHomeDir}`,
      ],
      {
        stdio: 'inherit',
        shell: shouldUseShell(),
        detached: process.platform !== 'win32',
        env: {
          ...process.env,
          HOME: tempHomeDir,
          USERPROFILE: tempHomeDir,
          SWATE_ELECTRON_E2E: '1',
          SWATE_ELECTRON_E2E_CREATE_ARC_PARENT: tempArcParentDir,
        },
      },
    );

    forgeProcess.on('exit', (code, signal) => {
      if (status !== 'ready') {
        failFast(`electron-forge exited before ready with code ${code ?? 'null'} signal ${signal ?? 'null'}`);
      }
    });

    await waitForDebugEndpoint();

    metadata.readyAtMs = Date.now();
    writeMetadata();
    status = 'ready';
  } catch (error) {
    failFast(error instanceof Error ? error.message : String(error));
    console.error(error);
  }
}

start();
