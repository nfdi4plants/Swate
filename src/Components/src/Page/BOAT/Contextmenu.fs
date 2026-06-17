namespace Components

open Feliz
open Types
open Browser
open Browser.Types
open ARCtrl


module private Helper =

    let preventDefault =
        prop.onContextMenu (fun e ->
            e.stopPropagation()
            e.preventDefault()
        ) 

    let button (name:string, resetter: unit -> unit, state, func: unit -> unit, props) =
        Html.li [
            Html.div [
                prop.className "hover:bg-[#a7d9ec] justify-between text-sm text-black select-none p-2"
                prop.onMouseDown (fun e -> 
                    func()
                    resetter()
                    
                    ()
                )   
                prop.onBlur (fun _ -> resetter() )
                yield! props
                prop.text name
            ]
        ]
    let divider = 
        Html.div [ 
            prop.className "border border-slate-400"
            prop.style 
                [style.margin(2,0); style.width (length.perc 80); style.margin length.auto; style.margin (length.rem 0)]  
        ]  

open Fable.SimpleJson      

module FunctionsContextmenu =

    // let isLocalStorageClear (key:string) () =
    //         match (Browser.WebStorage.localStorage.getItem key) with
    //         | null -> true // Local storage is clear if the item doesn't exist
    //         | _ -> false 

    // let setLocalHighlight (id: string)(nextList: string list) =
    //       let JSONstring= 
    //           Json.stringify nextList 
    //       Browser.WebStorage.localStorage.setItem(id, JSONstring)

    // let initialKeyhighlight (id: string) =
    //     if isLocalStorageClear id () = true then []
    //     else Json.parseAs<string list> (Browser.WebStorage.localStorage.getItem id) 

    // let initialTermhighlight (id: string) =
    //     if isLocalStorageClear id () = true then []
    //     else Json.parseAs<string list> (Browser.WebStorage.localStorage.getItem id) 

    // let mutable highlightKeyList = initialKeyhighlight "keyHighlight" 
    
    // let mutable highlightTermList = initialTermhighlight "termHighlight"
    
    let addAnnotationKeyNew (state: Annotation list, setState: Annotation list -> unit, elementID: string, highlight: Highlight, setHighlight: Highlight -> unit) ()=       
        let term = window.getSelection().ToString().Trim() 
        let yCoordinateOfSelection  =
            match window.getSelection() with
            | (selection: Selection) when selection.rangeCount > 0 ->
                let range = selection.getRangeAt(0)
                let rect = range.getBoundingClientRect()
                let relativeParent = document.getElementById(elementID).getBoundingClientRect()
                rect.bottom - relativeParent.top + 12.0
            | _ -> 0.0    

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> {a with IsOpen = false}) 
            let newAnnoList = [Annotation.init(OntologyAnnotation(term), body = CompositeCell.Term(OntologyAnnotation("")), height = yCoordinateOfSelection)]

            setState (List.append closedList newAnnoList)

            let newKeys =
                highlight.Keys
                |> Map.add yCoordinateOfSelection term

            setHighlight {highlight with Keys = newKeys}

        else 
            ()

        log yCoordinateOfSelection
            
        Browser.Dom.window.getSelection().removeAllRanges()  

        
    let addAnnotationBodyNew (state: Annotation list, setState: Annotation list -> unit, elementID: string, highlight: Highlight, setHighlight: Highlight -> unit) ()=  
        let term = window.getSelection().ToString().Trim()      
        let yCoordinateOfSelection  =
            match window.getSelection() with
            | (selection: Selection) when selection.rangeCount > 0 ->
                let range = selection.getRangeAt(0)
                let rect = range.getBoundingClientRect()
                let relativeParent = document.getElementById(elementID).getBoundingClientRect()
                rect.bottom - relativeParent.top + 12.0
                
            | _ -> 0.0     

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> {a with IsOpen = false}) 
            let newAnnoList = [Annotation.init(OntologyAnnotation(""), body = CompositeCell.Term(OntologyAnnotation(term)), height = yCoordinateOfSelection)]


            setState (List.append closedList newAnnoList)

            let newTerms =
                highlight.Terms
                |> Map.add yCoordinateOfSelection term

            setHighlight {highlight with Terms = newTerms}
            
        else 
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges()   

    let addAnnotationValueNew (state: Annotation list, setState: Annotation list -> unit, elementID: string, highlight: Highlight, setHighlight: Highlight -> unit) ()=  
        let term = window.getSelection().ToString().Trim()      
        let yCoordinateOfSelection  =
            match window.getSelection() with
            | (selection: Selection) when selection.rangeCount > 0 ->
                let range = selection.getRangeAt(0)
                let rect = range.getBoundingClientRect()
                let relativeParent = document.getElementById(elementID).getBoundingClientRect()
                rect.bottom - relativeParent.top + 12.0
                
            | _ -> 0.0     

        if term.Length <> 0 then
            let closedList = state |> List.map (fun a -> {a with IsOpen = false}) 
            let newAnnoList = [Annotation.init(OntologyAnnotation(""), body = CompositeCell.Unitized(term,OntologyAnnotation("")),height = yCoordinateOfSelection)]


            setState (List.append closedList newAnnoList)

            let newValues =
                highlight.Values
                |> Map.add yCoordinateOfSelection term

            setHighlight {highlight with Values = newValues}
            
        else 
            ()

        log yCoordinateOfSelection

        Browser.Dom.window.getSelection().removeAllRanges()   
       

    let addToLastAnnoAsKey(state: Annotation list, setState: Annotation list -> unit, highlight: Highlight, setHighlight: Highlight -> unit) () =
        let term = window.getSelection().ToString().Trim()
        if term.Length <> 0 then 

            let updatetedAnno = 
                {state.[state.Length - 1] with Search.Key = OntologyAnnotation(name = term)}

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newKeys =
                let height = state.[state.Length - 1].Height
                highlight.Keys
                |> Map.add height term

            setHighlight {highlight with Keys = newKeys}

    let addToLastAnnoAsBody(state: Annotation list, setState: Annotation list -> unit, highlight: Highlight, setHighlight: Highlight -> unit) () =
        let term = window.getSelection().ToString().Trim()
        if term.Length <> 0 then 
            let updatetedAnno =
                match state.[state.Length - 1].Search.Body with
                | CompositeCell.Unitized (v, oa) ->
                    {state.[state.Length - 1] with Search.Body = CompositeCell.Unitized(v, OntologyAnnotation(term))}
                | _ ->
                    {state.[state.Length - 1] with Search.Body = CompositeCell.Term(OntologyAnnotation(term))}

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newTerms =
                let height = state.[state.Length - 1].Height
                highlight.Terms
                |> Map.add height term

            setHighlight {highlight with Terms = newTerms}

    let addToLastAnnoAsValue(state: Annotation list, setState: Annotation list -> unit,  highlight: Highlight, setHighlight: Highlight -> unit) () =
        let term = window.getSelection().ToString().Trim()
        if term.Length <> 0 then 
            let updatetedAnno = 
                match state.[state.Length - 1].Search.Body with
                | CompositeCell.Term oa ->
                    {state.[state.Length - 1] with Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(oa.NameText))}
                | CompositeCell.Unitized (v, oa) ->
                    {state.[state.Length - 1] with Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(oa.NameText))}
                | _ ->
                    {state.[state.Length - 1] with Search.Body = CompositeCell.Unitized(term, OntologyAnnotation(""))}
                // {state.[state.Length - 1] with Search.Body = CompositeCell.Unitized(term,OntologyAnnotation(state.[state.Length - 1].Search.Body.ToString())); HighlightTerms = term}

            let newAnnoList =
                state
                |> List.mapi (fun i elem -> if i = state.Length - 1 then updatetedAnno else elem)

            setState newAnnoList

            let newValues =
                let height = state.[state.Length - 1].Height
                highlight.Values
                |> Map.add height term

            setHighlight {highlight with Values = newValues}


open Helper

open FunctionsContextmenu

module Contextmenu =
    let private contextmenu (mousex: float, mousey: float) (resetter: unit -> unit, state: Annotation list, setState: Annotation list -> unit, elementID:string, highlight: Highlight, setHighlight: Highlight -> unit )=
        /// This element will remove the contextmenu when clicking anywhere else
        let buttonList = [
            Html.div [ 
                prop.className "text-gray-500 text-sm p-1"
                prop.text "Add new annotation as .."
            ]
            button ("Key", resetter, state, addAnnotationKeyNew(state, setState, elementID, highlight, setHighlight), [])
            button ("Term", resetter,state, addAnnotationBodyNew(state, setState,elementID, highlight, setHighlight), []) 
            button ("Value", resetter,state, addAnnotationValueNew(state, setState,elementID, highlight, setHighlight), [])
            divider
            Html.div [ 
                prop.className "text-gray-500 text-sm p-1"
                prop.text "Add to last annotation as .."
            ]
            button ("Key", resetter,state, addToLastAnnoAsKey(state, setState, highlight, setHighlight),  [])
            button ("Term", resetter,state, addToLastAnnoAsBody(state, setState, highlight, setHighlight),  [])
            button ("Value", resetter,state, addToLastAnnoAsValue(state, setState, highlight, setHighlight),  [])
        ]
        Html.div [
            prop.tabIndex 0
            preventDefault
            prop.className "bg-[#cae8f4] border-slate-400 border-solid border"
            prop.style [
                style.backgroundColor " "
                style.position.absolute
                style.left (int mousex)
                style.top (int mousey)
                style.width 150
                style.zIndex 40
            ]
            prop.children [
                Html.ul buttonList
            ]
        ]

    let initialModal = {
                isActive = false
                location = (0.0,0.0)
            }

    let onContextMenu (modalContext:DropdownModal, state: Annotation list, setState: Annotation list -> unit, elementID:string, highlight: Highlight, setHighlight: Highlight -> unit) = 
        let resetter = fun () -> modalContext.setter initialModal //add actual function
        // let rmv = modalContext.setter initialModal 
        contextmenu (modalContext.modalState.location) (resetter, state, setState,elementID, highlight, setHighlight)


    
            
            

            
            



        


        




        



