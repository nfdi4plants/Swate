namespace Main.ArcMerge

open ARCtrl
open Main.ArcMerge.ArcMergeExtensions

module ArcMergeHelper =

    type ParsedFileEvent = {
        EventName: EventName
        Path: string
        EntityRef: ArcEntityRef
    }

    type DataMapEventIndex = {
        Assays: Set<string>
        Studies: Set<string>
        Runs: Set<string>
        Workflows: Set<string>
    }

    let private emptyDataMapEventIndex = {
        Assays = Set.empty
        Studies = Set.empty
        Runs = Set.empty
        Workflows = Set.empty
    }

    let private cloneDataMapOption (dataMap: DataMap option) : DataMap option =
        dataMap |> Option.map (fun dm -> dm.Copy())

    let internal parseFileEvents (events: FileEvent list) : ParsedFileEvent list =
        events
        |> List.map (fun event -> {
            EventName = event.EventName
            Path = event.Path
            EntityRef = ArcEntityRef.fromPath event.Path
        })

    let internal buildDataMapEventIndex (events: ParsedFileEvent list) : DataMapEventIndex =
        events
        |> List.fold
            (fun state event ->
                match event.EntityRef with
                | ArcEntityRef.AssayDataMap id -> {
                    state with
                        Assays = Set.add id state.Assays
                  }
                | ArcEntityRef.StudyDataMap id -> {
                    state with
                        Studies = Set.add id state.Studies
                  }
                | ArcEntityRef.RunDataMap id -> {
                    state with
                        Runs = Set.add id state.Runs
                  }
                | ArcEntityRef.WorkflowDataMap id -> {
                    state with
                        Workflows = Set.add id state.Workflows
                  }
                | _ -> state
            )
            emptyDataMapEventIndex

    let private copyInvestigationFields (arcLocal: ARC) (arcRemote: ARC) : unit =
        let remoteSnapshot = arcRemote.Copy()
        arcLocal.Title <- remoteSnapshot.Title
        arcLocal.Description <- remoteSnapshot.Description
        arcLocal.SubmissionDate <- remoteSnapshot.SubmissionDate
        arcLocal.PublicReleaseDate <- remoteSnapshot.PublicReleaseDate
        arcLocal.OntologySourceReferences <- remoteSnapshot.OntologySourceReferences
        arcLocal.Publications <- remoteSnapshot.Publications
        arcLocal.Contacts <- remoteSnapshot.Contacts
        arcLocal.Comments <- remoteSnapshot.Comments
        arcLocal.Remarks <- remoteSnapshot.Remarks
        arcLocal.License <- remoteSnapshot.License

    let internal applyEntityEvent
        (arcLocal: ARC)
        (arcRemote: ARC)
        (dataMapEvents: DataMapEventIndex)
        (entityRef: ArcEntityRef)
        (eventName: EventName)
        : unit =
        match entityRef, eventName with
        | ArcEntityRef.Investigation, (EventName.Add | EventName.Change) -> copyInvestigationFields arcLocal arcRemote
        | ArcEntityRef.Investigation, EventName.Unlink -> ()
        | ArcEntityRef.Assay id, (EventName.Add | EventName.Change) ->
            match arcRemote.TryGetAssay(id) with
            | None -> ()
            | Some remoteAssay ->
                match arcLocal.Assays |> Seq.tryFindIndex (fun assay -> assay.Identifier = id) with
                | None -> arcLocal.AddAssay(remoteAssay.Copy())
                | Some idx ->
                    let discCopy = remoteAssay.Copy()

                    if not (Set.contains id dataMapEvents.Assays) then
                        discCopy.DataMap <- cloneDataMapOption arcLocal.Assays.[idx].DataMap

                    arcLocal.Assays.[idx] <- discCopy
        | ArcEntityRef.Assay id, EventName.Unlink ->
            if arcLocal.ContainsAssay(id) then
                arcLocal.RemoveAssay(id)
        | ArcEntityRef.AssayDataMap id, (EventName.Add | EventName.Change) ->
            match arcLocal.TryGetAssay(id) with
            | None -> ()
            | Some localAssay ->
                match arcRemote.TryGetAssay(id) with
                | None -> ()
                | Some remoteAssay -> localAssay.DataMap <- cloneDataMapOption remoteAssay.DataMap
        | ArcEntityRef.AssayDataMap id, EventName.Unlink ->
            match arcLocal.TryGetAssay(id) with
            | None -> ()
            | Some localAssay -> localAssay.DataMap <- None
        | ArcEntityRef.Study id, (EventName.Add | EventName.Change) ->
            match arcRemote.TryGetStudy(id) with
            | None -> ()
            | Some remoteStudy ->
                match arcLocal.Studies |> Seq.tryFindIndex (fun study -> study.Identifier = id) with
                | None -> arcLocal.AddStudy(remoteStudy.Copy())
                | Some idx ->
                    let discCopy = remoteStudy.Copy()

                    if not (Set.contains id dataMapEvents.Studies) then
                        discCopy.DataMap <- cloneDataMapOption arcLocal.Studies.[idx].DataMap

                    arcLocal.Studies.[idx] <- discCopy
        | ArcEntityRef.Study id, EventName.Unlink ->
            if arcLocal.ContainsStudy(id) then
                arcLocal.RemoveStudy(id)
        | ArcEntityRef.StudyDataMap id, (EventName.Add | EventName.Change) ->
            match arcLocal.TryGetStudy(id) with
            | None -> ()
            | Some localStudy ->
                match arcRemote.TryGetStudy(id) with
                | None -> ()
                | Some remoteStudy -> localStudy.DataMap <- cloneDataMapOption remoteStudy.DataMap
        | ArcEntityRef.StudyDataMap id, EventName.Unlink ->
            match arcLocal.TryGetStudy(id) with
            | None -> ()
            | Some localStudy -> localStudy.DataMap <- None
        | ArcEntityRef.Run id, (EventName.Add | EventName.Change) ->
            match arcRemote.TryGetRun(id) with
            | None -> ()
            | Some remoteRun ->
                match arcLocal.Runs |> Seq.tryFindIndex (fun run -> run.Identifier = id) with
                | None -> arcLocal.AddRun(remoteRun.Copy())
                | Some idx ->
                    let discCopy = remoteRun.Copy()

                    if not (Set.contains id dataMapEvents.Runs) then
                        discCopy.DataMap <- cloneDataMapOption arcLocal.Runs.[idx].DataMap

                    arcLocal.Runs.[idx] <- discCopy
        | ArcEntityRef.Run id, EventName.Unlink ->
            if arcLocal.ContainsRun(id) then
                arcLocal.DeleteRun(id)
        | ArcEntityRef.RunDataMap id, (EventName.Add | EventName.Change) ->
            match arcLocal.TryGetRun(id) with
            | None -> ()
            | Some localRun ->
                match arcRemote.TryGetRun(id) with
                | None -> ()
                | Some remoteRun -> localRun.DataMap <- cloneDataMapOption remoteRun.DataMap
        | ArcEntityRef.RunDataMap id, EventName.Unlink ->
            match arcLocal.TryGetRun(id) with
            | None -> ()
            | Some localRun -> localRun.DataMap <- None
        | ArcEntityRef.Workflow id, (EventName.Add | EventName.Change) ->
            match arcRemote.TryGetWorkflow(id) with
            | None -> ()
            | Some remoteWorkflow ->
                match arcLocal.Workflows |> Seq.tryFindIndex (fun workflow -> workflow.Identifier = id) with
                | None -> arcLocal.AddWorkflow(remoteWorkflow.Copy())
                | Some idx ->
                    let discCopy = remoteWorkflow.Copy()

                    if not (Set.contains id dataMapEvents.Workflows) then
                        discCopy.DataMap <- cloneDataMapOption arcLocal.Workflows.[idx].DataMap

                    arcLocal.Workflows.[idx] <- discCopy
        | ArcEntityRef.Workflow id, EventName.Unlink ->
            if arcLocal.ContainsWorkflow(id) then
                arcLocal.DeleteWorkflow(id)
        | ArcEntityRef.WorkflowDataMap id, (EventName.Add | EventName.Change) ->
            match arcLocal.TryGetWorkflow(id) with
            | None -> ()
            | Some localWorkflow ->
                match arcRemote.TryGetWorkflow(id) with
                | None -> ()
                | Some remoteWorkflow -> localWorkflow.DataMap <- cloneDataMapOption remoteWorkflow.DataMap
        | ArcEntityRef.WorkflowDataMap id, EventName.Unlink ->
            match arcLocal.TryGetWorkflow(id) with
            | None -> ()
            | Some localWorkflow -> localWorkflow.DataMap <- None
        | ArcEntityRef.Unknown _, _ -> ()

[<AutoOpen>]
module ArcMergeLibraryExtensions =

    type ARC with
        static member merge (arcLocal: ARC) (arcRemote: ARC) (events: FileEvent list) : ARC =
            if not (arcLocal.hasInMemoryChanges()) then
                arcRemote.Copy()
            elif events.IsEmpty then
                arcLocal.Copy()
            else
                let mergedArc = arcLocal.Copy()
                let parsedEvents = ArcMergeHelper.parseFileEvents events
                let dataMapEvents = ArcMergeHelper.buildDataMapEventIndex parsedEvents

                for event in parsedEvents do
                    ArcMergeHelper.applyEntityEvent
                        mergedArc
                        arcRemote
                        dataMapEvents
                        event.EntityRef
                        event.EventName

                mergedArc
