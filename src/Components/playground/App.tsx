import React, { Fragment } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import Table from '../src/Table/Table.fs.ts';
import VirtualizedGrid from './Virtualized.tsx';

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

function TableContainer() {
  const [data, setData] = React.useState(Array.from({ length: 10_000 }, (_, i) => ({
    id: i,
    name: `Name ${i}`,
    description: `Description ${i}`,
  })));
  const columnDefs = [
    { id: 'index',
      header: "Index",
      cell: (props: any) => {
          return (
            <div className='text-end bg-black text-emerald-300'>{props.row.index}</div>
          );
      },
      size: 0,
    },
    {
      header: 'Name',
      accessorKey: 'name',
      cell: (props: any) => {
          return (
            <div className='px-2 text-end'>{props.cell.getValue()}</div>
          );
      },
    },
    {
      header: 'Description',
      accessorKey: 'description',
      cell: (props: any) => {
          return (
            <div className='px-2 text-end'>{props.cell.getValue()}</div>
          );
      },
    },
  ]
  return <Fragment>
    <h2 className='text-3xl'>Table</h2>
    <Table
      data={data}
      setData={setData}
      columnDefs={columnDefs}
    />
  </Fragment>
}

function VirtualizedGridContainer() {
  return <Fragment>
    <h2 className='text-3xl'>VirtualizedGrid</h2>
    <VirtualizedGrid />
  </Fragment>
}


const App = () => {
    return (
        <div className="container mx-auto flex flex-col p-2 gap-4">
            <h1 className='text-6xl'>Playground</h1>
            <TermSearchContainer />
            <VirtualizedGridContainer />
            <TableContainer />
        </div>
    );
};

export default App;
