from plex import get_local_server_identifier
from server import start_server
from argparse import ArgumentParser
from sys import argv

parser = ArgumentParser(description='Single Sign-On using Plex.')
action = parser.add_mutually_exclusive_group(required=True)
action.add_argument('-s', '--server', nargs=1, help='The unique identifier of the server')
action.add_argument('-p', '--preferences', nargs=1, help='Preferences.xml of the Plex server')

if __name__ == '__main__':
    args = parser.parse_args(argv[1::])
    server_identifier = None
    if hasattr(args, 'server'):
        server_identifier = args.server[0]
    else:
        server_identifier = get_local_server_identifier(args.preferences[0])
    start_server(server_identifier)
