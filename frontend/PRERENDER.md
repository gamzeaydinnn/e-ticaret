Quick prerender PoC (react-snap) for CRA

This file shows the minimal commands and how to verify meta tags are visible to bots.

1. Install (already done if you ran earlier):

   npm install --save-dev react-snap
   npm install --save serve

2. Build + prerender (postbuild runs react-snap):

   npm run build

   - This will run `react-scripts build` then `react-snap` (postbuild) which crawls the app and writes prerendered HTML into `build/`.

3. Serve the prerendered build locally:

   npm run serve:prerender

   - Serves the `build/` directory on port 5000.

4. Quick verification (bot-like request):

   # Save HTML as facebook bot would see it

   curl -A "facebookexternalhit/1.1" http://localhost:5000/category/meyve-sebze -o page.html

   # Search for og tags in the saved HTML

   # On Windows PowerShell:

   Select-String -Pattern "<meta property=\"og:title\"" page.html
   Select-String -Pattern "<meta property=\"og:description\"" page.html
   Select-String -Pattern "<meta property=\"og:image\"" page.html

5. Optional: Public testing

   - If you need Facebook/Twitter debugger to access the page, expose it via ngrok or deploy the build to a static host (GitHub Pages, Netlify, Vercel, static site host).

Notes & caveats

- react-snap is build-time prerender: if product data changes frequently, you'll need to re-run the build (CI job) and redeploy to update prerendered pages.
- If you want per-request dynamic render (always-fresh), consider SSR (Next.js) or a prerendering proxy (prerender.io) instead.

Canonical & production host note

- Set `REACT_APP_SITE_URL` in your CI/production environment to your real site URL (e.g. `https://www.sizin-domain.com`). The app now uses this value to build absolute `og:image` and `canonical` tags.
- If any `localhost` strings remain in built HTML, run the included script:

  NEW_HOST=https://www.sizin-domain.com node scripts/fix-canonical.js

  Or in PowerShell (replace with your domain):

  $env:NEW_HOST='https://www.sizin-domain.com'; node scripts\fix-canonical.js

If you want, I can run the local build+prerender and run the curl tests now and paste the verification output here. If you prefer to run them yourself, paste the `page.html` or the Select-String output and I'll analyze it.

CI integration (GitHub Actions)

- A GitHub Actions workflow was added at `.github/workflows/prerender.yml`.
- The workflow expects two repository secrets:
  - `SITE_URL` (e.g. `https://www.example.com`) — sets `REACT_APP_SITE_URL` during the build and is used by the `fix-canonical.js` fallback.
  - `BACKEND_URL` (optional) — if provided the workflow will call `scripts/generate-prerender-list.js` to fetch a JSON array of routes from `${BACKEND_URL}/api/prerender/routes`. If that call fails, a small default route list is used.

How it works in CI:

1. `generate-prerender-list.js` runs and attempts to fetch dynamic routes from the backend (if `BACKEND_URL` set). It writes those routes into `frontend/package.json` under `reactSnap.include`.
2. `npm ci` and `npm run build` run with `REACT_APP_SITE_URL` set to the `SITE_URL` secret so Helmet in the app produces absolute canonical/og URLs.
3. As a safety net the workflow runs `node scripts/fix-canonical.js` (with `NEW_HOST=SITE_URL`) which is idempotent and will patch any remaining localhost references.
4. The artifact `frontend/build` is uploaded for inspection or downstream deploy steps.

If you want the backend to provide the route list, implement a small endpoint:

GET /api/prerender/routes

Return value:

["/", "/category/meyve-sebze", "/product/abc-slug", ...]

This lets the CI prerender all public routes from the live dataset. If the backend is not reachable from CI, the script falls back to a small default list.

Routes with meta (optional)

If your backend can return richer prerender data, implement:

GET /api/prerender/routes-with-meta

Return value (example):

[
{"route":"/product/abc-slug","title":"Ürün ABC","description":"Kısa açıklama","image":"https://..."},
...
]

When provided, the CI job will save this to `frontend/prerender-data.json`. This file can be consumed by a pre-render hook or used to inject meta data into templates during build.
