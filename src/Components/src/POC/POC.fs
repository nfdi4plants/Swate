namespace Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz


/// These 2 Attributes will remove the type from the generated JS code
/// ⚠️ Due to [<Mangle(false)>] you can never use overloads in the same file!
[<Erase; Mangle(false)>]
type TestInput =

    /// [<ExportDefault>] can only be used on a single member in a module
    /// [<NamedParams>] is used for correct type hinting in typescript
    /// with: export function TestInput({ children, number }: {children?: ReactElement, number?: int32 }): any {
    /// without: export function TestInput(children?: ReactElement, number?: int32): any {
    /// ⚠️ ... fails because react requires object as input
    [<ExportDefault; NamedParams>]
    static member TestInput(?children: ReactElement, ?number: int) =
        let state, useState = React.useState (number |> Option.defaultValue 0)
        Html.div [
            prop.children [
                if children.IsSome then children.Value
                Html.div [
                    prop.textf "Number: %d" state
                ]
                Html.input [
                    prop.type'.number
                    prop.onChange(fun (number:int) -> useState number)
                ]
            ]
        ]
