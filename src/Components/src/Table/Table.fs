namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI

type private Person = {|
    id: int;
    firstName: string;
    lastName: string;
    age: int;
    visits: int;
    progress: int;
    status: string;
    createdAt: System.DateTime;
|}

module private Mock =

    open System

    let private random = Random()

    let private firstNames = ["Alice"; "Bob"; "Charlie"; "David"; "Emma"; "Frank"; "Grace"; "Hannah"; "Isaac"; "Julia"; "Kevin"; "Laura"; "Michael"; "Nina"; "Oliver"; "Paul"; "Quinn"; "Rachel"; "Sam"; "Tina"]
    let private lastNames = ["Smith"; "Johnson"; "Williams"; "Brown"; "Jones"; "Garcia"; "Miller"; "Davis"; "Rodriguez"; "Martinez"; "Hernandez"; "Lopez"; "Gonzalez"; "Wilson"; "Anderson"; "Thomas"; "Taylor"; "Moore"; "Jackson"; "Martin"]
    let private statuses = ["Active"; "Inactive"; "Pending"]

    let generatePerson id =
        {|
            id = id
            firstName = firstNames.[random.Next(firstNames.Length)]
            lastName = lastNames.[random.Next(lastNames.Length)]
            age = random.Next(18, 80)
            visits = random.Next(0, 100)
            progress = random.Next(0, 100)
            status = statuses.[random.Next(statuses.Length)]
            createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 365))
        |}

    let generateStaticPersonFromId id =
        {|
            id = id
            firstName = "FirstName" +  string id
            lastName = "LastName" + string id
            age = 42
            visits = 88
            progress = 100
            status = "Active"
            createdAt = DateTime.UtcNow.AddDays(-365)
        |}


[<Mangle(false); Erase>]
type Table =


    // example:
    // https://tanstack.com/virtual/latest/docs/framework/react/examples/table
    [<ReactComponent(true)>]
    static member Table<'A>(data: ResizeArray<'A>, setData: ResizeArray<'A> -> unit) =
        Html.div [

        ]