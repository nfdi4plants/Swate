import React, { Fragment } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import {Entry as Table} from '../src/Table/Table.fs.ts';
import {Entry as AnnotationTable} from '../src/Table/AnnotationTable.fs.ts';
import {Example as ContextMenuExample, ContextMenu} from '../src/GenericComponents/ContextMenu.fs.ts';
import { Menu } from "./NativeContextMenu";

function TermSearchContainer() {
  const [term, setTerm] = React.useState(undefined);
  return <Fragment>
    <h2 className='swt:text-3xl'>TermSearch</h2>
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

  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Table</h2>
    <Table />
  </div>
}

function AnnoTableContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Annotation Table</h2>
    <AnnotationTable />
  </div>
}

function ContextMenuContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Context Menu</h2>
    <ContextMenuExample />
  </div>
}

const App = () => {
    return (
        <div className="swt:container swt:mx-auto swt:flex swt:flex-col swt:p-2 swt:gap-4 swt:mb-12">
            <h1 className='swt:text-6xl'>Playground</h1>
            <TermSearchContainer />
            <ContextMenuContainer />
            <AnnoTableContainer />
            <TableContainer />
            {/* <Menu></Menu> */}
        </div>
    );
};

export default App;
