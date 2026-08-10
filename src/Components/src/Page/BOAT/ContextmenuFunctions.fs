namespace Components

open Feliz
open Types
open Browser
open Browser.Types
open ARCtrl
open Fable.SimpleJson

module FunctionsContextmenu =

    let addAnnotationKeyNew
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            elementID: string,
            highlight: Highlight,
            setHighlight: Highlight -> unit
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
                rect.right - relativeParent.left + 850.0
            | _ -> 0.0

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> { a with IsOpen = false })

            let newAnnoList = [
                Annotation.init (
                    OntologyAnnotation(term),
                    body = CompositeCell.Term(OntologyAnnotation("")),
                    height = yCoordinateOfSelection,
                    xCoordinate = xCoordinateOfSelection
                )
            ]

            setState (List.append closedList newAnnoList)

            let newKeys = highlight.Keys |> Map.add yCoordinateOfSelection term

            setHighlight { highlight with Keys = newKeys }

        else
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges ()


    let addAnnotationBodyNew
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            elementID: string,
            highlight: Highlight,
            setHighlight: Highlight -> unit
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
                rect.left - relativeParent.right
            | _ -> 0.0

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> { a with IsOpen = false })

            let newAnnoList = [
                Annotation.init (
                    OntologyAnnotation(""),
                    body = CompositeCell.Term(OntologyAnnotation(term)),
                    height = yCoordinateOfSelection,
                    xCoordinate = xCoordinateOfSelection
                )
            ]


            setState (List.append closedList newAnnoList)

            let newTerms = highlight.Terms |> Map.add yCoordinateOfSelection term

            setHighlight { highlight with Terms = newTerms }

        else
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges ()

    let addAnnotationValueNew
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            elementID: string,
            highlight: Highlight,
            setHighlight: Highlight -> unit
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
                rect.left - relativeParent.right
            | _ -> 0.0

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> { a with IsOpen = false })

            let newAnnoList = [
                Annotation.init (
                    OntologyAnnotation(""),
                    body = CompositeCell.Unitized(term, OntologyAnnotation("")),
                    height = yCoordinateOfSelection,
                    xCoordinate = xCoordinateOfSelection
                )
            ]


            setState (List.append closedList newAnnoList)

            let newValues = highlight.Values |> Map.add yCoordinateOfSelection term

            setHighlight { highlight with Values = newValues }

        else
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges ()


    let addToLastAnnoAsKey
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        )
        ()
        =
        let term = window.getSelection().ToString().Trim()

        if term.Length <> 0 then

            let updatetedAnno = {
                state.[state.Length - 1] with
                    Search.Key = OntologyAnnotation(name = term)
            }

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newKeys =
                let height = state.[state.Length - 1].Height
                highlight.Keys |> Map.add height term

            setHighlight { highlight with Keys = newKeys }

    let addToLastAnnoAsBody
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        )
        ()
        =
        let term = window.getSelection().ToString().Trim()

        if term.Length <> 0 then
            let updatetedAnno =
                match state.[state.Length - 1].Search.Body with
                | CompositeCell.Unitized(v, oa) -> {
                    state.[state.Length - 1] with
                        Search.Body = CompositeCell.Unitized(v, OntologyAnnotation(term))
                  }
                | _ -> {
                    state.[state.Length - 1] with
                        Search.Body = CompositeCell.Term(OntologyAnnotation(term))
                  }

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newTerms =
                let height = state.[state.Length - 1].Height
                highlight.Terms |> Map.add height term

            setHighlight { highlight with Terms = newTerms }

    let addToLastAnnoAsValue
        (
            state: Annotation list,
            setState: Annotation list -> unit,
            highlight: Highlight,
            setHighlight: Highlight -> unit
        )
        ()
        =
        let term = window.getSelection().ToString().Trim()

        if term.Length <> 0 then
            let updatetedAnno =
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

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newValues =
                let height = state.[state.Length - 1].Height
                highlight.Values |> Map.add height term

            setHighlight { highlight with Values = newValues }
