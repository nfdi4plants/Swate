namespace MainComponents.DataMap

open ARCtrl
open Feliz
open Feliz.Bulma
open Model
open SpreadsheetInterface
open Messages
open Shared

module private Helper =

    let updateFilePath (dtx: DataContext) (index: int) (dispatch: Messages.Msg -> unit) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.FilePath <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateSelector (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.Selector <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateSelectorFormat (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.SelectorFormat <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let dataFileTrytoString (dtf: DataFile option) =
        dtf |> Option.map _.ToStringRdb() |> Option.defaultValue "None"

    let updateDataFile (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = DataFile.tryFromString newVal
        dtx.DataType <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateFormat (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.Format <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateDescription (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.Description <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateGeneratedBy (dtx: DataContext) (index: int) (dispatch) (newVal: string) =
        let newVal = if newVal = "" then None else Some newVal
        dtx.GeneratedBy <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateExplication (dtx: DataContext) (index: int) (dispatch) (newVal: OntologyAnnotation option) =
        dtx.Explication <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateUnit (dtx: DataContext) (index: int) (dispatch) (newVal: OntologyAnnotation option) =
        dtx.Unit <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

    let updateObjectType (dtx: DataContext) (index: int) (dispatch) (newVal: OntologyAnnotation option) =
        dtx.ObjectType <- newVal
        UpdateDataMapDataContextAt(index, dtx) |> InterfaceMsg |> dispatch

module private Components =
    /// https://github.com/nfdi4plants/ARC-specification/blob/main/ISA-XLSX.md#examples-2
    let HeaderRow (state:Set<int>) setState (model:Model) (dispatch: Msg -> unit) =
        Html.tr [
            prop.children [
                MainComponents.CellStyles.RowLabel -1
                yield!
                    [
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Data Name")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Data FilePath")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Data Selector")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Data Selector Format")
                        //fun i -> Cells.Header (i, Spreadsheet.Main, "Data File Type")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Data Format")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Description")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "GeneratedBy")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Explication")
                        fun i -> Cells.Header (i, Spreadsheet.TSR, "Term Source REF")
                        fun i -> Cells.Header (i, Spreadsheet.TAN, "Term Accession Number")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Unit")
                        fun i -> Cells.Header (i, Spreadsheet.TSR, "Term Source REF")
                        fun i -> Cells.Header (i, Spreadsheet.TAN, "Term Accession Number")
                        fun i -> Cells.Header (i, Spreadsheet.Main, "Object Type")
                        fun i -> Cells.Header (i, Spreadsheet.TSR, "Term Source REF")
                        fun i -> Cells.Header (i, Spreadsheet.TAN, "Term Accession Number")
                    ]
                    |> List.mapi (fun i f -> f i)
            ]
        ]

    /// <summary>
    /// let columnIndex, rowIndex = index
    /// </summary>
    /// <param name="value"></param>
    /// <param name="setter"></param>
    /// <param name="index">let columnIndex, rowIndex = index</param>
    /// <param name="model"></param>
    /// <param name="dispatch"></param>
    let Body (value: string option, setter, index: int * int, model: Model.Model, dispatch: Messages.Msg -> unit, readonly: bool option) =
        let value = value |> Option.defaultValue ""
        Spreadsheet.Cells.Cell.BodyBase(Spreadsheet.ColumnType.Main, value, setter, index,model, dispatch, ?readonly=readonly)

    let BodyOntologyAnnotation (value: OntologyAnnotation option, setter: OntologyAnnotation option -> unit, index: int * int, model: Model.Model, dispatch: Messages.Msg -> unit) =
        let value = defaultArg value (OntologyAnnotation())
        let setter = fun (oa:OntologyAnnotation) ->
            if oa.isEmpty() then None else Some oa
            |> setter
        let oaSetter = {|
            oa = value;
            setter = fun (oa: OntologyAnnotation) -> oa |> setter
        |}
        let vMain = value.Name |> Option.defaultValue ""
        let setterMain = fun (s:string) ->
            value.Name <- if s = "" then None else Some s
            setter value
        // The same helper functions for TSR
        let vTSR = value.TermSourceREF |> Option.defaultValue ""
        let setterTSR = fun (s:string) ->
            value.TermSourceREF <- if s = "" then None else Some s
            setter value
        // the same helper for tan
        let vTAN = value.TermAccessionNumber |> Option.defaultValue ""
        let setterTAN = fun (s:string) ->
            value.TermAccessionNumber <- if s = "" then None else Some s
            setter value
        [
            Spreadsheet.Cells.Cell.BodyBase(Spreadsheet.ColumnType.Main, vMain, setterMain, index, model, dispatch, oasetter=oaSetter)
            Spreadsheet.Cells.Cell.BodyBase(Spreadsheet.ColumnType.TSR, vTSR, setterTSR, index, model, dispatch)
            Spreadsheet.Cells.Cell.BodyBase(Spreadsheet.ColumnType.TAN, vTAN, setterTAN, index, model, dispatch)
        ]

    let BodyRow (dtx: DataContext) (rowIndex: int) (state:Set<int>) (model:Model) (dispatch: Messages.Msg -> unit) =
        let mkIndex (col: int) = (col,rowIndex)
        let DataMapBaseBody (field, updateFunc) i = Body(field, updateFunc dtx rowIndex dispatch, mkIndex i, model, dispatch, None)
        let DataMapBaseBodyOA (field, updateFunc) i = BodyOntologyAnnotation(field, updateFunc dtx rowIndex dispatch, mkIndex i, model, dispatch)
        Html.tr [
            MainComponents.CellStyles.RowLabel rowIndex
            -1 |> fun i -> Body(dtx.Name, (fun _ -> ()), mkIndex i, model, dispatch, Some true)
            0 |> DataMapBaseBody(dtx.FilePath, Helper.updateFilePath)
            1 |> DataMapBaseBody(dtx.Selector, Helper.updateSelector)
            2 |> DataMapBaseBody(dtx.SelectorFormat, Helper.updateSelectorFormat)
            //3 |> fun i -> Spreadsheet.Cells.Cell.BodySelect(Helper.dataFileTrytoString dtx.DataType, (Helper.updateDataFile dtx rowIndex dispatch), ["None"; DataFile.DerivedDataFile.ToStringRdb(); DataFile.ImageFile.ToStringRdb(); DataFile.RawDataFile.ToStringRdb()], mkIndex i, model, dispatch)
            3 |> DataMapBaseBody(dtx.Format, Helper.updateFormat)
            4 |> DataMapBaseBody(dtx.Description, Helper.updateDescription)
            5 |> DataMapBaseBody(dtx.GeneratedBy, Helper.updateGeneratedBy)
            yield! 6 |> DataMapBaseBodyOA(dtx.Explication, Helper.updateExplication)
            yield! 7 |> DataMapBaseBodyOA(dtx.Unit, Helper.updateUnit)
            yield! 8 |> DataMapBaseBodyOA(dtx.ObjectType, Helper.updateObjectType)
        ]

    let BodyRows (dtm: DataMap) (state:Set<int>) (model:Model) (dispatch: Msg -> unit) =
        Html.tbody [
            for ri in 0 .. (dtm.DataContexts.Count-1) do
                yield BodyRow dtm.DataContexts.[ri] ri state model dispatch
        ]

type DataMap =

    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        let ref = React.useElementRef()
        let state, setState : Set<int> * (Set<int> -> unit) = React.useState(Set.empty)
        let dtm = model.SpreadsheetModel.DataMapOrDefault
        Html.div [
            prop.id "SPREADSHEET_MAIN_VIEW"
            prop.tabIndex 0
            prop.style [style.border(1, borderStyle.solid, "grey"); style.width.minContent; style.marginRight(length.vw 10)]
            prop.ref ref
            prop.onKeyDown(fun e -> Spreadsheet.KeyboardShortcuts.onKeydownEvent dispatch e)
            prop.children [
                Html.table [
                    prop.className "fixed_headers"
                    prop.children [
                        Html.thead [
                            Components.HeaderRow state setState model dispatch
                        ]
                        Components.BodyRows dtm state model dispatch
                    ]
                ]
            ]
        ]