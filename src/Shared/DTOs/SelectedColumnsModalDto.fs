module Shared.DTOs.SelectedColumnsModalDto

type SelectedColumns = {
    Columns: bool []
}
with
    static member init(length) =
        {
            Columns = Array.init length (fun _ -> true)
        }