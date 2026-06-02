module ElectronCore.ArcMerge.MockData

open ARCtrl

let createBaseTable (name: string) =
    let table = ArcTable(name)
    table.AddColumn(CompositeHeader.Input IOType.Sample)
    table.AddColumn(CompositeHeader.Output IOType.Sample)
    table.AddColumn(CompositeHeader.ProtocolREF)
    table.AddRowsEmpty 5
    table

let createArc () =
    let arc = ARC("Arc 1")
    let assay = arc.InitAssay("My Assay")
    assay.AddTable(createBaseTable "Sampling")
    assay.AddTable(createBaseTable "Testing")
    arc

let createCleanBaseArcPlusCopy () =
    let arc = createArc ()
    let arcCopy = arc.Copy()
    arc.GetUpdateContracts() |> ignore
    arc, arcCopy

let createCleanArc () =
    let arc = createArc ()
    arc.GetUpdateContracts() |> ignore
    arc

let createTwoCleanCopies () =
    let baseline = createCleanArc ()
    let localArc = baseline.Copy()
    localArc.GetUpdateContracts() |> ignore
    let remoteArc = baseline.Copy()
    remoteArc.GetUpdateContracts() |> ignore
    localArc, remoteArc
