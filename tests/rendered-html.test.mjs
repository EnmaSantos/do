import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import test, { after, before } from "node:test";

const projectRoot = fileURLToPath(new URL("..", import.meta.url));
const nextBin = fileURLToPath(
  new URL("../node_modules/next/dist/bin/next", import.meta.url),
);
const port = 31_000 + (process.pid % 1_000);
let server;
let serverOutput = "";

before(async () => {
  server = spawn(process.execPath, [nextBin, "start", "-p", String(port)], {
    cwd: projectRoot,
    stdio: ["ignore", "pipe", "pipe"],
  });
  server.stdout.on("data", (chunk) => {
    serverOutput += chunk;
  });
  server.stderr.on("data", (chunk) => {
    serverOutput += chunk;
  });

  for (let attempt = 0; attempt < 80; attempt += 1) {
    if (server.exitCode !== null) {
      throw new Error(`Next.js exited before startup:\n${serverOutput}`);
    }
    try {
      const response = await fetch(`http://127.0.0.1:${port}/`);
      if (response.ok) return;
    } catch {
      // The server is still starting.
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }

  throw new Error(`Next.js did not start in time:\n${serverOutput}`);
});

after(() => {
  server?.kill("SIGTERM");
});

async function render() {
  return fetch(`http://127.0.0.1:${port}/`, {
    headers: { accept: "text/html" },
  });
}

test("server-renders the OpsFlow sign-in experience", async () => {
  const response = await render();
  assert.equal(response.status, 200);
  assert.match(response.headers.get("content-type") ?? "", /^text\/html\b/i);
  const html = await response.text();
  assert.match(html, /<title>OpsFlow — Operations control center(?: · OpsFlow)?<\/title>/i);
  assert.match(
    html,
    /property="og:image" content="http:\/\/127\.0\.0\.1:\d+\/og\.png"/i,
  );
  assert.match(html, /Know what/);
  assert.match(html, /Fix what/);
  assert.match(html, /admin@opsflow\.demo/);
  assert.match(html, /support@opsflow\.demo/);
  assert.match(html, /Inventory Manager/);
  assert.doesNotMatch(html, /codex-preview|Your site is taking shape|react-loading-skeleton/i);
});

test("ships product metadata and removes starter preview wiring", async () => {
  const [page, layout, packageJson] = await Promise.all([
    readFile(new URL("../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/layout.tsx", import.meta.url), "utf8"),
    readFile(new URL("../package.json", import.meta.url), "utf8"),
  ]);
  assert.match(page, /OpsFlowApp/);
  assert.match(layout, /default:\s*"OpsFlow"/);
  assert.match(packageJson, /"name": "opsflow"/);
  assert.doesNotMatch(`${page}\n${layout}\n${packageJson}`, /codex-preview|SkeletonPreview|react-loading-skeleton/);
});
