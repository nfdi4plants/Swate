module OfficeInterop.TermCollectionFunctions

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared.OfficeInteropTypes
open Shared.TermTypes


/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let getSwateTermCollectionNameCol (context:RequestContext) =

    let workbook = context.workbook.load(propertyNames=U2.Case1 "worksheets")

    let termCollectionWorksheet = workbook.worksheets.getItem("SwateTermCollection")

    let termCollectionTermNameCol = termCollectionWorksheet.tables.getItem("SwateTermCollectionTable").getDataBodyRange().getColumn(0.)

    context.sync().``then`` (fun _ ->

        termCollectionTermNameCol
    )


/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let getSwateTermCollectionValues (context:RequestContext) =

    let workbook = context.workbook.load(propertyNames=U2.Case1 "worksheets")

    let termCollectionWorksheet = workbook.worksheets.getItem("SwateTermCollection")

    let termCollectionTableBodyRange = termCollectionWorksheet.tables.getItem("SwateTermCollectionTable").getDataBodyRange()
    let _ = termCollectionTableBodyRange.load(U2.Case1 "values")


    context.sync().``then`` (fun _ ->

        termCollectionTableBodyRange.values
        |> Seq.map (fun rowVals ->
            TermMinimal.create $"{rowVals.[0]}" $"{rowVals.[1]}"
        )
        |> Array.ofSeq
        |> Array.filter (fun x -> x.Name <> "" && x.TermAccession <> "")
    )


let setSearchQueryTerm (context:RequestContext) (queryTerm:string) = 

    let searchQueryCell = context.workbook.worksheets.getItem("SwateTermCollection").getCell(1.,2.).load(U2.Case1 "formulas")

    context.sync().``then`` (fun _ ->
        let newValue = Some (box queryTerm)
        let newValueRange = ResizeArray [|ResizeArray [|newValue|]|]
        searchQueryCell.formulas <- newValueRange
    )



type WorksheetOnSelectChangeHandlers = {
    WorksheetId  : string
    EventHandler : OfficeExtension.EventHandlerResult<SelectionChangedEventArgs> 
} with
    static member create worksheetId eventHandler = {
        WorksheetId  = worksheetId
        EventHandler = eventHandler
    }

type EventHandlers = {
    WorksheetOnSelectChangeHandlers : WorksheetOnSelectChangeHandlers list
} with
    static member init() = {
        WorksheetOnSelectChangeHandlers = List.empty
    }

    member this.addWorksheetOnSelectChangeHandlers id event =
        let newEventInfo = WorksheetOnSelectChangeHandlers.create id event
        let prevEvent = this.WorksheetOnSelectChangeHandlers
        {this with
            WorksheetOnSelectChangeHandlers = newEventInfo::prevEvent}

    member this.removeWorksheetOnSelectChangeHandlers id =
        let events = this.WorksheetOnSelectChangeHandlers |> List.filter (fun x -> x.WorksheetId = id)
        let rmvEvents =  events |> List.map (fun x -> x.EventHandler.remove())
        let nextEvents =  this.WorksheetOnSelectChangeHandlers |> List.filter (fun x -> x.WorksheetId <> id)
        {this with
            WorksheetOnSelectChangeHandlers = nextEvents}
        

let mutable eventHandlerModel = EventHandlers.init()

open System

/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let addUpdateSelectedCellToQueryParamHandler (context:RequestContext) =

    let workbook = context.workbook

    let id = "TestChangeQueryEvent"

    context.sync().``then``(fun _ ->

        eventHandlerModel <- eventHandlerModel.removeWorksheetOnSelectChangeHandlers id

        let handler = workbook.onSelectionChanged.add (fun t ->
            Excel.run(fun eventContext ->

                let selectedRange = eventContext.workbook.getSelectedRange().load(U2.Case1 "address")
                promise {
                    let! selectedAddress = eventContext.sync().``then``(fun _ ->
                        let address = selectedRange.address
                        let newValue = $"""=IF({address}<>0,{address},"")"""
                        newValue
                    )

                    let! setQuery = setSearchQueryTerm eventContext selectedAddress

                    return None
                }
            )
        )

        eventHandlerModel <- eventHandlerModel.addWorksheetOnSelectChangeHandlers id handler

        $"Existing EventHandlers: {eventHandlerModel.WorksheetOnSelectChangeHandlers |> List.map (fun x -> x.WorksheetId)}"
    ) 
          
//promise {

//let! termNamerange = OfficeInterop.TermCollectionFunctions.getSwateTermCollectionNameCol context

//let! addValidation = context.sync().``then``(fun _ ->

//    // https://fable.io/docs/communicate/js-from-fable.html
//    let t1 = createEmpty<ListDataValidation>
//    t1.inCellDropDown <- true
//    t1.source <- U2.Case1 "=SwateTermCollection!$D$2#"

//    let t2 = createEmpty<DataValidationRule>
//    t2.list <- Some t1

//    //https://stackoverflow.com/questions/37881457/how-to-implement-data-validation-in-excel-using-office-js-api
//    //https://docs.microsoft.com/de-de/javascript/api/excel/excel.datavalidation?view=excel-js-preview#rule
//    selectedRangeValidation.rule <- t2

//    $"{selectedRangeValidation.rule.list.Value.inCellDropDown},{selectedRangeValidation.rule.list.Value.source}"
//)

//let! mySync = context.sync().``then``(fun _ -> ())

//return addValidation
        //}