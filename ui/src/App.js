import React, { Component } from 'react';
import PlexOAuth from './plex';
import './App.css';

import LogoImg from './img/logo.png';

class App extends Component {
    state = {
        remember: false,
        loggedIn: false,
        failure: false
    };

    controller = null;

    async componentWillMount() {
        this.controller = new PlexOAuth(this.loginSuccess.bind(this), this.loginFailure.bind(this));
    }

    shouldRedirect() {
        return window.location.pathname !== '/' && !!window.location.pathname;
    }

    async login() {
        this.setState({
            failure: false
        });
        this.controller.performLogin(this.state.remember);
    }

    loginSuccess() {
        this.setState({
            loggedIn: true
        });
        if (this.shouldRedirect()) {
            setTimeout(() => {
                window.location = '/redirect' + window.location.pathname + window.location.search;
            }, 1000);
        }
    }

    loginFailure() {
        this.setState({
            failure: true
        });
    }

    toggleRemember() {
        this.setState({
            remember: !this.state.remember
        });
    }

    async logout() {
        this.controller.performLogout();
        this.setState({
            loggedIn: false
        });
    }

    render() {
        return (
            <div className="App">
                <div className="card login-card">
                    {this.state.loggedIn ? this.renderLogout() : this.renderLogin()}
                </div>
            </div>
        );
    }

    renderLogout() {
        return (
            <form id="logoutForm" onSubmit={e => e.preventDefault() && this.logout()} style={{textAlign: 'center'}}>
                <img className="card-img-top login-logo" src={LogoImg} alt="Logo" />
                <br />
                {
                    this.shouldRedirect() ? (
                        <a href={'/redirect' + window.location.pathname + window.location.search}
                           className="login-forgotten-link">
                           Click here if you are not automatically redirected.
                           <br />
                           <br />
                        </a>) : (void(0))
                }
                <button type="submit" className="btn btn-danger" onClick={() => this.logout()} autoFocus>Logout</button>
            </form>
        );
    }

    renderLogin() {
        return (
            <div className="login-form">
                <img className="card-img-top login-logo" src={LogoImg} alt="Logo" />
                <div className="alert alert-danger" role="alert" style={{display: this.state.failure ? 'block' : 'none'}}>
                Login was not successful.<br/>
                Please verify your access and try again.
                </div>
                
                <button type="submit"
                    className="btn btn-lg btn-outline-warning"
                    onClick={() => this.login()}
                    onFocus={e => e.target.blur()}>
                    Sign in with&nbsp;
                    <span className='plex-inline' />
                </button>
                <br />
                <label className={`login-remember-me-checkbox-${this.state.remember ? '' : 'un'}checked login-remember-me-checkbox`}
                    onClick={() => this.toggleRemember()}>Remember Me</label>
            </div>
        );
    }
}

export default App;
