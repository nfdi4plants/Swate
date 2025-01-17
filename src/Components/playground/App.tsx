import React from 'react';
import TermSearch from '../src/TermSearch/TermSearchV2.fs.js';

const App = () => {
    const [term, setTerm] = React.useState(undefined);
    return (
        <div className="container mx-auto flex flex-col p-2 gap-4">
            <h1 className='text-6xl'>Playground</h1>
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
            />
        </div>
    );
};

export default App;
