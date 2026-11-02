"""Loopback test proxy. Drop the first attendance response AFTER its commit.
Run against a disposable test server; never use as a public gateway.
"""
import http.server
import json
import socket
import threading
import urllib.request
import urllib.error

class Handler(http.server.BaseHTTPRequestHandler):
    dropped = False
    lock = threading.Lock()
    def log_message(self, *_):
        pass
    def forward(self):
        size = int(self.headers.get('Content-Length', 0))
        if size > 2 * 1024 * 1024:
            self.send_error(413)
            return
        body = self.rfile.read(size) if size else None
        headers = {'Content-Type': 'application/json'}
        if self.headers.get('Authorization'):
            headers['Authorization'] = self.headers['Authorization']
        req = urllib.request.Request('http://127.0.0.1:5187' + self.path, data=body, headers=headers, method=self.command)
        try:
            response = urllib.request.urlopen(req, timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            data, status = response.read(), response.status
        should_drop = False
        if body and self.path == '/v1/commands' and status == 200:
            with self.lock:
                if not Handler.dropped and json.loads(body).get('type') == 'attendance':
                    Handler.dropped = True
                    should_drop = True
        if should_drop:
            print('DROPPED_COMMITTED_ATTENDANCE_RESPONSE', flush=True)
            self.connection.shutdown(socket.SHUT_RDWR)
            self.connection.close()
            return
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(data)))
        self.end_headers()
        self.wfile.write(data)
    do_GET = forward
    do_POST = forward
    do_DELETE = forward

if __name__ == '__main__':
    http.server.ThreadingHTTPServer(('127.0.0.1', 5188), Handler).serve_forever()
