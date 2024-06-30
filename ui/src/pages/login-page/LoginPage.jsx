import React from 'react';
import PlexLogin from './components/plex-login';
import MarvinHeader from './components/marvin-header';
import BackgroundImage from '../../img/background.jpg';
import { preloadImage } from '../../common/utils';
import './LoginPage.css';

preloadImage(BackgroundImage);

const LoginPage = () => (
    <div className="login-page">
        <div className="card login-card">
            <MarvinHeader />
            <PlexLogin />
        </div>
    </div>
);

export default LoginPage;
