module Modals.Util

let inline RMV_MODAL dispatch = fun _ -> None |> Messages.UpdateModal |> dispatch