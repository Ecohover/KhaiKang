import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { i18n } from './i18n/index'
import '@khaikang/ui/tokens.css'
import './styles/app.css'

createApp(App).use(createPinia()).use(i18n).use(router).mount('#app')
