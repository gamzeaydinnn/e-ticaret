// scripts/generate-prerender-list.js
// Generates a list of routes for react-snap prerendering.
// Behavior:
// - If BACKEND_URL is provided, tries to GET `${BACKEND_URL}/api/prerender/routes` and expects a JSON array of routes.
// - On any failure (no BACKEND_URL, network error, non-array response) falls back to a small default list.
// - Writes the routes into frontend/package.json -> reactSnap.include so react-snap (postbuild) will use them.

const fs = require("fs");
const path = require("path");

const pkgPath = path.join(__dirname, "..", "package.json");
const backend = process.env.BACKEND_URL || "";
const backendToken = process.env.BACKEND_API_TOKEN || "";
const timeoutMs = 5000;

const defaults = ["/", "/category/meyve-sebze"];
const prerenderDataPath = path.join(__dirname, "..", "prerender-data.json");

async function fetchRoutes(url) {
  try {
    const controller = new AbortController();
    const id = setTimeout(() => controller.abort(), timeoutMs);
    const headers = {};
    if (backendToken) headers["Authorization"] = `Bearer ${backendToken}`;
    const res = await fetch(url, { signal: controller.signal, headers });
    clearTimeout(id);
    if (!res.ok) return null;
    const json = await res.json();
    if (
      Array.isArray(json) &&
      json.length &&
      typeof json[0] === "object" &&
      json[0].route
    ) {
      return json.map((r) => r.route);
    }
    if (Array.isArray(json)) return json;
    return null;
  } catch (e) {
    return null;
  }
}

(async function main() {
  console.log("generate-prerender-list: starting");
  let routes = null;
  if (backend) {
    const url = `${backend.replace(/\/$/, "")}/api/prerender/routes`;
    console.log("Attempting to fetch prerender routes from backend:", url);
    routes = await fetchRoutes(url);
    if (routes) console.log("Fetched", routes.length, "routes from backend");
    else
      console.log(
        "Failed to fetch routes from backend, falling back to defaults"
      );
  }
  if (!routes) routes = defaults;

  // If backend returned rich objects earlier, attempt to fetch full data and cache it
  if (backend) {
    try {
      const metaUrl = `${backend.replace(
        /\/$/,
        ""
      )}/api/prerender/routes-with-meta`;
      const controller = new AbortController();
      const id = setTimeout(() => controller.abort(), timeoutMs);
      const headers = {};
      if (backendToken) headers["Authorization"] = `Bearer ${backendToken}`;
      const resp = await fetch(metaUrl, { signal: controller.signal, headers });
      clearTimeout(id);
      if (resp.ok) {
        const j = await resp.json();
        if (
          Array.isArray(j) &&
          j.length &&
          typeof j[0] === "object" &&
          j[0].route
        ) {
          // write cache file for prerender metadata
          fs.writeFileSync(
            prerenderDataPath,
            JSON.stringify(j, null, 2) + "\n",
            "utf8"
          );
          console.log("Wrote prerender-data.json with", j.length, "items");
        }
      }
    } catch (e) {
      // ignore and continue
    }
  }

  // Read package.json
  const pkgRaw = fs.readFileSync(pkgPath, "utf8");
  const pkg = JSON.parse(pkgRaw);
  pkg.reactSnap = pkg.reactSnap || {};
  // de-dupe and ensure strings
  const uniq = Array.from(new Set(routes.map((r) => String(r))));
  pkg.reactSnap.include = uniq;
  fs.writeFileSync(pkgPath, JSON.stringify(pkg, null, 2) + "\n", "utf8");
  console.log(
    "Wrote reactSnap.include to package.json with",
    uniq.length,
    "routes"
  );
})();
