#!/usr/bin/env python3
"""Point the published site at your own domain.

    python3 scripts/bind-domain.py parametra.studio

Rewrites every absolute URL in docs/ (canonical link, Open Graph and Twitter tags,
structured data, robots.txt and sitemap.xml) from the current base to
https://<domain>, refreshes the sitemap date and writes docs/CNAME, which is the
file GitHub Pages reads to serve the site on that domain. Commit and push afterwards.
"""
import datetime, pathlib, re, sys

root = pathlib.Path(__file__).resolve().parents[1]
docs = root / 'docs'
if len(sys.argv) != 2 or sys.argv[1] in ('-h', '--help'):
    sys.exit(__doc__)
domain = re.sub(r'^https?://', '', sys.argv[1].strip().lower()).strip('/')
if not re.fullmatch(r'[a-z0-9-]+(\.[a-z0-9-]+)+', domain):
    sys.exit('That does not look like a domain name: ' + domain)
new = 'https://' + domain

index = docs / 'index.html'
html = index.read_text(encoding='utf-8')
m = re.search(r'<link rel="canonical" href="([^"]+?)/?">', html)
if not m:
    sys.exit('docs/index.html has no canonical link to rewrite.')
old = m.group(1).rstrip('/')
if old == new:
    print('Already bound to', new)
for f in (index, docs / 'robots.txt', docs / 'sitemap.xml'):
    if f.exists():
        text = f.read_text(encoding='utf-8')
        f.write_text(text.replace(old + '/', new + '/').replace(old + '"', new + '"'), encoding='utf-8')
        print(f'{f.relative_to(root)}: {text.count(old)} URL(s) now point at {new}')
sitemap = docs / 'sitemap.xml'
if sitemap.exists():
    sitemap.write_text(re.sub(r'<lastmod>[^<]*</lastmod>', f'<lastmod>{datetime.date.today().isoformat()}</lastmod>', sitemap.read_text(encoding='utf-8')), encoding='utf-8')
(docs / 'CNAME').write_text(domain + '\n', encoding='utf-8')
print('docs/CNAME ->', domain)
print('Next: commit and push, then in GitHub open Settings -> Pages, enter', domain, 'as the custom domain and tick "Enforce HTTPS" once the DNS check passes.')
