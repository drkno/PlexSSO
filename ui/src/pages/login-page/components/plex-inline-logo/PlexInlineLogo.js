import React from 'react';
import { preloadImage } from '../../../../utils';
import PlexLogoLight from '../../../../img/plex-light.svg';
import PlexLogoDark from '../../../../img/plex-dark.svg';
import './PlexInlineLogo.css';

preloadImage(PlexLogoLight);
preloadImage(PlexLogoDark);

const PlexInlineLogo = () => (
    <span className='plex-inline' />
);

export default PlexInlineLogo;
