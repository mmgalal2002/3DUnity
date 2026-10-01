"""Serve this Unity distribution on localhost with correct module/Wasm MIME types."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

class Handler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

    extensions_map = {**SimpleHTTPRequestHandler.extensions_map,
                      '.mjs': 'application/javascript', '.js': 'application/javascript',
                      '.wasm': 'application/wasm', '.data': 'application/octet-stream'}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8080)
    parser.add_argument('--directory', default=str(Path(__file__).resolve().parent / 'WebGL'))
    args = parser.parse_args()
    folder = Path(args.directory).resolve(strict=True)
    server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(Handler, directory=str(folder)))
    print(f'Room Studio: http://127.0.0.1:{args.port}/', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.server_close()
