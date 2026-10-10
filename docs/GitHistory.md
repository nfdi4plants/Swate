# Git history

The **Version history** button in Electron's left activity bar opens the history
sidebar while preserving the main content. Compact commit rows show the subject,
author, date and revision. File-status badges are visible before expansion:
**A** added, **D** deleted, **M** modified, **R** renamed, **C** copied and **T**
type changed. Each badge counts files, has a full accessible label and tooltip,
and wraps within narrow sidebars. Zero categories are omitted; an empty commit
shows a neutral **0 files** badge. **Counts unavailable** means its summary could not be loaded.
Expand a commit to load its changed files.
Click a file to compare that commit with its first parent in the existing paged
diff viewer. The first commit compares with an empty tree. Merge commits use
their first parent. File rows show the name, directory, change label and line
counts; hover for the full path and rename source. **Close comparison** returns
to the previous file or page. Workspace diff sessions close when leaving them,
so closing the comparison returns to an empty main area in that case. The sidebar
retains expanded commits and its scroll position while reading a comparison.

Comparisons start with equally sized Previous and Current panes. Each pane has
its own horizontal scrollbar; vertical scrolling keeps the rows aligned. Drag
the divider to resize the panes, or focus it and use Left/Right to adjust by 5%.
Double-click or press Enter to restore 50/50. The split stays between 15% and
85% and is retained while the viewer remains open.

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

History pages include optional summaries computed from the same NUL-delimited
name-status parser and first-parent comparison as the changed-file endpoint.
Root commits compare with an empty tree. Summary reads run in batches of at most
four, preserve commit order and do not request line statistics. A failed summary
leaves the commit available with no summary. File lists and line statistics load
on expansion; successfully loaded details also supply badges when a summary is
missing. Summary failure does not prevent opening file details or comparisons.

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
They also compare summaries with loaded file changes across two pinned pages,
and verify root commits and available merges against independent Git output.
Component unit tests cover status counting, and RTL tests verify all six badges,
zero counts, unavailable summaries and the loaded-details fallback.
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
- Inspect collapsed commits before requesting file lists, including later pages.
  Check the six status badges, zero-file commits and unavailable summaries. Use
  the narrow six-kind showcase to check badge wrapping and full hover labels.
