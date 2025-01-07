// MyComponent.tsx
import React, { useState } from 'react';

const MyComponent = () => {
  const [value, setValue] = useState('');
  const [clicked, setClicked] = useState(false);

  return (
    <div>
      <input
        data-testid="input-field"
        value={value}
        onChange={(e) => setValue(e.target.value)}
      />
      <button
        data-testid="button"
        className='bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 w-[300px]'
        onClick={() => setClicked(true)}
      >
        Click me
      </button>
      {clicked && <span data-testid="message">Button clicked!</span>}
    </div>
  );
};

export default MyComponent;
