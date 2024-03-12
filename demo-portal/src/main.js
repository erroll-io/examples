import './assets/main.css'

import { createPinia } from 'pinia'
import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import BaseCard from "./components/ui/BaseCard.vue";

const app = createApp(App)
const pinia = createPinia()

app
  .use(router)
  .use(pinia)
  .component("base-card", BaseCard)
  .mount('#app')
