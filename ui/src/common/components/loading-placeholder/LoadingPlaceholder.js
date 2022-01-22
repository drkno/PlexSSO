import React from 'react';
import './LoadingPlaceholder.css';

const LoadingPlaceholder = () => (
    <div className="spinner-border text-warning loading-placeholder" role="status">
        <span className="visually-hidden">Loading...</span>
    </div>
);

export default LoadingPlaceholder;
