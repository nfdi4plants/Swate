module ElectronCore.ArcFileMutationHelperTests

open ARCtrl
open Main.ArcVault.Helper
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open ARCtrl.Contract
open Vitest

let private expectOk (result: Result<'T, exn>) : 'T =
    match result with
    | Ok value -> value
    | Error err -> failwith err.Message

let private expectSome (value: 'T option) (message: string) : 'T =
    match value with
    | Some value -> value
    | None -> failwith message

Vitest.describe("ArcFileMutationHelper", fun () ->
    Vitest.test("normalizeArcFileRequestPath normalizes separators and trims trailing slash", fun () ->
        let request =
            FileContentDTO.create DTOType.ISA_Assay """{"dummy":"value"}""" @"assays\assay_1\isa.assay.xlsx\"

        let normalized = normalizeArcFileRequestPath request
        Vitest.expect(normalized.path).toBe("assays/assay_1/isa.assay.xlsx"))

    Vitest.test("updateARCByFileContentDTO updates an existing ISA assay while preserving static hash", fun () ->
        let oldArc = ARC("test-arc")
        let oldAssay = ArcAssay("assay_1", title = "old-title")
        oldAssay.StaticHash <- 1337
        oldArc.AddAssay(oldAssay)

        let updatedAssay = ArcAssay("assay_1", title = "new-title")

        let request =
            FileContentDTO.fromArcFile(ArcFiles.Assay updatedAssay)
            |> expectSome <| "Expected assay request DTO."

        let updatedArc = updateARCByFileContentDTO oldArc request |> expectOk
        let resultingAssay = updatedArc.GetAssay("assay_1")

        Vitest.expect(resultingAssay.Title).toEqual(Some "new-title")
        Vitest.expect(resultingAssay.StaticHash).toBe(1337))

    Vitest.test("updateARCByFileContentDTO updates datamap by parent info and preserves static hash", fun () ->
        let oldArc = ARC("test-arc")

        let existingDataMap = DataMap.init()
        existingDataMap.StaticHash <- 777

        let assay = ArcAssay("assay_1", datamap = existingDataMap)
        oldArc.AddAssay(assay)

        let incomingDataMap = DataMap.init()
        incomingDataMap.StaticHash <- 5

        let datamapParent = DatamapParentInfo.create "assay_1" DataMapParent.Assay

        let request =
            FileContentDTO.fromArcFile(ArcFiles.DataMap(Some datamapParent, incomingDataMap))
            |> expectSome <| "Expected datamap request DTO."

        let updatedArc = updateARCByFileContentDTO oldArc request |> expectOk

        let updatedDataMap =
            updatedArc.TryGetAssay("assay_1")
            |> expectSome <| "Expected assay to exist after datamap update."
            |> fun updatedAssay -> updatedAssay.DataMap
            |> expectSome <| "Expected datamap to be present on assay."

        Vitest.expect(updatedDataMap.StaticHash).toBe(777))

    Vitest.test("updateARCByFileContentDTO accepts semantically identical ISA content", fun () ->
        let oldArc = ARC("test-arc")
        oldArc.AddAssay(ArcAssay("assay_1", title = "stable"))

        let request =
            FileContentDTO.fromArcByPath "assays/assay_1/isa.assay.xlsx" oldArc
            |> expectSome <| "Expected current assay DTO from arc."

        let updatedArc = updateARCByFileContentDTO oldArc request |> expectOk
        let updatedAssay = updatedArc.GetAssay("assay_1")
        Vitest.expect(updatedAssay.Title).toEqual(Some "stable"))
)
