namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


module private TanStack =

    [<Literal>]
    let private TanStackTable = "@tanstack/react-table"
    module rec Table =

        [<StringEnum>]
        type SortDirection =
            | [<CompiledName("asc")>] Asc
            | [<CompiledName("desc")>] Desc

        type CellContext<'A, 'B> =
            abstract member getValue<'T>: unit -> 'T

        [<Import("Cell",TanStackTable)>]
        type Cell<'A, 'B> =
            abstract member id: string
            abstract member getContext: unit -> obj
            abstract member column: Column<'A, 'B>

        [<Import("Row",TanStackTable)>]
        type Row<'A> =
            abstract member id: string
            abstract member getVisibleCells: unit -> Cell<'A, obj> []

        [<Import("RowModel",TanStackTable)>]
        type RowModel<'A> =
            abstract member rows: Row<'A> []


        [<Import("ColumnDef",TanStackTable)>]
        type ColumnDef<'A, 'B> =
            abstract member header: obj
            abstract member cell: obj

        [<Import("Column",TanStackTable)>]
        type Column<'A, 'B> =
            abstract member getCanSort: unit -> bool
            abstract member getToggleSortingHandler: unit -> ((obj -> unit) option)
            abstract member columnDef: ColumnDef<'A, 'B>
            abstract member getIsSorted: unit -> U2<bool, SortDirection>

        [<Import("HeaderContext",TanStackTable)>]
        type HeaderContext<'A, 'B> =
            interface end

        [<Import("Header",TanStackTable)>]
        type Header<'A, 'B> =
            abstract member id: string
            abstract member colSpan: int
            abstract member getSize: unit -> int
            abstract member isPlaceholder: bool
            abstract member column: Column<'A, 'B>
            abstract member getContext: unit -> HeaderContext<'A, 'B>

        [<Import("HeaderGroup",TanStackTable)>]
        type HeaderGroup<'A> =
            abstract member id: string
            abstract member headers: Header<'A, obj> []

        [<Import("Table",TanStackTable)>]
        type Table<'A> =
            abstract member getRowModel : unit -> RowModel<'A>
            abstract member getHeaderGroups : unit -> HeaderGroup<'A> []

        [<Import("ColumnSort",TanStackTable)>]
        type ColumnSort = interface end

        [<Import("SortingState",TanStackTable)>]
        type SortingState = ColumnSort []

        // [<ReactComponent("flexRender",TanStackTable)>]
        // let flexRender(
        //     comp: obj,
        //     props: obj
        // ) = Feliz.React.imported()

        [<ImportMember(TanStackTable)>]
        let flexRender(
            comp: obj,
            props: obj
        ) : ReactElement = jsNative


        // https://fable.io/docs/javascript/features.html#using-delegates-for-disambiguation
        [<ImportMember(TanStackTable)>]
        let inline getCoreRowModel() : obj = jsNative

        // https://fable.io/docs/javascript/features.html#using-delegates-for-disambiguation
        [<ImportMember(TanStackTable)>]
        let inline getSortedRowModel() : obj = jsNative

        [<NamedParamsAttribute>]
        [<ImportMember(TanStackTable)>]
        let inline useReactTable<'A>(
            data: ResizeArray<'A>,
            columns: obj,
            state: obj,
            onSortingChange: SortingState -> unit,
            getCoreRowModel: obj,
            getSortedRowModel: obj,
            debugTable: bool
        ) : Table<'A> = jsNative

    module Virtual =
        [<Literal>]
        let private TanStackVirtual = "@tanstack/react-virtual"

        [<Import("VirtualItem",TanStackVirtual)>]
        type VirtualItem =
            abstract member size: int
            abstract member start: int
            abstract member index: int

        [<Import("Virtualizer",TanStackVirtual)>]
        type Virtualizer<'A, 'B> =
            abstract member getTotalSize: unit -> int
            abstract member getVirtualItems: unit -> VirtualItem []

        [<NamedParamsAttribute>]
        [<ImportMember(TanStackVirtual)>]
        let useVirtualizer<'A, 'B>(
            count: int,
            getScrollElement: unit -> Browser.Types.HTMLElement option,
            estimateSize: unit -> int,
            overscan: int
        ) : Virtualizer<'A, 'B> = jsNative

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

open TanStack.Table
open TanStack.Virtual

[<Mangle(false); Erase>]
type Table =

    static member private Header(header: TanStack.Table.Header<Person,obj>) =
        Html.th [
            prop.key header.id
            prop.colSpan header.colSpan
            prop.style [
                style.width (header.getSize())
            ]
            prop.children [
                if not header.isPlaceholder then
                    Html.div [
                        prop.className [
                            if header.column.getCanSort() then
                                "cursor-pointer select-none"
                        ]
                        prop.onClick(
                            match header.column.getToggleSortingHandler() with
                            | Some handler ->
                                fun _ -> handler()
                            | None ->
                                fun _ -> ()
                        )
                        prop.children [
                            TanStack.Table.flexRender(
                                header.column.columnDef.header,
                                header.getContext()
                            )
                            match header.column.getIsSorted() with
                            | U2.Case2 TanStack.Table.SortDirection.Asc ->
                                Html.span [
                                    prop.className "ml-2"
                                    prop.children [
                                        Html.i [
                                            prop.className "Up"
                                        ]
                                    ]
                                ]
                            | U2.Case2 TanStack.Table.SortDirection.Desc ->
                                Html.span [
                                    prop.className "ml-2"
                                    prop.children [
                                        Html.i [
                                            prop.className "Down"
                                        ]
                                    ]
                                ]
                            | _ -> Html.none
                        ]
                    ]
            ]
        ]

    static member private TBody(virtualizer: Virtualizer<_,_>, rows: Row<Person> []) =
        Html.tbody [
            prop.children (
                virtualizer.getVirtualItems()
                |> Array.mapi (fun index virtualRow ->
                    let row = rows.[virtualRow.index]
                    let yTranslate = virtualRow.start - index * virtualRow.size
                    Html.tr [
                        prop.key row.id
                        prop.style [
                            style.height virtualRow.size
                            style.custom("transform", sprintf "translateY(%dpx)" yTranslate)
                        ]
                        prop.children (
                            row.getVisibleCells()
                            |> Array.map(fun cell ->
                                Html.td [
                                    prop.key cell.id
                                    prop.children [
                                        TanStack.Table.flexRender(
                                            cell.column.columnDef.cell,
                                            cell.getContext()
                                        )
                                    ]
                                ]
                            )
                        )
                    ]
                )
            )
        ]

    // example:
    // https://tanstack.com/virtual/latest/docs/framework/react/examples/table
    [<ReactComponent(true)>]
    static member Table() =
        let sorting, setSorting = React.useState<TanStack.Table.SortingState>([||])

        let columns = React.useMemo<TanStack.Table.ColumnDef<Person, obj> []>(fun () ->
            [|
                !!{| accessorKey = "id"; header = "ID"; size = 60 |}
                !!{| accessorKey = "firstName"; cell = fun info -> info?getValue() |}
                !!{|
                    accessorFn = (fun row -> row?lastName);
                    id = "lastName";
                    cell = fun info -> info?getValue()
                    header = Html.span [prop.text "Last Name"]
                |}
                !!{|
                    accessorKey = "age"
                    header = fun () -> "Age"
                    size = 50
                |}
                !!{|
                    accessorKey = "visits"
                    header = fun () -> Html.span [prop.text "Visits"; prop.className "text-primary"]
                    size = 50
                |}
                !!{|
                    accessorKey = "status"
                    header = fun () -> Html.span [prop.text "Status"; prop.className "text-primary"]
                |}
                !!{|
                    accessorKey = "progress"
                    header = fun () -> Html.span [prop.text "Profile Progress"; prop.className "text-primary"]
                    size = 80
                |}
                !!{|
                    accessorKey = "createdAt"
                    header = fun () -> Html.span [prop.text "Created At"; prop.className "text-primary"]
                    cell = fun info ->
                        let dt: System.DateTime = info?getValue()
                        dt.Year.ToString() + "-" + dt.Month.ToString() + "-" + dt.Day.ToString()
                |}
            |]
        )

        let (data: ResizeArray<Person>), setData = React.useState(fun () ->
            let ra = ResizeArray()
            for i in 0 .. 5000 do
                ra.Add (Mock.generatePerson i)
            ra
        )

        let table = TanStack.Table.useReactTable<Person>(
            data,
            columns,
            !!{|sorting = sorting|},
            setSorting,
            TanStack.Table.getCoreRowModel(),
            TanStack.Table.getSortedRowModel(),
            true
        )

        let rows = table.getRowModel().rows

        let parentRef = React.useElementRef()

        let virtualizer =
            TanStack.Virtual.useVirtualizer(
                Array.length rows,
                (fun () -> parentRef.current),
                (fun () -> 34),
                20
            )

        Html.div [
            prop.ref parentRef
            prop.className "p-2"
            prop.children [
                Html.div [
                    prop.style [
                        style.height (virtualizer.getTotalSize())
                    ]
                    prop.children [
                        Html.table [
                            Html.thead (
                                table.getHeaderGroups()
                                |> Array.map (fun (headerGroup) ->
                                    Html.tr [
                                        prop.key headerGroup.id
                                        prop.children (
                                            headerGroup.headers
                                            |> Array.map (fun (header) ->
                                                Table.Header(header)
                                            )
                                        )
                                    ]
                                )
                            )
                            Table.TBody(virtualizer, rows)
                        ]
                    ]
                ]
            ]
        ]
