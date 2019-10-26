import React from 'react';
import './LoadingPlaceholder.css';

const LoadingPlaceholder = () => (
    <div className="spinner-border text-warning loading-placeholder" role="status">
        <span className="sr-only">Loading...</span>
    </div>
);

export default LoadingPlaceholder;
