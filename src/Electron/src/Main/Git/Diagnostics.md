# Git Diagnostics Safety

Use this guidance when enabling debug logs for `simple-git` in Swate Electron.

## Safe baseline

- Prefer `DEBUG=simple-git` for minimal plugin/runtime diagnostics.
- Treat all git diagnostics as sensitive data.
- Redact `Authorization: Bearer ...` values before persisting or sharing logs.
- Redact sensitive values even when `DEBUG` is unset; exception messages and stack traces can still contain command arguments.

## Do not enable in token-bearing sessions

- `simple-git:task:*`
- `simple-git:output:*`

These channels can include full command arguments and process output where `http.extraHeader` values may appear.

## Additional constraints

- Do not log access tokens from provider callbacks.
- Do not expose token values in IPC payloads.
- Keep `unsafe` options disabled:
  - `allowUnsafeCustomBinary = false`
  - `allowUnsafeProtocolOverride = false`
  - `allowUnsafePack = false`

## Sharing logs safely

- Before sharing logs, remove every line containing `Authorization`, `Bearer`, or `http.extraHeader`.
- If unsure whether a log is clean, do not share it until a second reviewer confirms redaction.
