import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './../tailwind.css';
import '@fortawesome/fontawesome-free/css/all.min.css'

ReactDOM.createRoot(document.getElementById('app')!).render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);
