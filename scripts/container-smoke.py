import json
import re
import subprocess
import tempfile
import time
from pathlib import Path
root = Path(__file__).resolve().parents[1]

def run(*arguments):
    result = subprocess.run(arguments, check=True, capture_output=True, text=True)
    return result.stdout

network = 'routeboard-package-check'
containers = ['routeboard-check-web', 'routeboard-check-api']
with tempfile.TemporaryDirectory(prefix='routeboard-check-') as directory:
    settings=Path(directory)/'settings.json'
    password=(root/'.local-secrets/postgres_password').read_text().strip().replace('"','""')
    settings.write_text(json.dumps({'ConnectionStrings': {'RouteBoard': f'Host=host.docker.internal;Port=5433;Database=routeboard_dev;Username=routeboard;Password="{password}"'}, 'ReverseProxy': {'Address':'172.30.99.2'}}))
    settings.chmod(0o644)
    try:
        run('docker','network','create','--subnet','172.30.99.0/24',network)
        run('docker','run','-d','--rm','--name',containers[1],'--network',network,'--network-alias','api','--ip','172.30.99.3','-v',f'{settings}:/app/appsettings.Production.json:ro','routeboard-api:deployment-check')
        run('docker','run','-d','--rm','--name',containers[0],'--network',network,'--ip','172.30.99.2','-p','127.0.0.1:8443:443','-p','127.0.0.1:8089:80','-e','SITE_ADDRESS=localhost','-e','OIDC_ORIGIN=https://identity.example.test','routeboard-web:deployment-check')
        for attempt in range(30):
            ready=subprocess.run(['curl','-ksf','https://localhost:8443/healthz'],capture_output=True,text=True)
            if ready.returncode == 0:
                break
            time.sleep(1)
        assert ready.returncode==0, 'Container health check did not become ready'
        print('GET /healthz: 200, database reachable through HTTPS proxy')
        page=run('curl','-ksi','https://localhost:8443/')
        assert re.search(r'^HTTP/\S+ 200', page) and 'cache-control: no-cache' in page.lower()
        assert "script-src 'self'" in page
        print('GET /: 200, HTML revalidation and CSP present')
        asset=re.search(r'/assets/[^" ]+\.js',page).group(0)
        headers=run('curl','-ksI','https://localhost:8443'+asset)
        assert 'immutable' in headers
        etag=re.search(r'(?im)^etag: (.+)$', headers).group(1).strip()
        code=run('curl','-ks','-o','/dev/null','-w','%{http_code}','-H',f'If-None-Match: {etag}','https://localhost:8443'+asset)
        assert code=='304', code
        print('Hashed JavaScript: immutable cache; If-None-Match: 304')
        for path,method in [('/api/trips','POST'),('/mcp','POST')]:
            response=run('curl','-ksi','-X',method,'https://localhost:8443'+path)
            assert re.search(r'^HTTP/\S+ 401', response), path
            assert 'cache-control: no-store' in response.lower()
            print(f'{method} {path}: anonymous 401, no-store')
        redirect=run('curl','-sI','http://localhost:8089/')
        assert '308 Permanent Redirect' in redirect and 'https://' in redirect
        print('HTTP ingress: 308 HTTPS redirect')
        print('Local development certificate used with curl -k; public certificate validity is NOT verified.')
    finally:
        for name in containers:
            subprocess.run(['docker','rm','-f',name],capture_output=True)
        subprocess.run(['docker','network','rm',network],capture_output=True)
