import React from 'react';
import TermSearch from '../src/TermSearch/TermSearchV2.fs.ts';

const App = () => {
    return (
        <div className="container mx-auto flex flex-col p-2 gap-4">
            <h1 className='text-6xl'>Playground</h1>
            <h2 className='text-3xl'>TermSearch</h2>
            <TermSearch
              onTermSelect={(term) => console.log(term)}
              term={undefined}
              parentId="test:xx"
              showDetails={true}
              debug={true}
            />
        </div>
    );
};

export default App;
