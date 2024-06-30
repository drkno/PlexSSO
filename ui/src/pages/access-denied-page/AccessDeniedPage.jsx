import React from 'react';
import Marvin from '../../img/marvin.png';
import { preloadImage } from '../../common/utils';
import './AccessDeniedPage.css';

preloadImage(Marvin);

const AccessDeniedPage = ({ message }) => (
    <div className="access-denied">
        <div className="access-denied-center">
            <img className="access-denied-item" src={Marvin} alt="Logo" />
            <br />
            <p className="access-denied-item" dangerouslySetInnerHTML={{ __html: message || 'Access Denied'}}></p>
        </div>
    </div>  
);

export default AccessDeniedPage;
