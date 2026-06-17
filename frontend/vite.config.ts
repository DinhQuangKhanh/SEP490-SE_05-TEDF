import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    // Pin the dev server to 5173 — the backend CORS allowlist
    // (backend/TEDF.API/.env.development) only permits localhost:5173 / :3000.
    // strictPort makes Vite fail loudly if 5173 is taken instead of silently
    // drifting to 5174/5175/… which then gets blocked by CORS.
    server: {
        port: 5173,
        strictPort: true,
    },
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './src'),
        },
    },
})
