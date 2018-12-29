const sleep = async(timeout) => new Promise(resolve => window.setTimeout(resolve, timeout));

const PlexHeaders = {
    'Accept': 'application/json',
    'X-Plex-Product': 'PlexSSO',
    'X-Plex-Version': 'Plex OAuth',
    'X-Plex-Client-Identifier': 'PlexSSOv2'
};

class PlexOAuth {
    constructor(onSuccess, onFailure) {
        this._width = window.innerWidth || document.documentElement.clientWidth || window.screen.width;
        this._height = window.innerHeight || document.documentElement.clientHeight || window.screen.height;
        this._left = ((this._width / 2) - 300) + (window.screenLeft || window.screenX);
        this._top = ((this._height / 2) - 350) + (window.screenTop || window.screenY);

        this._onSuccess = onSuccess;
        this._onFailure = onFailure;

        this._window = null;

        this._checkRememberedLogin();
    }

    async _signIn() {
        try {
            this._show();

            const {pin, code} = await this._getPlexOAuthPin();
            this._goTo(`https://app.plex.tv/auth/#!?clientID=${PlexHeaders['X-Plex-Client-Identifier']}&code=${code}`);
            
            let token;
            while(true) {
                const response = await fetch(`https://plex.tv/api/v2/pins/${pin}`, {
                    headers: PlexHeaders
                });

                const jsonData = await response.json();
                if (jsonData.authToken) {
                    token = jsonData.authToken;
                    break;
                }
                await sleep(1000);
            }
            this._hide();
            return token;
        }
        catch(e) {
            this._hide();
            throw e;
        }
    }

    async _getPlexOAuthPin() {
        const response = await fetch('https://plex.tv/api/v2/pins?strong=true', {
            method: 'POST',
            headers: PlexHeaders
        });
        const jsonData = await response.json();
        return {
            pin: jsonData.id,
            code: jsonData.code
        };
    }

    async _verifyToken(token) {
        await fetch('/api/v2/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                token: token,
            })
        });
    }

    async _checkRememberedLogin() {
        const token = localStorage.getItem('plex_token');
        if (token) {
            try {
                await this._verifyToken(token);
                this._onSuccess(token);
            }
            catch {
                // we don't care, the user will need to login again
            }
        }
        else if (await this.checkLoginStatus()) {
            this._onSuccess(null);
        }
    }

    async performLogin(rememberMe) {
        try {
            const token = await this._signIn();
            await this._verifyToken(token);
            if (rememberMe) {
                localStorage.setItem('plex_token', token);
            }
            this._onSuccess(token);
        }
        catch(e) {
            this._onFailure(e);
        }
    }

    async performLogout() {
        await fetch('/api/v2/logout');
        localStorage.removeItem('plex_token');
    }

    async checkLoginStatus() {
        const response = await fetch('/api/v2/sso');
        const json = await response.json();
        return !!json.success;
    }

    _show() {
        this._window = window.open('', 'PlexSSO', `scrollbars=yes, width=${600}, height=${700}, top=${this._top}, left=${this._left}`);
        if (window.focus) {
            this._window.focus();
        }
    }

    _hide() {
        if (this._window) {
            this._window.close();
        }
    }

    _goTo(url) {
        this._window.location = url;
    }
}

export default PlexOAuth;
