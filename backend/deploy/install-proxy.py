"""Install the TFBC-only Nginx include after backing up and checking the site.

Run on the BaoTa server after the Docker API is healthy. This changes only the
configured site's server block; failed checks restore the original configuration.
"""
import argparse
import datetime
import hashlib
import json
import pathlib
import re
import shutil
import subprocess
import urllib.request


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--config', default='/www/server/panel/vhost/nginx/120.55.98.74_3000.conf')
    parser.add_argument('--nginx', default='/www/server/nginx/sbin/nginx')
    args = parser.parse_args()
    base = pathlib.Path('/opt/tfbc')
    health = json.load(urllib.request.urlopen('http://127.0.0.1:3107/tfbc-api/health', timeout=5))
    assert health == {'status': 'ok', 'database': 'ok'}, health

    def request(path):
        return subprocess.run([
            'curl', '--max-time', '12', '--resolve', 'metapool.cloud:443:127.0.0.1',
            '--fail', '--silent', '--show-error', 'https://metapool.cloud' + path,
        ], check=True, capture_output=True).stdout

    before = hashlib.sha256(request('/')).hexdigest()
    cfg = pathlib.Path(args.config)
    original = cfg.read_text()
    blocks = list(re.finditer(r'(?m)^server\s*\{', original))
    assert len(blocks) == 1, 'Expected exactly one server block; inspect configuration first.'
    assert re.search(r'server_name[^;]*\bmetapool\.cloud(?:\s|;)', original)
    include = 'include /opt/tfbc/backend/deploy/baota-nginx.conf;'
    assert include not in original, 'The TFBC include is already installed.'
    assert '/tfbc-api' not in original, 'Inspect the existing TFBC routing before changing it.'
    backup_dir = base / 'backups'
    backup_dir.mkdir(exist_ok=True)
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    backup = backup_dir / ('nginx-' + stamp + '.conf')
    shutil.copy2(cfg, backup)
    marker = '\n    # TFBC score API - isolated path\n    ' + include + '\n'
    try:
        cfg.write_text(original[:blocks[0].end()] + marker + original[blocks[0].end():])
        check = subprocess.run([args.nginx, '-t'], capture_output=True, text=True)
        assert check.returncode == 0, check.stderr
        subprocess.run([args.nginx, '-s', 'reload'], check=True)
        after = hashlib.sha256(request('/')).hexdigest()
        assert before == after, 'Root HTML changed.'
        public = json.loads(request('/tfbc-api/health'))
        scores = json.loads(request('/tfbc-api/v1/scores?limit=10'))
        assert public == health
        assert scores == {'scores': [], 'limit': 10}, scores
        result = {
            'deployed': True, 'health': public, 'emptyLeaderboard': True,
            'rootUnchanged': True, 'rootSha256': before, 'nginxBackup': str(backup),
        }
        (base / 'deployment-check.json').write_text(json.dumps(result, indent=2))
        print(json.dumps(result))
    except Exception:
        shutil.copy2(backup, cfg)
        subprocess.run([args.nginx, '-t'], check=True)
        subprocess.run([args.nginx, '-s', 'reload'], check=True)
        raise


if __name__ == '__main__':
    main()
