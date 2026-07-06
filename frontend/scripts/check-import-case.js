/**
 * Linux deployment: verify relative import path segments match exact on-disk casing.
 */
const fs = require("fs");
const path = require("path");

const SRC = path.join(__dirname, "..", "src");
const EXTENSIONS = [".js", ".jsx", ".ts", ".tsx", ".css", ".json"];
const LEGACY_FILES = new Set([
  "pages/Admin/AdminProductForm.js",
  "pages/Admin/Products.jsx",
]);

function walk(dir, out = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.name.startsWith(".") || entry.name === "node_modules") continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full, out);
    else if (/\.(js|jsx|ts|tsx)$/.test(entry.name)) out.push(full);
  }
  return out;
}

function resolveSegment(current, segment) {
  if (!fs.existsSync(current)) {
    return { ok: false, reason: `Directory missing: ${path.relative(SRC, current)}` };
  }

  const entries = fs.readdirSync(current);
  const exact = entries.find((e) => e === segment);
  if (exact) {
    return { ok: true, next: path.join(current, exact) };
  }

  const ci = entries.find((e) => e.toLowerCase() === segment.toLowerCase());
  if (ci) {
    return {
      ok: false,
      caseMismatch: {
        segment,
        actual: ci,
        inDir: path.relative(SRC, current).replace(/\\/g, "/"),
      },
    };
  }

  for (const ext of EXTENSIONS) {
    const fileName = segment + ext;
    const fileExact = entries.find((e) => e === fileName);
    if (fileExact) {
      return { ok: true, next: path.join(current, fileName), resolvedFile: true };
    }
    const fileCi = entries.find((e) => e.toLowerCase() === fileName.toLowerCase());
    if (fileCi) {
      return {
        ok: false,
        caseMismatch: {
          segment: fileName,
          actual: fileCi,
          inDir: path.relative(SRC, current).replace(/\\/g, "/"),
        },
      };
    }
  }

  return {
    ok: false,
    reason: `Segment "${segment}" not found in ${path.relative(SRC, current)}`,
  };
}

function resolveDirectoryTarget(dirPath) {
  if (!fs.existsSync(dirPath) || !fs.statSync(dirPath).isDirectory()) {
    return { ok: false, reason: `Target directory missing: ${path.relative(SRC, dirPath)}` };
  }
  for (const ext of EXTENSIONS) {
    const indexPath = path.join(dirPath, "index" + ext);
    if (fs.existsSync(indexPath)) {
      return { ok: true };
    }
  }
  return { ok: false, reason: `Directory has no index file: ${path.relative(SRC, dirPath)}` };
}

function resolveImport(fromFile, importPath) {
  const segments = importPath.split("/");
  let current = path.dirname(fromFile);

  for (const segment of segments) {
    if (segment === ".") continue;
    if (segment === "..") {
      current = path.dirname(current);
      continue;
    }

    const step = resolveSegment(current, segment);
    if (!step.ok) return step;
    current = step.next;
    if (step.resolvedFile) return { ok: true };
  }

  if (fs.existsSync(current) && fs.statSync(current).isFile()) {
    return { ok: true };
  }

  if (fs.existsSync(current) && fs.statSync(current).isDirectory()) {
    return resolveDirectoryTarget(current);
  }

  for (const ext of EXTENSIONS) {
    if (fs.existsSync(current + ext)) return { ok: true };
  }

  return { ok: false, reason: `Target file missing: ${path.relative(SRC, current)}` };
}

function stripComments(source) {
  return source
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/^\s*\/\/.*$/gm, "");
}

const importRe = /\bfrom\s+["'](\.[^"']+)["']|\brequire\s*\(\s*["'](\.[^"']+)["']\s*\)/g;
const files = walk(SRC);
const caseIssues = [];
const missing = [];

for (const file of files) {
  const relFile = path.relative(SRC, file).replace(/\\/g, "/");
  if (LEGACY_FILES.has(relFile)) continue;

  const content = stripComments(fs.readFileSync(file, "utf8"));
  let match;
  while ((match = importRe.exec(content))) {
    const imp = match[1] || match[2];
    const result = resolveImport(file, imp);
    if (result.ok) continue;
    const item = { file: relFile, import: imp, ...result };
    if (result.caseMismatch) caseIssues.push(item);
    else missing.push(item);
  }
}

console.log(`Scanned ${files.length} files`);
console.log(`Case mismatches: ${caseIssues.length}`);
caseIssues.forEach((i) => {
  console.log(`  ${i.file}: "${i.import}" -> use "${i.actual}" in ${i.inDir}/`);
});
console.log(`Missing/broken: ${missing.length}`);
missing.forEach((i) => {
  console.log(`  ${i.file}: "${i.import}" (${i.reason})`);
});

process.exit(caseIssues.length > 0 || missing.length > 0 ? 1 : 0);
