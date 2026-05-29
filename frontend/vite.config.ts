import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

const AUTH = 'http://localhost:5240';
const TOURNAMENT = 'http://localhost:5015';
const RATING = 'http://localhost:5196';
const NOTIFICATION = 'http://localhost:5210';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, 'src') },
  },
  server: {
    host: '0.0.0.0',
    port: 3000,
    proxy: {
      '/api/auth': AUTH,
      '/api/admin/tournaments': TOURNAMENT,
      '/api/admin': AUTH,
      '/api/tournaments': TOURNAMENT,
      '/api/organizer': TOURNAMENT,
      '/api/ratings': RATING,
      '/api/notifications': NOTIFICATION,
    },
  },
  preview: { host: '0.0.0.0', port: 3000 },
  build: { outDir: 'dist', sourcemap: false, target: 'es2022' },
});
