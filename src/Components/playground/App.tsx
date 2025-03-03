import React, { Fragment } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import Table from '../src/Table/Table.fs.ts';
import { Term } from '../../Shared/Database.fs.ts';


function TermSearchContainer() {
  const [term, setTerm] = React.useState(undefined);
  return <Fragment>
    <h2 className='text-3xl'>TermSearch</h2>
    {/* <TermSearch
      onTermSelect={(term) => setTerm(term as Term | undefined)}
      term={term}
      showDetails
      debug={true}
    /> */}
    <TermSearch
      onTermSelect={(term) => setTerm(term as Term | undefined)}
      term={term}
      parentId="MS:1000031"
      showDetails
      debug={true}
      advancedSearch
    />
  </Fragment>
}

import { useVirtualizer } from '@tanstack/react-virtual'
import {
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { ColumnDef, Row, SortingState } from '@tanstack/react-table'

type Person = {
  id: number
  firstName: string
  lastName: string
  age: number
  visits: number
  progress: number
  status: 'relationship' | 'complicated' | 'single'
  createdAt: Date
}

function makeData(count: number): Array<Person> {
  // function to start performance profiling
  console.log("[Start mocking data]")
  const start = performance.now()
  const results = Array.from({ length: count }, (_, index) => {
    const random = Math.random()
    const person: Person = {
      id: index,
      firstName: 'John',
      lastName: 'Doe',
      age: 33,
      visits: 45,
      progress: Math.round(random * 100),
      status: ['relationship', 'complicated', 'single'][
        Math.floor(random * 3)
      ] as 'relationship' | 'complicated' | 'single',
      createdAt: new Date(Date.now() - Math.round(random * 1e10)),
    }
    return person;
  })
  const end = performance.now();
  console.log(`Execution time: ${end - start} ms`);
  console.log("[Finish mocking data]")
  return results;
}

function ReactTableVirtualized() {
  const [sorting, setSorting] = React.useState<SortingState>([])

  const columns = React.useMemo<Array<ColumnDef<Person>>>(
    () => [
      {
        accessorKey: 'id',
        header: 'ID',
        size: 60,
      },
      {
        accessorKey: 'firstName',
        cell: (info) => info.getValue(),
      },
      {
        accessorFn: (row) => row.lastName,
        id: 'lastName',
        cell: (info) => info.getValue(),
        header: () => <span>Last Name</span>,
      },
      {
        accessorKey: 'age',
        header: () => 'Age',
        size: 50,
      },
      {
        accessorKey: 'visits',
        header: () => <span>Visits</span>,
        size: 50,
      },
      {
        accessorKey: 'status',
        header: 'Status',
      },
      {
        accessorKey: 'progress',
        header: 'Profile Progress',
        size: 80,
      },
      {
        accessorKey: 'createdAt',
        header: 'Created At',
        cell: (info) => info.getValue<Date>().toLocaleString(),
      },
    ],
    [],
  )

  const [data, setData] = React.useState(() => makeData(5_000))

  const table = useReactTable({
    data,
    columns,
    state: {
      sorting,
    },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    debugTable: true,
  })

  const { rows } = table.getRowModel()

  const parentRef = React.useRef<HTMLDivElement>(null)

  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 34,
    overscan: 20,
  })

  return (
    <div ref={parentRef} className="container">
      <div style={{ height: `${virtualizer.getTotalSize()}px` }}>
        <table>
          <thead>
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  return (
                    <th
                      key={header.id}
                      colSpan={header.colSpan}
                      style={{ width: header.getSize() }}
                    >
                      {header.isPlaceholder ? null : (
                        <div
                          {...{
                            className: header.column.getCanSort()
                              ? 'cursor-pointer select-none'
                              : '',
                            onClick: header.column.getToggleSortingHandler(),
                          }}
                        >
                          {flexRender(
                            header.column.columnDef.header,
                            header.getContext(),
                          )}
                          {{
                            asc: ' 🔼',
                            desc: ' 🔽',
                          }[header.column.getIsSorted() as string] ?? null}
                        </div>
                      )}
                    </th>
                  )
                })}
              </tr>
            ))}
          </thead>
          <tbody>
            {virtualizer.getVirtualItems().map((virtualRow, index) => {
              const row = rows[virtualRow.index]
              return (
                <tr
                  key={row.id}
                  style={{
                    height: `${virtualRow.size}px`,
                    transform: `translateY(${
                      virtualRow.start - index * virtualRow.size
                    }px)`,
                  }}
                >
                  {row.getVisibleCells().map((cell) => {
                    return (
                      <td key={cell.id}>
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext(),
                        )}
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function TableContainer() {
  return <Fragment>
    <h2 className='text-3xl'>Table</h2>
    <div className='max-h-[600px] overflow-y-auto border'>
      {/* <Table /> */}
      <ReactTableVirtualized />
    </div>
  </Fragment>
}


const App = () => {
    return (
        <div className="container mx-auto flex flex-col p-2 gap-4">
            <h1 className='text-6xl'>Playground</h1>
            <TermSearchContainer />
            <TableContainer />
        </div>
    );
};

export default App;
