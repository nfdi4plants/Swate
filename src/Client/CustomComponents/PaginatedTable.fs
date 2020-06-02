module PaginatedTable

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open ExcelColors
open Model
open Messages

//TO-DO: generic pageination table for dsiplay of e.g. large amounts of advanced search copmponents

type paginationParameters = {
    ChunkSize   : int
    OnNext      : Msg
    OnPrevious  : Msg

}

let createPaginationLinkFromIndex (dispatch:Msg->unit) (pageIndex:int) (currentPageinationIndex: int)=

    Pagination.Link.a [
        Pagination.Link.Current (pageIndex = currentPageinationIndex)
        Pagination.Link.Props [OnClick (fun _ -> pageIndex |> ChangePageinationIndex |> AdvancedSearch |> dispatch)]
    ] [
        span [] [str (string (pageIndex+1))]
    ]

let pageinateDynamic (dispatch:Msg->unit) (currentPageinationIndex: int) (pageCount:int)  = 
    (*[0 .. pageCount-1].*)
    [(max 1 (currentPageinationIndex-2)) .. (min (currentPageinationIndex+2) (pageCount-2)) ]
    |> List.map (
        fun index -> createPaginationLinkFromIndex dispatch index currentPageinationIndex
    ) 


let paginatedTableComponent (model:Model) (dispatch: Msg -> unit) (elements:ReactElement []) =

    if elements.Length > 0 then 

        let currentPageinationIndex = model.AdvancedSearchState.AdvancedSearchResultPageinationIndex
        let chunked = elements |> Array.chunkBySize 5
        let len = chunked.Length 
    
        Container.container [] [
            Pagination.pagination [Pagination.IsCentered] [
                Pagination.previous [Props [OnClick (fun _ -> (max (currentPageinationIndex - 1) 0) |> ChangePageinationIndex |> AdvancedSearch |> dispatch )]] [str "Prev"]
                Pagination.list [] [
                    yield createPaginationLinkFromIndex dispatch 0 currentPageinationIndex
                    if len > 5 then yield Pagination.ellipsis []
                    yield! pageinateDynamic dispatch currentPageinationIndex (len - 1)
                    if len > 5 then yield Pagination.ellipsis []
                    yield createPaginationLinkFromIndex dispatch (len-1) currentPageinationIndex
                ]
                Pagination.next [Props [OnClick (fun _ -> (min (currentPageinationIndex + 1) (len - 1)) |> ChangePageinationIndex |> AdvancedSearch |> dispatch )]] [str "Next"]
            ]
            Table.table [Table.IsFullWidth] [
                thead [] []
                tbody [] (
                    chunked.[currentPageinationIndex] |> Array.toList
                )
            ]
        ]
    else div [] []