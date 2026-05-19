namespace Main.ArcMerge

open ARCtrl

[<AutoOpen>]
module ArcMergeExtensions =

    //Used to handle intiated but empty datamaps that have a StaticHash of 0, which would otherwise be indistinguishable from unchanged datamaps
    let private cleanEmptyDataMapStaticHash = System.Int32.MinValue

    let cleanDataMapStaticHash (dataMap: DataMap) =
        let hash = dataMap.GetHashCode()

        if hash = 0 then
            cleanEmptyDataMapStaticHash
        else
            hash

    type DataMap with
        member this.hasInMemoryChanges() : bool =
            if this.StaticHash = cleanEmptyDataMapStaticHash then
                this.GetHashCode() <> 0
            else
                this.StaticHash = 0 || this.StaticHash <> this.GetHashCode()

    type ArcAssay with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                match this.DataMap with
                | Some dm -> isDirty <- isDirty || dm.hasInMemoryChanges()
                | None -> ()

            isDirty

    type ArcStudy with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                match this.DataMap with
                | Some dm -> isDirty <- isDirty || dm.hasInMemoryChanges()
                | None -> ()

            isDirty

    type ArcRun with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                match this.DataMap with
                | Some dm -> isDirty <- isDirty || dm.hasInMemoryChanges()
                | None -> ()

            isDirty

    type ArcWorkflow with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                match this.DataMap with
                | Some dm -> isDirty <- isDirty || dm.hasInMemoryChanges()
                | None -> ()

            isDirty

    type ARC with
        member this.hasInMemoryChanges() =
            let mutable isDirty = this.StaticHash <> this.GetLightHashCode()

            if not isDirty then
                for assay in this.Assays do
                    isDirty <- isDirty || assay.hasInMemoryChanges()

            if not isDirty then
                for study in this.Studies do
                    isDirty <- isDirty || study.hasInMemoryChanges()

            if not isDirty then
                for run in this.Runs do
                    isDirty <- isDirty || run.hasInMemoryChanges()

            if not isDirty then
                for workflow in this.Workflows do
                    isDirty <- isDirty || workflow.hasInMemoryChanges()

            isDirty
