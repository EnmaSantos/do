import type { NextConfig } from "next";
import { fileURLToPath } from "node:url";

const projectRoot = fileURLToPath(new URL(".", import.meta.url));

const nextConfig: NextConfig = {
  output: "standalone",
  outputFileTracingRoot: projectRoot,
  turbopack: {
    root: projectRoot,
  },
};

export default nextConfig;
