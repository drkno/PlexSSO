import React from 'react';
import { preloadImage } from '../../../../common/utils';
import MarvinImg from '../../../../img/marvin-eyes.png';
import './MarvinHeader.css';

preloadImage(MarvinImg);

const MarvinHeader = () => (
    <img className="card-img-top marvin-header" src={MarvinImg} alt="Logo" />
);

export default MarvinHeader;
