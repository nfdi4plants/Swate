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

function TableContainer() {
  return <Fragment>
    <h2 className='text-3xl'>Table</h2>
    <div className='max-h-[600px] overflow-y-auto border'>
      <Table />
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
