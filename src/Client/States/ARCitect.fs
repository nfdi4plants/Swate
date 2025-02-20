module Model.ARCitect

open ARCtrl
open Fable.Core

module Interop =

    [<RequireQualifiedAccess>]
    module InteropTypes =

        /// StringEnum to make it a simple string in js world
        [<RequireQualifiedAccessAttribute>]
        [<StringEnum>]
        type ARCFile =
            | Investigation
            | Study
            | Assay
            | Template

    type IARCitectOutAPI = {
        Init: unit -> JS.Promise<InteropTypes.ARCFile * string>
        Save: InteropTypes.ARCFile * string -> JS.Promise<unit>
        RequestPaths: bool -> JS.Promise<bool>
        /// returns person jsons
        RequestPersons: unit -> JS.Promise<string []>
    }

    type IARCitectInAPI = {
        TestHello: string -> JS.Promise<string>
        /// JS.Promise<wasSuccessful: bool>
        ResponsePaths: string [] -> JS.Promise<bool>
    }


let api =
    MessageInterop.MessageInterop.createApi()
    |> MessageInterop.MessageInterop.buildOutProxy<Interop.IARCitectOutAPI>

open Elmish

type Msg =
    | Init of ApiCall<unit, (Interop.InteropTypes.ARCFile * string)>
    | Save of ArcFiles
    /// ApiCall<selectDirectories: bool, wasSuccessful: bool>
    | RequestPaths of ApiCall<bool, bool>
    /// Selecting paths requires user input, which we cannot await.
    /// To avoid timeout `RequestPaths` simply returns true if call was successful, 
    /// ... and `ResponsePaths` will be sent as soon as user selected the directories 
    | ResponsePaths of string []
    /// expects person jsons
    | RequestPersons of ApiCall<unit, string []>

type Model =
    {
        Paths: string []
        Persons: Person []
    }

    static member init() = {
        Paths = [||];
        Persons = [||]
    }