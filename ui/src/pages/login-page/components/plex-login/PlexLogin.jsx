import React, { Component } from 'react';
import PlexOAuth from '../../../../common/PlexOAuth';
import { LoginForm, LogoutForm } from '../login-logout-form';
import LoadingPlaceholder from '../../../../common/components/loading-placeholder';
import AccessDeniedPage from '../../../access-denied-page';

class PlexLogin extends Component {
    state = {
        loggedInStatus: null,
        failedLogin: false,
        tier: ''
    };

    async componentDidMount() {
        const pathParams = this.getRedirectParams() || {
            service: '',
            path: '/'
        };
        const loggedInState = await PlexOAuth.isLoggedIn(pathParams.service, pathParams.path);
        this.setState({
            loggedInStatus: this.getLoggedInStatus(loggedInState),
            failedLogin: loggedInState.success === false,
            tier: loggedInState.tier,
            message: loggedInState.message
        });
    }

    async login(rememberMe) {
        this.setState({
            loggedInStatus: 'transition'
        });

        const pathParams = this.getRedirectParams() || {
            service: '',
            path: '/'
        };
        const loginResult = await PlexOAuth.login(rememberMe, pathParams.service, pathParams.path);
        this.setState({
            loggedInStatus: this.getLoggedInStatus(loginResult),
            failedLogin: loginResult.loggedIn === false || loginResult.success === false,
            tier: loginResult.tier,
            message: loginResult.message
        });
    }

    getLoggedInStatus(loggedInState) {
        if (loggedInState.accessBlocked) {
            if (loggedInState.status === 400) {
                return 'error';
            }
            return 'blocked';
        }
        return loggedInState.loggedIn;
    }

    async logout() {
        await PlexOAuth.logout();
        this.setState({
            loggedInStatus: false,
            failedLogin: false,
            tier: '',
            message: null
        });
    }

    getRedirectParams() {
        // Check for returnUrl in query params (used by OIDC)
        const urlParams = new URLSearchParams(window.location.search);
        const returnUrl = urlParams.get('returnUrl');
        if (returnUrl) {
            return {
                service: 'oidc',
                path: '/',
                redirectPath: decodeURIComponent(returnUrl)
            };
        }

        if (window.location.pathname !== '/' &&
            !!window.location.pathname &&
            !window.location.pathname.startsWith('/redirect/') &&
            !window.location.pathname.startsWith('/sso/')) {

            const location = window.location.pathname;
            const pathSplit = location.split('/');
            const service = pathSplit[1];
            const path = pathSplit.length < 2 ? '/' : location.substring(service.length + 1) + window.location.search + window.location.hash;

            return {
                service,
                path,
                redirectPath: `/redirect/${service}${path}`
            };
        }
        return null;
    }

    checkRedirect() {
        const result = this.getRedirectParams();
        if (result && this.state.loggedInStatus !== 'blocked') {
            if ('requestIdleCallback' in window) {
                requestIdleCallback(() => {
                    window.location = result.redirectPath;
                }, {
                    timeout: 1000
                });
            } else {
                setTimeout(() => {
                    window.location = result.redirectPath;
                }, 1000);
            }
            return result.redirectPath;
        }
        return null;
    }

    render() {
        switch (this.state.loggedInStatus) {
            case 'blocked': return (<AccessDeniedPage message={this.state.message} />);
            case 'transition': return (<LoadingPlaceholder />);
            case 'error':
                return (<>
                    <AccessDeniedPage message='An error occurred with your Plex account.<br/>Try logging out then in again.' />
                    <LogoutForm logout={this.logout.bind(this)} tier={void (0)} redirectTo={void (0)} />
                </>);
            case true: return (<LogoutForm logout={this.logout.bind(this)} tier={this.state.tier} redirectTo={this.checkRedirect()} />);
            case false: return (<LoginForm login={this.login.bind(this)} failure={this.state.failedLogin} />);
            default: return (<LoadingPlaceholder />);
        }
    }
}

export default PlexLogin;
