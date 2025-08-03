import React, { Fragment, useEffect } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import {Entry as Table} from '../src/Table/Table.fs.ts';
import {Entry as AnnotationTable} from '../src/AnnotationTable/AnnotationTable.fs.ts';
import {Example as ContextMenuExample, ContextMenu} from '../src/GenericComponents/ContextMenu.fs.ts';
import {TIBApi} from '../src/Util/Api.fs.ts';
import {Entry as TemplateFilter} from '../src/Template/TemplateFilter.fs.ts';
import {Entry as ComboBox} from '../src/GenericComponents/ComboBox.fs.ts';

function TermSearchContainer() {
  const [term, setTerm] = React.useState(undefined);
  const [term2, setTerm2] = React.useState(undefined);
  return <Fragment>
    <h2 className='swt:text-3xl'>TermSearch</h2>
    {/* <TermSearch
      onTermSelect={(term) => setTerm(term as Term | undefined)}
      term={term}
      showDetails
      debug={true}
    /> */}
    <div className='swt:max-w-2xl swt:flex swt:flex-col swt:gap-4'>
      <div>
        <label className='swt:text-gray-200'>
          TIB Search
        </label>
        <TermSearch
          onTermSelect={(term) => setTerm2(term as Term | undefined)}
          term={term2}
          disableDefaultSearch
          disableDefaultParentSearch
          disableDefaultAllChildrenSearch
          showDetails
          advancedSearch
          termSearchQueries={[
            ["tib_search", (query) => TIBApi.defaultSearch(query, 10, "DataPLANT")]
          ]}
          parentSearchQueries={[
            ["tib_search", ([parentId, query]) => TIBApi.searchChildrenOf(query, parentId, 10, "DataPLANT")]
          ]}
          allChildrenSearchQueries={[
            ["tib_search", (parentId) => TIBApi.searchAllChildrenOf(parentId, 500, "DataPLANT")]
          ]}
        />
      </div>
      <div>
        <label className='swt:text-gray-200'>
          Term Search
        </label>
        <TermSearch
          onTermSelect={(term) => setTerm(term as Term | undefined)}
          term={term}
          parentId="MS:1000031"
          showDetails
          debug={true}
          advancedSearch
        />
      </div>
    </div>
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

function TemplateFilterContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Template Filter</h2>
    <TemplateFilter />
  </div>
}

function ComboBoxContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Combo Box</h2>
    <ComboBox />
  </div>
}

const App = () => {
    return (
        <div className="swt:container swt:mx-auto swt:flex swt:flex-col swt:p-2 swt:gap-4 swt:mb-12">
            <h1 className='swt:text-6xl'>Playground</h1>
            <TemplateFilterContainer />
            <ComboBoxContainer />
            <TermSearchContainer />
            <ContextMenuContainer />
            <AnnoTableContainer />
            <TableContainer />
            {/* <Menu></Menu> */}
        </div>
    );
};

export default App;
