namespace Components

open Feliz
open Types
open Browser
open Browser.Types
open ARCtrl
open Fable.SimpleJson

module FunctionsContextmenu =

    let addAnnotationNew
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            elementID: string,
            newType: string
        )
        ()
        =
        let term = window.getSelection().ToString().Trim()

        let yCoordinateOfSelection =
            match window.getSelection () with
            | (selection: Selection) when selection.rangeCount > 0 ->
                let range = selection.getRangeAt (0)
                let rect = range.getBoundingClientRect ()
                let relativeParent = document.getElementById(elementID).getBoundingClientRect ()
                rect.bottom - relativeParent.top + 12.0
            | _ -> 0.0

        let xCoordinateOfSelection =
            match window.getSelection () with
            | (selection: Selection) when selection.rangeCount > 0 ->
                let range = selection.getRangeAt (0)
                let rect = range.getBoundingClientRect ()
                let relativeParent = document.getElementById(elementID).getBoundingClientRect ()
                rect.right - relativeParent.left + 20.0
            | _ -> 0.0

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> { a with IsOpen = false })

            let newAnnoList = 
                match newType with
                |"key" -> 
                    [
                    Annotation.init (
                        key = OntologyAnnotation(term),
                        body = CompositeCell.Term(OntologyAnnotation("")),
                        height = yCoordinateOfSelection,
                        xCoordinate = xCoordinateOfSelection
                    )
                    ]
                |"term" -> 
                    [
                    Annotation.init (
                        key = OntologyAnnotation(""),
                        body = CompositeCell.Term(OntologyAnnotation(term)),

                        height = yCoordinateOfSelection,
                        xCoordinate = xCoordinateOfSelection
                    )
                    ]
                |"value" -> 
                    [
                    Annotation.init (
                        key = OntologyAnnotation(""),
                        body = CompositeCell.Unitized(term, OntologyAnnotation("")),
                        height = yCoordinateOfSelection,
                        xCoordinate = xCoordinateOfSelection
                    )
                    ]
                |_ -> []                  

            setState (List.append closedList newAnnoList)

        else
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges ()

    let addToLastAnno
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            newType: string
        )
        ()
        =
        let term = window.getSelection().ToString().Trim()

        if term.Length <> 0 then

            let updatetedAnno = 
                match newType with
                |"key" -> {
                    state.[state.Length - 1] with
                        Search.Key = OntologyAnnotation(name = term)
                    }   
                |"term" -> 
                    match state.[state.Length - 1].Search.Body with
                    | CompositeCell.Unitized(v, oa) -> {
                        state.[state.Length - 1] with
                            Search.Body = CompositeCell.Unitized(v, OntologyAnnotation(term))
                        }
                    | _ -> {
                        state.[state.Length - 1] with
                            Search.Body = CompositeCell.Term(OntologyAnnotation(term))
                    }
                |"value" -> 
                    match state.[state.Length - 1].Search.Body with
                    | CompositeCell.Term oa -> {
                        state.[state.Length - 1] with
                            Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(oa.NameText))
                        }
                    | CompositeCell.Unitized(v, oa) -> {
                        state.[state.Length - 1] with
                            Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(oa.NameText))
                        }
                    | _ -> {
                        state.[state.Length - 1] with
                            Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(""))
                        }
                |_ -> state.[state.Length - 1]

            
            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

