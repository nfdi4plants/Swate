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

        [<RequireQualifiedAccessAttribute>]
        [<StringEnum>]
        type ARCitectPathsTarget =
            | FilePicker
            | DataAnnotator

    type RequestPathsPojo = {| target: InteropTypes.ARCitectPathsTarget; dictionaries: bool|}

    type ResponsePathsPojo = {| target: InteropTypes.ARCitectPathsTarget; paths: string []|}

    type IARCitectOutAPI = {
        Init: unit -> JS.Promise<InteropTypes.ARCFile * string>
        Save: InteropTypes.ARCFile * string -> JS.Promise<unit>
        RequestPaths: RequestPathsPojo -> JS.Promise<bool>
        /// returns person jsons
        RequestPersons: unit -> JS.Promise<string []>
    }

    type IARCitectInAPI = {
        TestHello: string -> JS.Promise<string>
        /// JS.Promise<wasSuccessful: bool>
        ResponsePaths: ResponsePathsPojo -> JS.Promise<bool>
    }


let api =
    MessageInterop.MessageInterop.createApi()
    |> MessageInterop.MessageInterop.buildOutProxy<Interop.IARCitectOutAPI>

open Elmish

type Msg =
    | Init of ApiCall<unit, (Interop.InteropTypes.ARCFile * string)>
    | Save of ArcFiles
    /// ApiCall<selectDirectories: bool, wasSuccessful: bool>
    | RequestPaths of ApiCall<Interop.RequestPathsPojo, bool>
    /// Selecting paths requires user input, which we cannot await.
    /// To avoid timeout `RequestPaths` simply returns true if call was successful,
    /// ... and `ResponsePaths` will be sent as soon as user selected the directories
    | ResponsePaths of Interop.ResponsePathsPojo
    /// expects person jsons
    | RequestPersons of ApiCall<unit, string []>

type Model =
    {
        Persons: Person []
    }

    static member init() = {
        Persons = [||]
    }