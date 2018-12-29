import urllib.request
import urllib.parse
import xml.etree.ElementTree as ET
import json
from enum import Enum

class AccessLevel(Enum):
    Owner = 1
    HomeUser = 2
    NormalUser = 4
    NoAccess = 8

def get_servers(token):
    headers = {
        'includeHttps': '1',
        'includeRelay': '1',
        'X-Plex-Product': 'PlexSSO',
        'X-Plex-Version': 'Plex OAuth',
        'X-Plex-Client-Identifier': 'PlexSSOv2',
        'X-Plex-Token': token
    }
    req = urllib.request.Request(
        'https://plex.tv/api/resources?' + urllib.parse.urlencode(headers), 
        data=None, 
        headers={
            'Accept': 'application/json'
        }
    )
    res = urllib.request.urlopen(req)    
    raw_data = res.read().decode('utf-8')
    return ET.fromstring(raw_data)

def get_server_details(xmlTree):
    return [(x.attrib['clientIdentifier'], AccessLevel.Owner if x.attrib.get('owned') == '1' else (AccessLevel.HomeUser if x.attrib.get('home') == '1' else AccessLevel.NormalUser)) for x in xmlTree.iter('Device')]

def get_local_server_identifier(path = 'Preferences.xml'):
    prefs = ET.parse(path)
    return prefs.getroot().attrib['ProcessedMachineIdentifier']

def get_access_privileges(server_id, token):
    try:
        servers = get_servers(token)
        server_details = get_server_details(servers)
        return next(accessLevel for (id, accessLevel) in server_details if id == server_id)
    except:
        return AccessLevel.NoAccess
