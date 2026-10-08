#!/usr/bin/env python3
import re
import urllib.request


class NoRedir(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


opener = urllib.request.build_opener(NoRedir)
for p in ["/markalar", "/kategoriler", "/kurumsal"]:
    req = urllib.request.Request(
        "https://golkoygurme.com.tr" + p,
        method="HEAD",
        headers={"User-Agent": "Mozilla/5.0"},
    )
    try:
        r = opener.open(req, timeout=20)
        print(p, r.status)
    except urllib.error.HTTPError as e:
        print(p, e.code, e.headers.get("Location"))

html = urllib.request.urlopen(
    urllib.request.Request(
        "https://golkoygurme.com.tr/",
        headers={"User-Agent": "Mozilla/5.0", "Cache-Control": "no-cache"},
    ),
    timeout=20,
).read().decode("utf-8", "replace")
js = re.findall(r"/static/js/main\.[^\"]+\.js", html)
print("mainjs", js[:1])
if js:
    body = urllib.request.urlopen(
        urllib.request.Request(
            "https://golkoygurme.com.tr" + js[0],
            headers={"User-Agent": "Mozilla/5.0"},
        ),
        timeout=30,
    ).read().decode("utf-8", "replace")
    print("Kategoriler", "Kategoriler" in body)
    print("Markalar", "Markalar" in body)
    print("Kurumsal", "Kurumsal" in body)
    print("/kategoriler", "/kategoriler" in body)
