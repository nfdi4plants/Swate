# File Explorer Architecture

This document describes the current explorer architecture in the Electron application.

The app now keeps two parallel models:

- A flat filesystem `FileEntry` list for note search, path-based helpers, and low-level file refreshes.
- An ARC object tree (`ArcExplorerNode list`) for the sidebar explorer.

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant FE as File Explorer UI
    participant RA as Renderer App
    participant RFE as Renderer FileExplorer Adapter
    participant IPC as Main IPC
    participant AV as ArcVault
    participant FTC as FileTreeCreator
    participant AET as ArcExplorerTreeCreator
    participant PV as Preview Pane

    U->>IPC: Open ARC
    IPC->>AV: Open or focus vault
    AV->>AV: LoadArc()
    AV->>FTC: getFileEntries(arcPath)
    FTC-->>AV: FileEntry[]
    AV->>AV: createFileEntryTree(FileEntry[])
    AV->>AET: createArcExplorerTree(arc, fileEntries)
    AET-->>AV: ArcExplorerNode list
    AV-->>RA: fileTreeUpdate(Dictionary<string, FileEntry>)
    AV-->>RA: arcExplorerTreeUpdate(ArcExplorerNode list)

    RA->>RA: Store FileTree and ArcExplorerTree in WorkspaceState
    RA->>RFE: createArcExplorer(...)
    RFE->>FE: createFileTree(...selectedTreeItemPath...)
    FE-->>U: Render ARC tree

    U->>FE: Click ARC node, note, sample, or group
    FE->>RFE: onItemClick(item)

    alt Group or virtual node
        RFE->>RA: setPageState(None)
        RA-->>U: Selection only
    else Object or file-backed node
        RFE->>RFE: resolvePreviewPath(path)
        Note over RFE: Datamap clicks are redirected to\nisa.assay/study/workflow/run.xlsx
        RFE->>RA: setSelectedTreeItemPath(previewPath)
        RFE->>RA: setSelectedExplorerItemId(item.Id)
        RFE->>IPC: openFile(previewPath)
        IPC->>AV: Resolve file from loaded ARC or filesystem
        AV-->>IPC: Arc object JSON or text content
        IPC-->>RFE: PageState
        RFE->>RA: setPageState(PageState)
        RA->>RA: Parse ArcFileData to ArcFiles option
        RA->>PV: Render metadata/table/datamap/text preview
        PV-->>U: Show selected content
    end

    rect rgb(245,245,245)
        Note over AV,RA: Background refresh path
        AV->>AV: File watcher detects change
        AV->>FTC: getFileEntries(arcPath)
        FTC-->>AV: Updated FileEntry[]
        AV->>AET: Rebuild ArcExplorerNode list from ARC + file entries
        AV-->>RA: fileTreeUpdate(...)
        AV-->>RA: arcExplorerTreeUpdate(...)
        RA->>RFE: Rebuild tree and preserve selected path and expanded nodes where possible
    end
```

## Main Pieces

- `src/Electron/src/Main/FileTreeCreator.fs`
  Scans the ARC directory, filters ignored paths, and annotates files with Git LFS tracking information.

- `src/Electron/src/Main/ArcVault.fs`
  Owns the loaded ARC, watches the filesystem, and pushes both file tree and ARC explorer updates to the renderer.

- `src/Electron/src/Main/ArcExplorerTreeCreator.fs`
  Builds the ARC object-based sidebar tree from `vault.arc` plus filesystem-derived notes.

- `src/Electron/src/Swate.Electron.Shared/FileIOTypes.fs`
  Defines the flat `FileEntry` transport type and the shared `ArcExplorerNode` contract used by the object tree.

- `src/Electron/src/Renderer/App.fs`
  Stores both `FileTree` and `ArcExplorerTree` in renderer workspace state and selects the sidebar item by explorer id or fallback path.

- `src/Electron/src/Renderer/Components/FileExplorer.fs`
  Adapts the shared ARC object tree into the reusable explorer component and maps object/file clicks to preview requests.

- `src/Components/src/FileExplorer/FileTreeDataStructures.fs`
  Contains the reusable file tree model, update logic, and helper operations.

- `src/Components/src/FileExplorer/FileExplorer.fs`
  Renders the generic file explorer UI, including selection, expansion, breadcrumbs, and context menu hooks.

- `src/Components/src/FileExplorer/FileExplorerBreadcrumbs.fs`
  Renders the breadcrumb trail for the selected tree node.

- `src/Components/src/FileExplorer/FileExplorerGitLfsHelper.fs`
  Builds Git LFS context menu actions and resolves repository-relative file paths.

## Notes

- The sidebar is ARC-object-first. Studies, assays, workflows, runs, notes, and samples are represented as domain nodes, not reconstructed from path segments in the renderer.
- Relationship edges are rendered as reference nodes:
  - `Study -> Assay`
  - `Workflow -> Subworkflow`
  - `Run -> Workflow`
- Notes remain filesystem-derived from `notes/...` paths.
- Samples are virtual nodes derived from ARC tables by collecting sample identifiers from `IOType.Sample` columns.
- Datamap file clicks are still resolved through the owning ISA object path before preview.
- The reusable explorer component is still application-agnostic; Electron-specific behavior remains in the renderer adapter.
