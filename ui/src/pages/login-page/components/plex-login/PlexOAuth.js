const sleep = async(timeout) => new Promise(resolve => window.setTimeout(resolve, timeout));

const PlexHeaders = {
    'Accept': 'application/json',
    'X-Plex-Product': 'PlexSSO',
    'X-Plex-Version': 'Plex OAuth',
    'X-Plex-Client-Identifier': 'PlexSSOv2'
};

class PlexOAuth {
    constructor() {
        this._width = window.innerWidth || document.documentElement.clientWidth || window.screen.width;
        this._height = window.innerHeight || document.documentElement.clientHeight || window.screen.height;
        this._left = ((this._width / 2) - 300) + (window.screenLeft || window.screenX);
        this._top = ((this._height / 2) - 350) + (window.screenTop || window.screenY);
        this._window = null;
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
        if (this._window) {
            this._window.location = url;
        }
    }

    async _getPlexToken() {
        try {
            this._show();

            const {pin, code} = await this._getPlexOAuthPin();
            this._goTo(`https://app.plex.tv/auth/#!?clientID=${PlexHeaders['X-Plex-Client-Identifier']}&code=${code}`);
            
            let token = null;
            while(true) {
                const response = await fetch(`https://plex.tv/api/v2/pins/${pin}`, {
                    headers: PlexHeaders
                });

                const jsonData = await response.json();
                if (jsonData.authToken || this._window.closed) {
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
            console.error(e);
            return null;
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
        const response = await fetch('/api/v2/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                token: token,
            })
        });
        return response.status < 400;
    }

    async login(rememberMe, existingToken = null) {
        const token = existingToken || await this._getPlexToken();
        if (!token || !await this._verifyToken(token)) {
            return false;
        }
        if (rememberMe) {
            localStorage.setItem('plex_token', token);
        }
        return true;
    }

    async logout() {
        await fetch('/api/v2/logout');
        localStorage.removeItem('plex_token');
    }

    async isLoggedIn() {
        const response = await fetch('/api/v2/sso');
        const json = await response.json();
        if (json.success) {
            return true;
        }

        // remember me
        const storedToken = localStorage.getItem('plex_token');
        if (!!storedToken) {
            return await this.login(true, storedToken);
        }

        return false;
    }
}

export default PlexOAuth;
