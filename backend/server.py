from plex import get_access_privileges, AccessLevel
from bottle import run, post, get, request, response, route, static_file, error, debug
from cachetools import TTLCache
from uuid import uuid4
from json import loads, dumps
from traceback import print_exc
from re import compile
from os.path import splitext, basename, abspath, realpath, join, isfile
from urllib.parse import urlparse

ip_address_re = compile(r'^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$')

server_id = None
auth_token_cache = TTLCache(maxsize=128, ttl=3600)
cookie_sig_key = uuid4().urn
ui_path = abspath('./ui')

def get_host_info():
    host_header = request.get_header('x-upstream-host', request.get_header('host'))
    protocol = request.get_header('x-upstream-protocol')
    host_protocol = protocol if protocol is not None else 'http'
    if ip_address_re.match(host_header):
        return {
            'host': host_header,
            'subdomain': '',
            'domain': host_header,
            'protocol': host_protocol
        }
    else:
        return {
            'host': host_header,
            'subdomain': host_header[:min(host_header.find('.'), len(host_header)):],
            'domain': host_header[max(host_header.find('.') + 1, 0)::],
            'protocol': host_protocol
        }

def get_data_from_cookie():
    token = request.get_cookie('kPlexSSOKookieV2', secret=cookie_sig_key)
    return (token, auth_token_cache.get(token))

@get('/api/v2/healthcheck')
def healthcheck():
    response.set_header('content-type', 'application/json')
    return dumps({
        'healthy': True
    })

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
            host_info = get_host_info()
            sso_token = uuid4().urn
            auth_token_cache[sso_token] = access_privileges
            response.set_cookie('kPlexSSOKookieV2', sso_token, secret=cookie_sig_key, domain=host_info['domain'], max_age=3600, path='/')
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
        host_info = get_host_info()
        response.delete_cookie('kPlexSSOKookieV2', domain=host_info['domain'], path='/')
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
            host_info = get_host_info()
            response.set_cookie('kPlexSSOKookieV2', token, secret=cookie_sig_key, domain=host_info['domain'], max_age=3600, path='/')
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

@route('/redirect/<service_name>')
@route('/redirect/<service_name>/')
@route('/redirect/<service_name>/<path_params:re:.*>')
def redirect_to_service(service_name, path_params = ''):
    (token, level) = get_data_from_cookie()
    host_info = get_host_info()
    if level is None:
        response.set_header('location', '%s://%s/%s/%s' % (host_info['protocol'], host_info['host'], service_name, path_params))
    else:
        response.set_header('location', '%s://%s.%s/%s' % (host_info['protocol'], service_name, host_info['domain'], path_params))
    response.status = 302
    response.set_header('content-type', 'application/json')
    return dumps({
        'success': level is not None
    })

@error(404)
def serve_static_files(code):
    file_path = request.path
    _, extension = splitext(basename(urlparse(file_path).path))
    if extension is None or extension == '':
        file_path += ('/' if len(file_path) > 0 and file_path[-1] != '/' else '') + 'index.html'
        
    approx_file_path = realpath(join(ui_path, '.' + file_path))
    if not approx_file_path.startswith(ui_path):
        response.status = 404
        return ''

    if not isfile(approx_file_path):
        if file_path.count('/') <= 1:
            response.status = 404
            return ''
        file_path = file_path[file_path.index('/', 1)::]
        approx_file_path = realpath(join(ui_path, '.' + file_path))
        if not approx_file_path.startswith(ui_path) or not isfile(approx_file_path):
            response.status = 404
            return ''

    try:
        return static_file(file_path, ui_path)
    except:
        response.status = 404
        return ''

def start_server(server_identifier):
    global server_id
    server_id = server_identifier
    run(host='0.0.0.0', port=4200)
