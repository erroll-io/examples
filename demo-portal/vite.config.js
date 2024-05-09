import { fileURLToPath, URL } from 'node:url'

import { defineConfig, loadEnv } from 'vite'
import basicSsl from '@vitejs/plugin-basic-ssl'
import vue from '@vitejs/plugin-vue'
import { hydrateEnvFromSsm } from './src/config.js'

export default defineConfig(async ({ command, mode }) => {

  const env = loadEnv(mode, process.cwd(), 'VUE_APP')

  await hydrateEnvFromSsm(env);

  return {
    define: {
      'process.env': JSON.stringify(env)
    },
    plugins: [
      vue(),
      basicSsl({
        domains: ['*.erroll.io']
      })
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    server: {
      host: 'local.erroll.io', 
      port: 8080,
    }
  }
})
