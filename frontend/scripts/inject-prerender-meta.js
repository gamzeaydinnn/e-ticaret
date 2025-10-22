// scripts/inject-prerender-meta.js
// Reads ../prerender-data.json and injects/replaces meta tags in build HTML files.
// Usage: run after build and after fix-canonical.js in CI.

const fs = require("fs");
const path = require("path");

const buildDir = path.join(__dirname, "..", "build");
const dataPath = path.join(__dirname, "..", "prerender-data.json");

function normSiteUrl(u) {
  if (!u) return "";
  if (!/^https?:\/\//i.test(u)) u = "https://" + u;
  return u.replace(/\/$/, "");
}

const siteUrl = normSiteUrl(
  process.env.NEW_HOST ||
    process.env.REACT_APP_SITE_URL ||
    process.env.SITE_URL ||
    ""
);

if (!fs.existsSync(dataPath)) {
  console.log("No prerender-data.json found, skipping meta injection");
  process.exit(0);
}

const data = JSON.parse(fs.readFileSync(dataPath, "utf8"));
if (!Array.isArray(data) || data.length === 0) {
  console.log("prerender-data.json empty, skipping");
  process.exit(0);
}

function routeToFile(route) {
  if (!route) return path.join(buildDir, "index.html");
  if (route === "/") return path.join(buildDir, "index.html");
  const p = route.replace(/^\//, "") + "/index.html";
  return path.join(buildDir, p);
}

function escapeAttr(s) {
  return String(s || "").replace(/"/g, "&quot;");
}

let patched = 0;
for (const item of data) {
  const route = item.route || item.path || "/";
  const file = routeToFile(route);
  if (!fs.existsSync(file)) continue;
  let html = fs.readFileSync(file, "utf8");

  const title = item.title || "";
  const desc = item.description || "";
  const image = item.image || "";
  const canonical = siteUrl ? `${siteUrl}${route === "/" ? "" : route}` : null;

  // Replace or insert <title>
  if (title) {
    if (/<title>.*<\/title>/i.test(html)) {
      html = html.replace(
        /<title>.*<\/title>/i,
        `<title>${escapeAttr(title)}</title>`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <title>${escapeAttr(title)}</title>`
      );
    }
  }

  // description meta
  if (desc) {
    if (/<meta[^>]+name=(?:"|')description(?:"|')/i.test(html)) {
      html = html.replace(
        /<meta([^>]+)name=(?:"|')description(?:"|')([^>]*)>/i,
        ` <meta name="description" content="${escapeAttr(desc)}">`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <meta name="description" content="${escapeAttr(desc)}">`
      );
    }
  }

  // canonical link
  if (canonical) {
    if (/<link[^>]+rel=(?:"|')canonical(?:"|')/i.test(html)) {
      html = html.replace(
        /<link([^>]+)rel=(?:"|')canonical(?:"|')([^>]*)>/i,
        `<link rel="canonical" href="${escapeAttr(canonical)}">`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <link rel="canonical" href="${escapeAttr(canonical)}">`
      );
    }
  }

  // og:title, og:description, og:image
  if (title) {
    if (/<meta[^>]+property=(?:"|')og:title(?:"|')/i.test(html)) {
      html = html.replace(
        /<meta([^>]+)property=(?:"|')og:title(?:"|')([^>]*)>/i,
        `<meta property="og:title" content="${escapeAttr(title)}">`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <meta property="og:title" content="${escapeAttr(title)}">`
      );
    }
  }
  if (desc) {
    if (/<meta[^>]+property=(?:"|')og:description(?:"|')/i.test(html)) {
      html = html.replace(
        /<meta([^>]+)property=(?:"|')og:description(?:"|')([^>]*)>/i,
        `<meta property="og:description" content="${escapeAttr(desc)}">`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <meta property="og:description" content="${escapeAttr(
          desc
        )}">`
      );
    }
  }
  if (image) {
    const imageAbs = /^https?:\/\//i.test(image)
      ? image
      : siteUrl
      ? `${siteUrl}${image.startsWith("/") ? "" : "/"}${image}`
      : image;
    if (/<meta[^>]+property=(?:"|')og:image(?:"|')/i.test(html)) {
      html = html.replace(
        /<meta([^>]+)property=(?:"|')og:image(?:"|')([^>]*)>/i,
        `<meta property="og:image" content="${escapeAttr(imageAbs)}">`
      );
    } else {
      html = html.replace(
        /<head([^>]*)>/i,
        `<head$1>\n  <meta property="og:image" content="${escapeAttr(
          imageAbs
        )}">`
      );
    }
  }

  fs.writeFileSync(file, html, "utf8");
  patched++;
  console.log("Injected meta for", route, "->", file);
}

console.log("inject-prerender-meta: done. patched=", patched);
