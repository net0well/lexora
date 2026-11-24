import 'primeicons/primeicons.css'
import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'

import App from './App.vue'
import { i18n } from './i18n'
import { createAppRouter } from './router'
import { lexoraPreset } from './theme/lexoraPreset'

const app = createApp(App)

app.use(createPinia())
app.use(i18n)
app.use(createAppRouter())
app.use(PrimeVue, {
  theme: {
    preset: lexoraPreset,
    options: { darkModeSelector: '.lexora-dark' },
  },
})
app.use(ToastService)
app.use(ConfirmationService)

app.mount('#app')
