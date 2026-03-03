import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import '@uiw/react-markdown-preview/markdown.css';
import './../tailwind.css';


ReactDOM.createRoot(document.getElementById('app')!).render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);
