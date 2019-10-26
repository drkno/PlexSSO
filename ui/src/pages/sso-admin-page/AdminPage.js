import React, { useState } from 'react';
import PlexOAuth from '../../common/PlexOAuth';
import LoginPage from '../login-page';
import './AdminPage.css';

const AdminPage = () => (
    <div>Placeholder</div>
);

const AdminPageAuthChecker = () => {
    const [loggedIn, updateLoggedIn] = useState(false);
    PlexOAuth.on('loggedInStatus', updateLoggedIn);
    return loggedIn ? (<AdminPage />) : (<LoginPage />);
};

export default AdminPageAuthChecker;
