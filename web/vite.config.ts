import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      manifest: {
        name: 'Alfred',
        short_name: 'Alfred',
        description: 'Your personal butler for life admin',
        theme_color: '#1c1c1e',
        background_color: '#1c1c1e',
        display: 'standalone',
        start_url: '/',
        icons: [
          { src: '/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icon-512.png', sizes: '512x512', type: 'image/png' },
        ],
      },
    }),
  ],
  server: {
    proxy: {
      // Vite dev server forwards API calls to the ASP.NET backend — no CORS needed.
      '/api': 'http://localhost:5037',
    },
  },
})
