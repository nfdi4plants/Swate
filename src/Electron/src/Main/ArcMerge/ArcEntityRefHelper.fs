namespace Main.ArcMerge

module ArcEntityRefHelper =

    let investigationFile = ARCtrl.ArcPathHelper.InvestigationFileName
    let assayFile = ARCtrl.ArcPathHelper.AssayFileName
    let studyFile = ARCtrl.ArcPathHelper.StudyFileName
    let runFile = ARCtrl.ArcPathHelper.RunFileName
    let workflowFile = ARCtrl.ArcPathHelper.WorkflowFileName
    let dataMapFile = ARCtrl.ArcPathHelper.DataMapFileName

    let assaysFolder = ARCtrl.ArcPathHelper.AssaysFolderName
    let studiesFolder = ARCtrl.ArcPathHelper.StudiesFolderName
    let runsFolder = ARCtrl.ArcPathHelper.RunsFolderName
    let workflowsFolder = ARCtrl.ArcPathHelper.WorkflowsFolderName

open ArcEntityRefHelper

[<AutoOpen>]
module ArcEntityRefExtensions =

    type ArcEntityRef with
        static member fromPath(path: string) : ArcEntityRef =
            let segments =
                path.Split([| '/'; '\\' |])
                |> Array.filter (fun segment -> segment <> "" && segment <> ".")

            if segments.Length = 0 then
                ArcEntityRef.Unknown path
            elif segments.[segments.Length - 1] = investigationFile then
                ArcEntityRef.Investigation
            elif segments.Length >= 3 then
                let folder = segments.[segments.Length - 3]
                let identifier = segments.[segments.Length - 2]
                let fileName = segments.[segments.Length - 1]

                match folder, fileName with
                | f, n when f = assaysFolder && n = assayFile -> ArcEntityRef.Assay identifier
                | f, n when f = assaysFolder && n = dataMapFile -> ArcEntityRef.AssayDataMap identifier
                | f, n when f = studiesFolder && n = studyFile -> ArcEntityRef.Study identifier
                | f, n when f = studiesFolder && n = dataMapFile -> ArcEntityRef.StudyDataMap identifier
                | f, n when f = runsFolder && n = runFile -> ArcEntityRef.Run identifier
                | f, n when f = runsFolder && n = dataMapFile -> ArcEntityRef.RunDataMap identifier
                | f, n when f = workflowsFolder && n = workflowFile -> ArcEntityRef.Workflow identifier
                | f, n when f = workflowsFolder && n = dataMapFile -> ArcEntityRef.WorkflowDataMap identifier
                | _ -> ArcEntityRef.Unknown path
            else
                ArcEntityRef.Unknown path
