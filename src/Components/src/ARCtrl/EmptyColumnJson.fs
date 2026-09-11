module internal ARCtrl.EmptyColumnJson

open Fable.Core
open Fable.Core.JsInterop

module private NativeJson =
    [<Emit("JSON.parse($0, $1)")>]
    let parseWithReviver (json: string) (reviver: System.Func<string, obj, obj>) : obj = jsNative

    [<Emit("Array.isArray($0)")>]
    let isArray (value: obj) : bool = jsNative

    [<Emit("$0 !== null && typeof $0 === 'object' && !Array.isArray($0)")>]
    let isObject (value: obj) : bool = jsNative

    [<Emit("$0 === null")>]
    let isNull (value: obj) : bool = jsNative

    [<Emit("Number.isInteger($0)")>]
    let isInteger (value: obj) : bool = jsNative

[<AllowNullLiteral>]
type private TableJson =
    abstract headers: obj
    abstract rowCount: float
    abstract columns: obj
    abstract h: obj
    abstract r: float
    abstract c: obj

/// Temporary workaround for ARCtrl encoding an empty sparse column as null,
/// although its decoder expects an array. Operates only on serialized table
/// payloads; the source ARC and its sparse cells are never mutated.
let normalize (compressed: bool) (json: string) =
    if not (json.Contains "null") then
        json
    else
        let mutable changed = false

        let normalizeTable (value: obj) =
            if NativeJson.isObject value then
                let table = unbox<TableJson> value

                let headers, rowCount, columns =
                    if compressed then
                        table.h, table.r, table.c
                    else
                        table.headers, table.rowCount, table.columns

                if rowCount > 0. && NativeJson.isArray headers && NativeJson.isArray columns then
                    for entry in unbox<obj[]> columns do
                        if NativeJson.isArray entry then
                            let pair = unbox<obj[]> entry

                            if pair.Length = 2 && NativeJson.isInteger pair.[0] && NativeJson.isNull pair.[1] then
                                pair.[1] <- box ([||]: obj[])
                                changed <- true

        let parsed =
            NativeJson.parseWithReviver
                json
                (System.Func<_, _, _>(fun key value ->
                    // Both ARCtrl formats use Tables for assay/study/run tables
                    // and table for templates, including nested ARC documents.
                    if key = "Tables" && NativeJson.isArray value then
                        unbox<obj[]> value |> Array.iter normalizeTable
                    elif key = "table" then
                        normalizeTable value

                    value
                ))

        if changed then JS.JSON.stringify parsed else json
