import React, { Component } from 'react';
import PlexOAuth from '../../../../common/PlexOAuth';
import { LoginForm, LogoutForm } from '../login-logout-form';
import LoadingPlaceholder from '../../../../common/components/loading-placeholder';

class PlexLogin extends Component {
    state = {
        loggedInStatus: null,
        failedLogin: false
    };

    async componentDidMount() {
        this.setState({
            loggedInStatus: await PlexOAuth.isLoggedIn()
        });
    }

    async login(rememberMe) {
        this.setState({
            loggedInStatus: 'transition'
        });
        const loginResult = await PlexOAuth.login(rememberMe);
        this.setState({
            loggedInStatus: loginResult,
            failedLogin: !loginResult
        });
    }

    async logout() {
        await PlexOAuth.logout();
        this.setState({
            loggedInStatus: false,
            failedLogin: false
        });
    }

    checkRedirect() {
        const result = window.location.pathname !== '/' &&
            !!window.location.pathname &&
            !window.location.pathname.startsWith('/redirect/') &&
            !window.location.pathname.startsWith('/sso/');
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
