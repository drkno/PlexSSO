import React, { Component } from 'react';
import PlexOAuth from './PlexOAuth';
import { LoginForm, LogoutForm } from '../login-logout-form';
import LoadingPlaceholder from '../loading-placeholder';

const plexOAuthProvider = new PlexOAuth();

class PlexLogin extends Component {
    state = {
        loggedInStatus: null,
        failedLogin: false
    };

    async componentDidMount() {
        this.setState({
            loggedInStatus: await plexOAuthProvider.isLoggedIn()
        });
    }

    async login(rememberMe) {
        this.setState({
            loggedInStatus: 'transition'
        });
        const loginResult = await plexOAuthProvider.login(rememberMe);
        this.setState({
            loggedInStatus: loginResult,
            failedLogin: !loginResult
        });
    }

    async logout() {
        await plexOAuthProvider.logout();
        this.setState({
            loggedInStatus: false,
            failedLogin: false
        });
    }

    checkRedirect() {
        const result = window.location.pathname !== '/' && !!window.location.pathname && !window.location.pathname.includes('/redirect/');
        if (result) {
            const redirectUrl = '/redirect' + window.location.pathname + window.location.search;
            window.requestIdleCallback(() => {
                window.location = redirectUrl;
            }, {
                timeout: 1000
            });
            return redirectUrl;
        }
        return null;
    }

    render() {
        switch (this.state.loggedInStatus) {
            case 'transition': return (<LoadingPlaceholder />);
            case true: return (<LogoutForm logout={this.logout.bind(this)} redirectTo={this.checkRedirect()} />);
            case false: return (<LoginForm login={this.login.bind(this)} failure={this.state.failedLogin} />);
            default: return (<LoadingPlaceholder />);
        }
    }
}

export default PlexLogin;
