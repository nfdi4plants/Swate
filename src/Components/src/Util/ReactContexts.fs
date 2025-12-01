module Swate.Components.Contexts

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
module TermSearch =

    let TermSearchConfigCtx =
        React.createContext<TermSearchConfigCtx> (TermSearchConfigCtx.init ())

    let TermSearchActiveKeysCtx =
        React.createContext<StateContext<TermSearchConfigLocalStorageActiveKeysCtx>> (
            {
                data = TermSearchConfigLocalStorageActiveKeysCtx.init ()
                setData = fun keys -> printfn "Setting active keys not given: %A" keys
            }
        )

    let TermSearchAllKeysCtx = React.createContext<Set<string>> (Set.empty)

[<Erase; Mangle(false)>]
module Table =

    type TableState
        (
            isActive: CellCoordinate -> bool,
            isOrigin: CellCoordinate -> bool,
            isSelected: CellCoordinate -> bool,
            onBlur: CellCoordinate -> Browser.Types.FocusEvent -> unit,
            onKeyDown: CellCoordinate -> Browser.Types.KeyboardEvent -> unit,
            onClick: CellCoordinate -> Browser.Types.MouseEvent -> unit
        ) =
        member val isActive = isActive with get, set
        member val isOrigin = isOrigin with get, set
        member val isSelected = isSelected with get, set
        member val onBlur = onBlur with get, set
        member val onKeyDown = onKeyDown with get, set
        member val onClick = onClick with get, set

    module TableState =

        let init () =
            TableState(
                (fun _ ->
                    console.warn "TableCtx default isActive"
                    false
                ),
                (fun _ ->
                    console.warn "TableCtx default isOrigin"
                    false
                ),
                (fun _ ->
                    console.warn "TableCtx default isSelected"
                    false
                ),
                (fun _ _ -> console.warn "TableCtx default onBlur"),
                (fun _ _ -> console.warn "TableCtx default onKeyDown"),
                (fun _ _ -> console.warn "TableCtx default onClick")
            )

    let TableStateCtx = React.createContext<TableState> (TableState.init ())


[<Erase; Mangle(false)>]
module AnnotationTable =

    type AnnotationTableContext = {
        SelectedCells: CellCoordinateRange option
    } with

        static member init(?selectedCells) = { SelectedCells = selectedCells }

    let AnnotationTableStateCtx =
        React.createContext<StateContext<Map<string, AnnotationTableContext>>> (
            {
                data = Map.empty
                setData = fun _ -> console.warn "No context provider for AnnotationTableStateCtx found!"
            }
        )

[<Erase; Mangle(false)>]
module BaseModal =
    type BaseModalContext = {
        isOpen: bool
        setIsOpen: bool -> unit
        headerId: string
        descId: string
    }

    let BaseModalCtx = React.createContext<BaseModalContext option> (None)