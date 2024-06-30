import React, { Component } from 'react';
import LoginPage from './pages/login-page';
import LoadingPlaceholder from './common/components/loading-placeholder';
import './App.css';

const asyncComponent = importComponent => {
    class AsyncComponent extends Component {
        constructor(props) {
            super(props);
            this.state = {
                component: null
            };
        }

        async componentDidMount() {
            const { default: component } = await importComponent();
            this.setState({
                component: component
            });
        }

        render() {
            const C = this.state.component;
            return C ? <C {...this.props} /> : <LoadingPlaceholder />;
        }
    }
    return AsyncComponent;
};

let App;
if (window.location.pathname.startsWith('/sso/403')) {
    App = asyncComponent(() => import('./pages/access-denied-page'));
}
else if (window.location.pathname.startsWith('/sso/admin')) {
    App = asyncComponent(() => import('./pages/sso-admin-page'));
}
else {
    App = LoginPage;
}

export default App;
