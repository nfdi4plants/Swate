module OfficeAddinMock

open Fable.Core
open Fable.Core.JsInterop

type OfficeAddinMock =
    [<Emit("new $0($1)")>]
    abstract member OfficeMockObject: obj -> obj

let OfficeAddinMock: OfficeAddinMock = importDefault "office-addin-mock"