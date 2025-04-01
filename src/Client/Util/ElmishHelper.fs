[<AutoOpenAttribute>]
module ElmishHelper

module Elmish =

    type ApiCallWithFail<'s, 'f> =
        | Start of 's
        | Finished of 'f
        | Failed of exn

    type ApiCall<'s, 'f> =
        | Start of 's
        | Finished of 'f