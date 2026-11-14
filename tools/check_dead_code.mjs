#!/usr/bin/env node
/* TypeScript AST-based import graph. The compiler's noUnusedLocals and
 * noUnusedParameters plus Rust Clippy check individual symbols separately. */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
const project = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../apps/desktop");
const src = path.join(project, "src");
// TypeScript 7 ships a native compiler but no JavaScript Compiler API.
// The official TypeScript 6 compatibility package supplies AST parsing only.
const requireTs = createRequire(path.join(project, "package.json"));
const js = requireTs("@typescript/typescript6");
const ts = js.default ?? js;
function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((ent) =>
    ent.isDirectory() ? walk(path.join(dir, ent.name)) : [path.join(dir, ent.name)]);
}
const all = walk(src).filter((p) => /\.(tsx?|jsx?)$/.test(p) && !p.endsWith(".d.ts"));
const allSet = new Set(all);
const entrypoints = [path.join(src, "main.tsx"),
  ...walk(path.join(project, "tests")).filter((p) => /\.(mjs|js|ts|tsx)$/.test(p))];
const reachable = new Set();
function visit(file) {
  if (reachable.has(file)) return;
  reachable.add(file);
  const code = fs.readFileSync(file, "utf8");
  const source = ts.createSourceFile(file, code, ts.ScriptTarget.Latest, true,
    file.endsWith(".tsx") ? ts.ScriptKind.TSX : file.endsWith(".ts") ? ts.ScriptKind.TS : ts.ScriptKind.JS);
  function resolve(spec) {
    if (!spec.startsWith(".") && !spec.startsWith("@/")) return;
    const root = spec.startsWith("@/") ? path.join(src, spec.slice(2)) : path.resolve(path.dirname(file), spec);
    const options = [root, root + ".ts", root + ".tsx", root + ".js", root + ".jsx",
      path.join(root, "index.ts"), path.join(root, "index.tsx")];
    const target = options.find((x) => allSet.has(x));
    if (target) visit(target);
  }
  function scan(node) {
    if ((ts.isImportDeclaration(node) || ts.isExportDeclaration(node))
      && node.moduleSpecifier && ts.isStringLiteral(node.moduleSpecifier))
      resolve(node.moduleSpecifier.text);
    if (ts.isCallExpression(node) && node.arguments.length
      && ts.isStringLiteral(node.arguments[0])
      && (node.expression.kind === ts.SyntaxKind.ImportKeyword
        || node.expression.getText(source) === "require"))
      resolve(node.arguments[0].text);
    ts.forEachChild(node, scan);
  }
  scan(source);
}
for (const p of entrypoints) visit(p);
const orphaned = all.filter((p) => !reachable.has(p)).map((p) => path.relative(project, p));
if (orphaned.length) {
  console.error("Unused production TS modules (delete or wire them into the app):\n" + orphaned.join("\n"));
  process.exitCode = 1;
} else {
  console.log("Dead-code module graph OK: " + all.length + " production TS/TSX modules reachable via app/tests");
}
