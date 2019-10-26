import React from 'react';
import LoginRedirectLink from '../login-redirect-link';

const LogoutForm = ({ logout, redirectTo }) => {
    return (
        <form id="logoutForm" onSubmit={e => e.preventDefault() && logout()}>
            <br />
            {
                !!redirectTo ? (<LoginRedirectLink redirectTo={redirectTo} />) : (void(0))
            }
            <button type="submit" className="btn btn-danger" onClick={logout} autoFocus>Logout</button>
        </form>
    );
};

export default LogoutForm;
