from plex import get_access_privileges, get_local_server_identifier, AccessLevel
from bottle import run, post, get, request, response
from cachetools import TTLCache
from sys import argv
from uuid import uuid4
from json import loads, dumps
from traceback import print_exc

domain = None
server_id = None
auth_token_cache = TTLCache(maxsize=128, ttl=3600)
cookie_sig_key = uuid4().urn

def get_data_from_cookie():
    token = request.get_cookie('kPlexSSOKookieV2', secret=cookie_sig_key)
    return (token, auth_token_cache.get(token))

@post('/api/v2/login')
def login():
    try:
        (token, access_privileges) = get_data_from_cookie()
        if access_privileges is None:
            body = request.body.read()
            jsonData = loads(body)
            token = jsonData.get('token')
            response.set_header('content-type', 'application/json')
            access_privileges = get_access_privileges(server_id, token)
        if access_privileges is None or access_privileges == AccessLevel.NoAccess:
            response.status = 403
            return dumps({
                'success': False
            })
        else:
            sso_token = uuid4().urn
            auth_token_cache[sso_token] = access_privileges
            response.set_cookie('kPlexSSOKookieV2', sso_token, secret=cookie_sig_key, domain=domain, max_age=3600, path='/')
            return dumps({
                'success': True
            })
    except Exception:
        print_exc()
        response.status = 400
        return dumps({
            'success': False
        })

@get('/api/v2/logout')
def logout():
    try:
        (token, level) = get_data_from_cookie()
        if level is not None:
            del auth_token_cache[token]
        response.delete_cookie('kPlexSSOKookieV2', domain=domain, path='/')
        response.set_header('content-type', 'application/json')
        return dumps({
            'success': True
        })
    except Exception:
        print_exc()
        response.status = 400
        return dumps({
            'success': False
        })

@get('/api/v2/sso')
def sso():
    try:
        (token, level) = get_data_from_cookie()
        response.set_header('content-type', 'application/json')
        if level is None:
            response.status = 403
            return dumps({
                'success': False
            })
        else:
            auth_token_cache[token] = level
            response.set_cookie('kPlexSSOKookieV2', token, secret=cookie_sig_key, domain=domain, max_age=3600, path='/')
            response.status = 200
            return dumps({
                'success': True
            })
    except Exception:
        print_exc()
        response.status = 400
        return dumps({
            'success': False
        })

if __name__ == '__main__':
    domain = argv[1]
    server_id = get_local_server_identifier(argv[2] if len(argv) == 3 else 'Preferences.xml')
    run(host='127.0.0.1', port=4200)
