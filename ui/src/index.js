import React from 'react';
import ReactDOM from 'react-dom';
import 'promise-polyfill';
import 'whatwg-fetch';
import './index.css';
import App from './App';
import registerServiceWorker from './registerServiceWorker';

ReactDOM.render(<App />, document.getElementById('root'));
registerServiceWorker();
