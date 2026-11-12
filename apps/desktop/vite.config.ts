import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Tauri očekuje fiksni dev port i ignorira watch nad Rust kodom.
const host = process.env.TAURI_DEV_HOST;

export default defineConfig({
  plugins: [react()],
  // Tauri preuzima konzolu; ne brišemo Vite output.
  clearScreen: false,
  server: {
    port: 1420,
    strictPort: true,
    host: host || false,
    hmr: host ? { protocol: "ws", host, port: 1421 } : undefined,
    watch: { ignored: ["**/src-tauri/**"] },
  },
  // env varijable prefiksirane s VITE_ ili TAURI_ su izložene frontendu.
  envPrefix: ["VITE_", "TAURI_"],
  build: {
    target: "esnext",
    sourcemap: !!process.env.TAURI_DEBUG,
  },
});
