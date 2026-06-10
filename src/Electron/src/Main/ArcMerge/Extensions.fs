namespace Main.ArcMerge

open ARCtrl

[<AutoOpen>]
module ArcMergeExtensions =

    // Used to handle initiated but empty datamaps that have a StaticHash of 0, which would otherwise be indistinguishable from unchanged datamaps.
    let private cleanEmptyDataMapStaticHash = System.Int32.MinValue

    let cleanDataMapStaticHash (dataMap: DataMap) =
        let hash = dataMap.GetHashCode()

        if hash = 0 then cleanEmptyDataMapStaticHash else hash

    type DataMap with
        member this.hasInMemoryChanges() : bool =
            if this.StaticHash = cleanEmptyDataMapStaticHash then
                this.GetHashCode() <> 0
            else
                this.StaticHash = 0 || this.StaticHash <> this.GetHashCode()

    let private hasStaticHashOrDataMapChanges staticHash lightHash (dataMap: DataMap option) =
        staticHash <> lightHash
        || (dataMap |> Option.exists (fun dm -> dm.hasInMemoryChanges ()))

    let private syncDataMapStaticHash (sourceDataMap: DataMap option) (targetDataMap: DataMap option) =
        match sourceDataMap, targetDataMap with
        | Some sourceDataMap, Some targetDataMap -> targetDataMap.StaticHash <- sourceDataMap.StaticHash
        | _ -> ()

    let private syncEntityStaticHashes
        (targetEntities: seq<'Entity>)
        (tryGetSource: string -> 'Entity option)
        (getIdentifier: 'Entity -> string)
        (syncEntityStaticHash: 'Entity -> 'Entity -> unit)
        =
        for targetEntity in targetEntities do
            targetEntity
            |> getIdentifier
            |> tryGetSource
            |> Option.iter (fun sourceEntity -> syncEntityStaticHash sourceEntity targetEntity)

    /// Syncs static hashes from source ARC to target ARC for matching entities.
    /// This keeps ARCtrl update contract generation scoped to actual changes.
    let syncArcStaticHashes (source: ARC) (target: ARC) : unit =
        target.StaticHash <- source.StaticHash

        match source.License, target.License with
        | Some sourceLicense, Some targetLicense -> targetLicense.StaticHash <- sourceLicense.StaticHash
        | _ -> ()

        syncEntityStaticHashes
            target.Studies
            source.TryGetStudy
            (fun (study: ArcStudy) -> study.Identifier)
            (fun sourceStudy targetStudy ->
                targetStudy.StaticHash <- sourceStudy.StaticHash
                syncDataMapStaticHash sourceStudy.DataMap targetStudy.DataMap
            )

        syncEntityStaticHashes
            target.Assays
            source.TryGetAssay
            (fun (assay: ArcAssay) -> assay.Identifier)
            (fun sourceAssay targetAssay ->
                targetAssay.StaticHash <- sourceAssay.StaticHash
                syncDataMapStaticHash sourceAssay.DataMap targetAssay.DataMap
            )

        syncEntityStaticHashes
            target.Workflows
            source.TryGetWorkflow
            (fun (workflow: ArcWorkflow) -> workflow.Identifier)
            (fun sourceWorkflow targetWorkflow ->
                targetWorkflow.StaticHash <- sourceWorkflow.StaticHash
                syncDataMapStaticHash sourceWorkflow.DataMap targetWorkflow.DataMap
            )

        syncEntityStaticHashes
            target.Runs
            source.TryGetRun
            (fun (run: ArcRun) -> run.Identifier)
            (fun sourceRun targetRun ->
                targetRun.StaticHash <- sourceRun.StaticHash
                syncDataMapStaticHash sourceRun.DataMap targetRun.DataMap
            )

    /// Copies ARC and preserves static hashes so unchanged entities are not treated as newly created.
    let copyArcPreservingStaticHashes (arc: ARC) : ARC =
        let copiedArc = arc.Copy()
        syncArcStaticHashes arc copiedArc
        copiedArc

    type ArcAssay with
        member this.hasInMemoryChanges() =
            hasStaticHashOrDataMapChanges this.StaticHash (this.GetLightHashCode()) this.DataMap

    type ArcStudy with
        member this.hasInMemoryChanges() =
            hasStaticHashOrDataMapChanges this.StaticHash (this.GetLightHashCode()) this.DataMap

    type ArcRun with
        member this.hasInMemoryChanges() =
            hasStaticHashOrDataMapChanges this.StaticHash (this.GetLightHashCode()) this.DataMap

    type ArcWorkflow with
        member this.hasInMemoryChanges() =
            hasStaticHashOrDataMapChanges this.StaticHash (this.GetLightHashCode()) this.DataMap

    type ARC with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                for assay in this.Assays do
                    isDirty <- isDirty || assay.hasInMemoryChanges ()

            if not isDirty then
                for study in this.Studies do
                    isDirty <- isDirty || study.hasInMemoryChanges ()

            if not isDirty then
                for run in this.Runs do
                    isDirty <- isDirty || run.hasInMemoryChanges ()

            if not isDirty then
                for workflow in this.Workflows do
                    isDirty <- isDirty || workflow.hasInMemoryChanges ()

            isDirty
