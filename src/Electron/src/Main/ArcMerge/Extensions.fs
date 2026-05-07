namespace Main.ArcMerge

open ARCtrl

[<AutoOpen>]
module ArcMergeExtensions =

    type DataMap with
        member this.hasInMemoryChanges() : bool =
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
