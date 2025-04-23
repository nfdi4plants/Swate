import React, { Fragment } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import {Entry as Table} from '../src/Table/Table.fs.ts';
// // import {type Term} from '../src/Util/Types.fs.ts';
import {Entry as AnnotationTable} from '../src/Table/AnnotationTable.fs.ts';
import {Example as ContextMenu} from '../src/GenericComponents/ContextMenu.fs.ts';

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

  return <div className='flex flex-col gap-4'>
    <h2 className='text-3xl'>Table</h2>
    <Table />
  </div>
}

function AnnoTableContainer() {
  return <div className='flex flex-col gap-4'>
    <h2 className='text-3xl'>Annotation Table</h2>
    <AnnotationTable />
  </div>
}

function ContextMenuContainer() {
  return <div className='flex flex-col gap-4'>
    <h2 className='text-3xl'>Context Menu</h2>
    <ContextMenu />
  </div>
}

const App = () => {
    return (
        <div className="container mx-auto flex flex-col p-2 gap-4 mb-12">
            <h1 className='text-6xl'>Playground</h1>
            <TermSearchContainer />
            <ContextMenuContainer />
            <AnnoTableContainer />
            <TableContainer />
        </div>
    );
};

export default App;
