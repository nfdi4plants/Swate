module TemplateMetadata

//open Newtonsoft.Json.Schema

//let resolver = JSchemaUrlResolver()
    
//let jsonSchemaPath = @"public/TemplateMetadataSchema.json"

//let writeSettings =
//    let s = JSchemaWriterSettings()
//    s.ReferenceHandling <- JSchemaWriterReferenceHandling.Never
//    s

//let getJsonSchemaAsXml =
//    System.IO.File.ReadAllText jsonSchemaPath
//    |> fun x -> JSchema.Parse(x,resolver).ToString(writeSettings)

open System
open System.IO
open FSharpSpreadsheetML

open Shared
open ProtocolTemplateTypes
open DynamicObj
open Newtonsoft.Json

type TemplateMetadataJsonExport() = 
    inherit DynamicObj()

    static member init(?props) =
        let t = TemplateMetadataJsonExport()
        if props.IsSome then
            props.Value |> List.iter t.setProp
            t
        else
            t.setProp("", None)
            t
            
    member this.setProp(key,value) = DynObj.setValueOpt this key value

    member this.print() = DynObj.print this

    member this.toJson() = this |> JsonConvert.SerializeObject

let private maxColumnByRows (rows:DocumentFormat.OpenXml.Spreadsheet.Row []) =
    rows
    |> Array.map (fun row -> (Row.Spans.toBoundaries >> snd) row.Spans)
    |> (Array.max >> int)

let private findRowByKey (key:string) (rowValues: string option [][])=
    rowValues
    |> Array.find (fun row ->
        row.[0].IsSome && row.[0].Value = key
    )

let private findRowValuesByKey (key:string) (rowValues: string option [][])=
    findRowByKey key rowValues
    |> Array.tail

/// Also gets rows from children
let private getAllRelatedRowsValues (metadata:TemplateMetadata.MetadataField) (rows: string option [][]) =
    let rec collectRows (crMetadata:TemplateMetadata.MetadataField) =
        if crMetadata.Children.IsEmpty then
            let row = rows |> findRowValuesByKey crMetadata.ExtendedNameKey
            [|row|]
        else
            let childRows = crMetadata.Children |> Array.ofList |> Array.collect (fun child -> collectRows child)
            childRows
    collectRows metadata

let private convertToDynObject (sheetData:DocumentFormat.OpenXml.Spreadsheet.SheetData) sst (metadata:TemplateMetadata.MetadataField) =
    let rows = SheetData.getRows sheetData |> Array.ofSeq
    let rowValues =
        rows
        |> Array.map (fun row ->
            let spans = row.Spans
            let leftB,rightB = Row.Spans.toBoundaries spans
            [|
                for i = leftB to rightB do
                    yield 
                        Row.tryGetValueAt sst i row
            |]
        )
    let rec convertDynamic (listIndex: int option) (forwardOutput:TemplateMetadataJsonExport option) (metadata:TemplateMetadata.MetadataField) =
        let output =
            if forwardOutput.IsSome then
                forwardOutput.Value
            else
                TemplateMetadataJsonExport.init()
        match metadata with
        /// hit leaves without children
        | isOutput when metadata.Children = [] ->
            let isList = listIndex.IsSome
            let rowValues = rowValues |> findRowValuesByKey metadata.ExtendedNameKey |> Array.choose id
            if isList then
                let v =
                    let tryFromArr = Array.tryItem (listIndex.Value) rowValues
                    Option.defaultValue "" tryFromArr
                output.setProp(metadata.Key, Some v)
            else
                let v = if Array.isEmpty >> not <| rowValues then rowValues.[0] else ""
                output.setProp(metadata.Key, Some v)
            output
        /// Treat nested lists as object, as nested lists cannot be represented in excel
        | isNestedObjectList when metadata.Children <> [] && metadata.List && metadata.Key <> "" && listIndex.IsSome ->
            /// "WARNING: Cannot parse nested list: metadata.Key, metadata.ExtendedNameKey. Treat by default as object."
            let noList = { metadata with List = false }
            convertDynamic listIndex forwardOutput noList
        /// children are represented by columns
        | isObjectList when metadata.Children <> [] && metadata.List && metadata.Key <> "" ->
            let childRows = getAllRelatedRowsValues metadata rowValues
            /// Only calculate max columns if cell still contains a value. Filter out None and ""
            let notEmptyChildRows = childRows |> Array.map (Array.choose id) |> Array.map (Array.filter (fun x -> x <> ""))
            let maxColumn = notEmptyChildRows |> Array.map Array.length |> Array.max
            let childObjects =
                [| for i = 0 to maxColumn-1 do
                    let childOutput = TemplateMetadataJsonExport.init()
                    let addChildObject = metadata.Children |> List.map (convertDynamic (Some i) (Some childOutput))
                    yield
                        childOutput
                |]
            output.setProp(metadata.Key, Some childObjects)
            output
        /// hit if json object
        | isObject when metadata.Children <> [] && metadata.Key <> "" ->
            let childOutput = TemplateMetadataJsonExport.init()
            // Add key values from children to childOutput. childOutput will be added to output afterwards.
            let childObject = metadata.Children |> List.map (convertDynamic listIndex (Some childOutput)) 
            output.setProp(metadata.Key, Some childOutput)
            output
        /// This hits only root objects without key
        | isRoot ->
            let addChildObject =
                metadata.Children
                |> List.map (fun childMetadata ->
                    convertDynamic listIndex (Some output) childMetadata
                )
            output
    convertDynamic None None metadata

let parseDynMetadataFromByteArr (byteArray:byte []) =
    let ms = new MemoryStream(byteArray)
    let spreadsheet = Spreadsheet.fromStream ms false
    let sst = Spreadsheet.tryGetSharedStringTable spreadsheet
    let sheetOpt = Spreadsheet.tryGetSheetBySheetName ProtocolTemplateTypes.TemplateMetadata.TemplateMetadataWorksheetName spreadsheet
    if sheetOpt.IsNone then failwith $"Could not find template metadata worksheet: {ProtocolTemplateTypes.TemplateMetadata.TemplateMetadataWorksheetName}"

    convertToDynObject sheetOpt.Value sst TemplateMetadata.root
