namespace ARCtrl.Contract

open Fable.Core
open Fable.Core.JsInterop

[<StringEnum>]
[<RequireQualifiedAccess>]
type DTOType = 
    | [<CompiledName("ISA_Assay")>] ISA_Assay // isa.assay.xlsx
    | [<CompiledName("ISA_Study")>] ISA_Study
    | [<CompiledName("ISA_Investigation")>] ISA_Investigation 
    | [<CompiledName("JSON")>] JSON // arc.json
    | [<CompiledName("Markdown")>] Markdown // README.md
    | [<CompiledName("CWL")>] CWL // workflow.cwl, might be a new DTO once we 
    | [<CompiledName("PlainText")>] PlainText // any other
    | [<CompiledName("Cli")>] CLI
    //| [<CompiledName("EmptyFile")>] EmptyFile // .gitkeep

[<AttachMembers>]
type CLITool = 
    {
        Name: string
        Arguments: string array
    }

    static member create(name,arguments) = {Name = name; Arguments = arguments}

[<Erase>]
[<RequireQualifiedAccess>]
type DTO =
    | Spreadsheet of obj
    | Text of string
    | CLITool of CLITool

    member this.isSpreadsheet =
        match this with | Spreadsheet _ -> true | _ -> false

    member this.isText =
        match this with | Text _ -> true | _ -> false

    member this.isCLITool =
        match this with | CLITool _ -> true | _ -> false

    member this.AsSpreadsheet() =
        match this with | Spreadsheet s -> s | _ -> failwith "Not a spreadsheet"

    member this.AsText() =
        match this with | Text t -> t | _ -> failwith "Not text"

    member this.AsCLITool() =
        match this with | CLITool c -> c | _ -> failwith "Not a CLI tool"

[<StringEnum>]
type Operation =
    | [<CompiledName("CREATE")>] CREATE
    | [<CompiledName("UPDATE")>] UPDATE
    | [<CompiledName("DELETE")>] DELETE
    | [<CompiledName("READ")>] READ
    | [<CompiledName("EXECUTE")>] EXECUTE

[<AttachMembers>]
type Contract = 
    {
        /// Determines what io operation should be done: CREATE, READ, DELETE, ...
        Operation : Operation
        /// The path where the io operation should be executed. The path is relative to ARC root.
        Path: string
        /// The type of DTO that is expected: json, plaintext, isa_study, isa_assay, isa_investigation
        DTOType : DTOType option
        /// The actual DTO, as discriminate union.
        DTO: DTO option
    }
    [<NamedParams(fromIndex=2)>]
    static member create(op, path, ?dtoType, ?dto) = {Operation= op; Path = path; DTOType = dtoType; DTO = dto}
    /// <summary>Create a CREATE contract with all necessary information.</summary>
    /// <param name="path">The path relative from ARC root, at which the new file should be created.</param>
    /// <param name="dtoType">The file type.</param>
    /// <param name="dto">The file data.</param>
    /// <returns>Returns a CREATE contract.</returns>
    static member createCreate(path, dtoType: DTOType, ?dto: DTO) = {Operation= Operation.CREATE; Path = path; DTOType = Some dtoType; DTO = dto}
    /// <summary>Create a UPDATE contract with all necessary information.
    /// 
    /// Update contracts will overwrite in case of a string as DTO and will specifically update relevant changes only for spreadsheet files.
    /// </summary>
    /// <param name="path">The path relative from ARC root, at which the file should be updated.</param>
    /// <param name="dtoType">The file type.</param>
    /// <param name="dto">The file data.</param>
    /// <returns>Returns a UPDATE contract.</returns>
    static member createUpdate(path, dtoType: DTOType, dto: DTO) = {Operation= Operation.UPDATE; Path = path; DTOType = Some dtoType; DTO = Some dto}
    /// <summary>Create a DELETE contract with all necessary information. </summary>
    /// <param name="path">The path relative from ARC root, at which the file should be deleted.</param>
    /// <returns>Returns a DELETE contract.</returns>
    static member createDelete(path) = {Operation= Operation.DELETE; Path = path; DTOType = None; DTO = None}
    /// <summary>Create a READ contract with all necessary information.
    /// 
    /// Created without DTO, any api user should update the READ contract with the io read result for further api use.
    /// Please check documentation for the exact workflow.
    /// </summary>
    /// <param name="path">The path relative from ARC root, at which the file to read is located.</param>
    /// <param name="dtoType">The file type.</param>
    /// <returns>Returns a READ contract.</returns>
    static member createRead(path, dtoType: DTOType) = {Operation= Operation.READ; Path = path; DTOType = Some dtoType; DTO = None}
    /// <summary>Create a EXECUTE contract with all necessary information.
    /// 
    /// This contract type is used to communicate cli tool execution.
    /// </summary>
    /// <param name="dto">The cli tool information.</param>
    /// <param name="path">The path relative from ARC root, at which the cli tool should be executed. **Default:** ARC root</param>
    /// <returns>Returns a EXECUTE contract.</returns>
    static member createExecute(dto: CLITool, ?path: string) = 
        let path = Option.defaultValue "" path
        {Operation= Operation.EXECUTE; Path = path; DTOType = Some DTOType.CLI; DTO = Some <| DTO.CLITool dto}
