import React, { Fragment } from 'react';
import TermSearch from '../src/TermSearch/TermSearch.fs.ts';
import {Entry as Table} from '../src/Table/Table.fs.ts';
import {Entry as AnnotationTable} from '../src/AnnotationTable/AnnotationTable.fs.ts';
import AnnotationTableCtxProvider from '../src/AnnotationTable/AnnotationTableContextProvider.fs.ts';
import {Example as ContextMenuExample, ContextMenu} from '../src/GenericComponents/ContextMenu.fs.ts';
import {TIBApi} from '../src/Api/TIBApi.fs.ts';
import {Entry as TemplateFilter} from '../src/Template/TemplateFilter.fs.ts';
import {Entry as ComboBox} from '../src/GenericComponents/ComboBox.fs.ts';
import {Entry as Select} from '../src/GenericComponents/Select.fs.ts';
import {Entry as BaseModal} from '../src/GenericComponents/BaseModal.fs.ts';
import { Wizard as LandingWizard } from '../src/Landing/Landing.fs.ts';
import { Exports_createLandingDraft as createLandingDraft, Exports_createLandingUiState as createLandingUiState } from '../src/Landing/Types.fs.ts';
import {TIBQueryProvider as TermSearchConfigProvider} from '../src/TermSearch/TermSearchConfigProvider.fs.ts';
import {Entry as TermSearchConfigSetter} from '../src/TermSearch/TermSearchConfigSetter.fs.ts';
import { Term } from '../../Shared/Database.fs.ts';
import {Entry as DataMapTable} from '../src/DataMapTable/DataMapTable.fs.ts';
import {Entry as Layout} from '../src/Layout/Layout.fs.js'
import {FileExplorerExample_Example} from '../src/FileExplorer/FileExplorer.fs.ts'
import {Entry as WidgetController} from '../src/Widgets/Widgets.fs.ts';
import {Entry as NoteSearch} from '../src/Notes/NoteSearch/NoteSearchComponent.fs.ts'
import {Entry as TextInputWithMarkdown} from '../src/MarkdownText/TextInputWithMarkdown.fs.ts';
import {Entry as AuthButton} from '../src/Authentication/Authentication.fs.ts';
// import {Entry as DataHubSidebarEntry} from '../src/DataHub/DataHubSidebar.fs.ts';
import {GitLabEntry as DataHubBrowser} from '../src/DataHub/DataHubBrowser.fs.ts';
import {Entry as ARCSelectorEntry} from '../src/ARCSelector/Selector.fs.ts';
import {Entry as ArcFileEditor} from '../src/ArcFileEditor/ArcFileEditor.fs.ts';

function TermSearchContainer() {
  const [term, setTerm] = React.useState(undefined);
  const [term2, setTerm2] = React.useState(undefined);
  const [term3, setTerm3] = React.useState(undefined);
  const [counter, setCounter] = React.useState(0)
  return <Fragment>
    <h2 className='swt:text-3xl'>TermSearch</h2>
    {/* <TermSearch
      onTermSelect={(term) => setTerm(term as Term | undefined)}
      term={term}
      showDetails
      debug={true}
    /> */}
    <div className='swt:max-w-2xl swt:flex swt:flex-col swt:gap-4'>
      <div className='swt:flex swt:flex-col'>
        <label className='swt:text-gray-200'>
          TIB Search
        </label>
        <TermSearch
          onTermChange={(term) => setTerm2(term)}
          term={term2}
          disableDefaultSearch
          disableDefaultParentSearch
          disableDefaultAllChildrenSearch
          termSearchQueries={[
            ["tib_search", (query) => TIBApi.defaultSearch(query, 10, "DataPLANT")]
          ]}
          parentSearchQueries={[
            ["tib_search", ([parentId, query]) => TIBApi.searchChildrenOf(query, parentId, 10, "DataPLANT")]
          ]}
          allChildrenSearchQueries={[
            ["tib_search", (parentId) => TIBApi.searchAllChildrenOf(parentId, 500, "DataPLANT")]
          ]}
          onBlur={() => console.log("TermSearch blurred")}
        />
      </div>
      <div className='swt:flex swt:flex-col'>
        <label className='swt:text-gray-200'>
          Term Search
        </label>
        <TermSearch
          onTermChange={(term) => setTerm(term as Term | undefined)}
          term={term}
          parentId="MS:1000031"
          debug={true}
          onBlur={() => console.log("TermSearch blurred")}
        />
      </div>
      <div className='swt:flex swt:flex-col swt:border swt:p-2'>
        <label className='swt:text-gray-200'>
          Term Search with Provider
        </label>
        <TermSearchConfigProvider>
          <>
            <TermSearchConfigSetter />
            <TermSearch
              onTermChange={(term) => setTerm3(term as Term | undefined)}
              term={term3}
            />
          </>
        </TermSearchConfigProvider>
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
    <AnnotationTableCtxProvider>
      <AnnotationTable />
    </AnnotationTableCtxProvider>
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

function SelectContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Select</h2>
    <Select />
  </div>
}

function BaseModalContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Base Modal</h2>
    <BaseModal />
  </div>
}

function DataMapTableContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-3xl'>Data Map Table</h2>
    <DataMapTable />
  </div>
}



function FileExplorerContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>File Explorer</h2>
    <FileExplorerExample_Example />
  </div>
}

function WidgetControllerContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>Widget Controller</h2>
    <WidgetController />
  </div>
}

function LandingContainer() {
  const [draft, setDraft] = React.useState(createLandingDraft());
  const [uiState, setUiState] = React.useState(createLandingUiState());

  return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>Landing</h2>
    <LandingWizard
      draft={draft}
      setDraft={setDraft}
      uiState={uiState}
      setUiState={setUiState}
      onSubmit={(payload) => console.log('Landing submit payload', payload)}
    />
  </div>
}

function MarkdownTextContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>Markdown Editor</h2>
    <TextInputWithMarkdown />
  </div>
}

function AuthButtonContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>Authentication Button</h2>
    <AuthButton />
  </div>
}

// function DataHubSidebarContainer() {
//   return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
//     <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>DataHub Sidebar</h2>
//     <DataHubSidebarEntry />
//   </div>
// }

function DataHubBrowserContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>DataHub Browser</h2>
    <DataHubBrowser />
  </div>
}

function ARCSelectorContainer() {
  return <div className='swt:flex swt:flex-col swt:gap-4 swt:w-full'>
    <h2 className='swt:text-5xl swt:font-bold swt:mb-4'>ARC Selector</h2>
    <ARCSelectorEntry />
  </div>
}

const App = () => {
    return (
        <ArcFileEditor />
        // <AuthButtonContainer />
        // <div className="swt:container swt:mx-auto swt:flex swt:flex-col swt:p-2 swt:gap-8 swt:mb-12">
        //     <NoteSearch />
        //     <MarkdownTextContainer />
        //     <LandingContainer />
        // </div>
    );
};

export default App;
