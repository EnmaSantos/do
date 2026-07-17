import type { Metadata } from "next";
import { headers } from "next/headers";
import { OpsFlowApp } from "./OpsFlowApp";

export async function generateMetadata(): Promise<Metadata> {
  const requestHeaders = await headers();
  const host = (requestHeaders.get("x-forwarded-host") ?? requestHeaders.get("host") ?? "localhost:3000").split(",")[0].trim();
  const protocol = (requestHeaders.get("x-forwarded-proto") ?? (host.startsWith("localhost") ? "http" : "https")).split(",")[0].trim();
  const imageUrl = `${protocol}://${host}/og.png`;

  return {
    title: "OpsFlow — Operations control center",
    description: "Production support, inventory operations, and reusable incident knowledge in one workspace.",
    openGraph: {
      title: "OpsFlow",
      description: "Know what’s running. Fix what’s not.",
      type: "website",
      images: [{ url: imageUrl, width: 1731, height: 909, alt: "OpsFlow operations control center" }],
    },
    twitter: {
      card: "summary_large_image",
      title: "OpsFlow",
      description: "Know what’s running. Fix what’s not.",
      images: [imageUrl],
    },
  };
}

export default function Home() {
  return <OpsFlowApp />;
}
