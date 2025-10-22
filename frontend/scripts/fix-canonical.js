// scripts/fix-canonical.js
// Safe, idempotent fixer for prerendered HTML files.
// Usage (PowerShell):
// Set-Location 'c:\Users\GAMZE\Desktop\eticaret\frontend'; $env:NEW_HOST='https://www.example.com'; node scripts\fix-canonical.js

const fs = require("fs");
const path = require("path");

const buildDir = path.join(__dirname, "..", "build");
const oldHost = process.env.OLD_HOST || "http://localhost:45678";
let newHost = process.env.NEW_HOST || process.env.REACT_APP_SITE_URL || "";

if (!newHost) {
  console.error(
    "NEW_HOST or REACT_APP_SITE_URL not provided. Set NEW_HOST or REACT_APP_SITE_URL env var."
  );
  process.exit(1);
}

// normalize: ensure protocol and remove trailing slash
if (!/^https?:\/\//i.test(newHost)) {
  newHost = `https://${newHost}`;
}
newHost = newHost.replace(/\/$/, "");

function walk(dir) {
  return fs
    .readdirSync(dir, { withFileTypes: true })
    .flatMap((d) =>
      d.isDirectory() ? walk(path.join(dir, d.name)) : [path.join(dir, d.name)]
    );
}

function makeAbsolutePath(p) {
  if (!p) return p;
  if (/^https?:\/\//i.test(p)) return p; // already absolute
  if (p.startsWith("//")) return `https:${p}`; // protocol-relative -> https
  if (p.startsWith("/")) return `${newHost}${p}`;
  return `${newHost}/${p}`;
}

function resolvePossiblyConcatenated(orig) {
  if (!orig) return orig;
  // If already points to newHost, keep
  if (orig.indexOf(newHost) === 0) return orig;
  // If contains oldHost followed by http(s) (concatenation artifact), drop the oldHost prefix
  const concatPattern = new RegExp(
    oldHost.replace(/[-/\\^$*+?.()|[\]{}]/g, "\\$&") + "(https?:\\/\\/)",
    "i"
  );
  if (concatPattern.test(orig)) {
    return orig.replace(concatPattern, "$1");
  }
  // If starts with oldHost -> replace host with newHost
  if (orig.indexOf(oldHost) === 0) {
    return newHost + orig.slice(oldHost.length);
  }
  // If relative path
  if (orig.startsWith("/")) return `${newHost}${orig}`;
  // If protocol-relative
  if (orig.startsWith("//")) return `https:${orig}`;
  // otherwise, leave as-is
  return orig;
}

if (!fs.existsSync(buildDir)) {
  console.error("Build directory not found:", buildDir);
  process.exit(1);
}

const files = walk(buildDir).filter((f) => f.endsWith(".html"));
let patchedCount = 0;

files.forEach((file) => {
  let s = fs.readFileSync(file, "utf8");
  const original = s;

  // 1) Fix canonical link href
  s = s.replace(/<link[^>]*rel=(?:"|')canonical(?:"|')[^>]*>/gi, (match) => {
    const hrefMatch = match.match(/href=(?:"|')([^"']+)(?:"|')/i);
    if (!hrefMatch) return match;
    const raw = hrefMatch[1];
    const fixed = resolvePossiblyConcatenated(raw);
    if (fixed === raw) return match; // nothing changed
    return match.replace(hrefMatch[0], `href="${fixed}"`);
  });

  // 2) Fix og:image and twitter:image meta tags
  s = s.replace(
    /<meta[^>]*(?:property|name)=(?:"|')(og:image|twitter:image)(?:"|')[^>]*>/gi,
    (match) => {
      const contentMatch = match.match(/content=(?:"|')([^"']+)(?:"|')/i);
      if (!contentMatch) return match;
      const raw = contentMatch[1];
      const fixed = resolvePossiblyConcatenated(raw);
      const absolute = makeAbsolutePath(fixed);
      if (absolute === raw) return match;
      return match.replace(contentMatch[0], `content="${absolute}"`);
    }
  );

  // 3) Convert src="/..." and src='/...' for images/scripts if they are root-relative
  s = s.replace(
    /(src=)(["'])\/(images\/[^"]+?)\2/gi,
    (m, p1, q, p3) => `${p1}${q}${newHost}/${p3}${q}`
  );
  s = s.replace(
    /(href=)(["'])\/(images\/[^"]+?)\2/gi,
    (m, p1, q, p3) => `${p1}${q}${newHost}/${p3}${q}`
  );

  // 4) Convert url('/images/...) inside inline styles
  s = s.replace(
    /url\((['"]?)\/images\/([^\)'"]+)\1\)/gi,
    (m, q, p1) => `url(${q}${newHost}/images/${p1}${q})`
  );

  // 5) Clean up concatenation artifacts where oldHost was left adjacent to newHost due to prior bad replacements
  const escapedOld = oldHost.replace(/[-/\\^$*+?.()|[\]{}]/g, "\\$&");
  s = s.replace(new RegExp(escapedOld + "(https?:\\/\\/)", "gi"), "$1");

  if (s !== original) {
    fs.writeFileSync(file, s, "utf8");
    console.log("patched", file);
    patchedCount++;
  }
});

console.log(`done. files inspected: ${files.length}, patched: ${patchedCount}`);
