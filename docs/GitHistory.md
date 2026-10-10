# Git history

The **Version history** button in Electron's left activity bar opens the history
sidebar while preserving the main content. Compact commit rows show the subject,
author, date and revision. Expand a commit to load its changed files.
Click a file to compare that commit with its first parent in the existing paged
diff viewer. The first commit compares with an empty tree. Merge commits use
their first parent. File rows show the name, directory, change label and line
counts; hover for the full path and rename source. **Close comparison** returns
to the previous file or page. Workspace diff sessions close when leaving them,
so closing the comparison returns to an empty main area in that case. The sidebar
retains expanded commits and its scroll position while reading a comparison.

History follows the active branch and loads 30 commits at a time. Further pages
are pinned to the first page's HEAD. **Refresh** loads the current branch again.
This version does not search history or browse other branches.

## Implementation

`Page/GitHistory` in the Components project receives data and callbacks from its
host. It has no dependency on Electron, ARC state or a version-control provider.
`GitHistorySidebar` hosts it in Electron, with state owned by `GitHistoryContext`.
There is no main-content history page; that area is used for selected file diffs.

`IGitHistoryApi` is a separate typed IPC bridge. All native Git reads, argument
validation and patch parsing live in `Main/IPC/GitHistory.fs`. Calls resolve the
vault of the calling window and require that folder to be the Git repository
root. They never modify the index, files, branches or refs, and do not change the
existing provider workflow.

Native process options are plain JavaScript objects. Git configuration is
isolated with `NUL` on Windows and `/dev/null` elsewhere, and inherited Git
directory/configuration overrides are removed. Failures distinguish a folder
outside Git from a missing Git executable and retain Git's diagnostic message.

The history comparison shows a committed patch with three context lines per
hunk, with whole-line highlights. Context expansion is unavailable for these
patches. Binary files (including `.xlsx`), Git LFS pointers, submodules and
non-UTF-8 patches show an unavailable explanation. Native reads have a 30-second
timeout and a 16 MiB output limit; displayed patches have an 8 MiB limit. Recovery
guidance is informational; there is no restore action.

## Verification commands

Run commands from the repository root. Build checks compile F#; they do not
transpile or run Electron or Storybook.

```powershell
dotnet build ./src/Electron/src/Swate.Electron.fsproj -m:1

# Component showcase and its browser checks.
npm --prefix src/Components run storybook
npm --prefix src/Components run test:storybook -- src/Page/GitHistory/GitHistory.stories.tsx

# Compile and launch Electron for manual integration checks.
npm --prefix src/Electron run start

# Focused history integration tests; all Git commands are read-only.
Push-Location tests/Electron.Core
dotnet fable -o output -s --define SWATE_ENVIRONMENT
npx vitest run output/GitHistory.test.js
Pop-Location

# Existing component and Electron regression suites (run sequentially).
npm --prefix src/Components run test:unit
npm --prefix src/Components run test:rtl
dotnet run --project ./build/Build.fsproj -- test run electron-core
```

The component scripts clean and transpile shared output. Stop the Storybook
watcher before running component test commands; do not run them together.
The focused history tests use this checkout's existing commits and require a
recent `.fs`, `.md` or `.tsx` change for their text-diff scenario. They compare
HEAD, index contents and workspace status before and after history reads.
If Fable project discovery fails with `NU1900` because NuGet's vulnerability
service is unreachable, temporarily set `$env:NuGetAudit = 'false'` in that
verification shell and retry. Restore its previous value afterward. This
Fable version treats MSBuild warning output as a discovery failure.

## Manual scenarios

- Open history on a repository with commits and on an initialized repository
  with no commits. A folder outside a repository should show a useful error.
- Expand commits with added, edited, deleted and renamed files. Check labels,
  old paths and optional line counts; click each file and verify the revision
  labels and displayed patch against its first parent.
- Check the initial commit, a merge commit, an empty commit and a rename with
  unchanged contents. An empty text patch should remain a valid comparison.
- Select binary and Git LFS files and confirm that their contents are described
  as unavailable instead of displaying a pointer as the research file.
- Open history while editing a file and check that the main page is preserved.
  Load older commits, expand several entries, select files and close a diff.
  Check selected-file highlighting and retained sidebar scroll position.
- Rapidly select different files, refresh, or open a different ARC while requests
  are running. Replies from the old selection or workspace must not replace the
  current view. Check branch changes and new commits after refreshing history.
- Inspect light/dark themes, narrow layouts and keyboard-only expansion/file
  selection in the native GitHistory Storybook showcases.
