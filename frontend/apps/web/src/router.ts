import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from './stores/auth'
import ChangePasswordView from './views/ChangePasswordView.vue'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import SetupView from './views/SetupView.vue'
import UnavailableView from './views/UnavailableView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/setup', name: 'setup', component: SetupView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/change-password', name: 'change-password', component: ChangePasswordView },
    { path: '/unavailable', name: 'unavailable', component: UnavailableView },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.ready) {
    try {
      await auth.bootstrap()
    } catch (reason) {
      auth.setStartupError(reason instanceof Error ? reason.message : '無法連線到 KhaiKang API。')
      return { name: 'unavailable' }
    }
  }

  if (auth.startupError && to.name !== 'unavailable') {
    return { name: 'unavailable' }
  }

  if (auth.requiresInitialization && to.name !== 'setup') {
    return { name: 'setup' }
  }

  if (!auth.isAuthenticated && !auth.requiresInitialization && to.name !== 'login') {
    return { name: 'login' }
  }

  if (auth.user?.mustChangePassword && to.name !== 'change-password') {
    return { name: 'change-password' }
  }

  if (auth.isAuthenticated && !auth.user?.mustChangePassword && ['login', 'setup'].includes(String(to.name))) {
    return { name: 'home' }
  }

  return true
})
