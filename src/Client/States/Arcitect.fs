namespace ARCitect

type Msg =
    | Init
    | Error of exn

type IEventHandler = {
    InitResponse: string -> unit
    Error: exn -> unit
}