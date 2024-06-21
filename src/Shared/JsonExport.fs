module Shared.JsonExport

type JsonExportFormat =
    | ARCtrl
    | ARCtrlCompressed
    | ISA
    | ROCrate

    static member fromString (str: string) =
        match str.ToLower() with
        | "arctrl" -> ARCtrl
        | "arctrlcompressed" -> ARCtrlCompressed
        | "isa" -> ISA
        | "rocrate" -> ROCrate
        | _ -> failwithf "Unknown JSON export format: %s" str
