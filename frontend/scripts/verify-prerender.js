// scripts/verify-prerender.js
// Simple check for prerendered HTML files: ensures given routes have canonical and og:image tags.
const fs = require("fs");
const path = require("path");

const buildDir = path.join(__dirname, "..", "build");
const routesToCheck = process.argv.slice(2).length
  ? process.argv.slice(2)
  : ["/", "/category/meyve-sebze"];

function routeToFile(route) {
  if (route === "/") return path.join(buildDir, "index.html");
  const p = route.replace(/^\//, "") + "/index.html";
  return path.join(buildDir, p);
}

let failed = false;
routesToCheck.forEach((r) => {
  const f = routeToFile(r);
  if (!fs.existsSync(f)) {
    console.error("MISSING:", f);
    failed = true;
    return;
  }
  const s = fs.readFileSync(f, "utf8");
  const hasCanonical = /<link[^>]+rel=(?:"|')canonical(?:"|')/i.test(s);
  const hasOgImage =
    /<meta[^>]+property=(?:"|')(og:image|twitter:image)(?:"|')/i.test(s);
  console.log(r, "canonical=", hasCanonical, "ogImage=", hasOgImage);
  if (!hasCanonical || !hasOgImage) failed = true;
});
if (failed) process.exit(2);
console.log("verify-prerender: ok");
