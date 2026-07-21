import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { apiClient, clearCsrfToken, problemMessage, secureHeaders } from '../api/client'
import type { AuthenticatedUserResponse } from '../api/contracts'

interface InitialCredentials {
  username: string
  initialPassword: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthenticatedUserResponse>()
  const requiresInitialization = ref(false)
  const initialCredentials = ref<InitialCredentials>()
  const startupError = ref('')
  const ready = ref(false)

  const isAuthenticated = computed(() => Boolean(user.value))

  async function bootstrap(): Promise<void> {
    if (ready.value) {
      return
    }

    const setup = await apiClient.getSetupStatus()
    if (!setup.data) {
      throw new Error('無法取得系統初始化狀態。')
    }

    requiresInitialization.value = setup.data.requiresInitialization
    if (!requiresInitialization.value) {
      await restoreSession()
    }

    ready.value = true
  }

  function setStartupError(message: string): void {
    startupError.value = message
    ready.value = true
  }

  async function restoreSession(): Promise<void> {
    const current = await apiClient.getCurrentUser()
    if (current.data) {
      user.value = current.data
      return
    }

    const refreshed = await apiClient.refreshSession(await secureHeaders())
    if (refreshed.data) {
      clearCsrfToken()
    }
    user.value = refreshed.data
  }

  async function initialize(): Promise<void> {
    const response = await apiClient.initializeAdmin(await secureHeaders())
    if (response.error || !response.data) {
      throw new Error(problemMessage(response.error, '系統初始化失敗。'))
    }

    initialCredentials.value = response.data
    requiresInitialization.value = false
  }

  async function completeInitialization(): Promise<void> {
    if (!initialCredentials.value) {
      throw new Error('初始帳號資訊已失效，請重新開啟初始化畫面。')
    }

    await login(
      initialCredentials.value.username,
      initialCredentials.value.initialPassword,
      false,
    )
  }

  async function login(username: string, password: string, rememberMe: boolean): Promise<void> {
    const response = await apiClient.login(
      { username, password, rememberMe },
      await secureHeaders(),
    )
    if (response.error || !response.data) {
      throw new Error(problemMessage(response.error, '登入失敗，請確認帳號與密碼。'))
    }

    user.value = response.data
    clearCsrfToken()
  }

  async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
    const response = await apiClient.changePassword(
      { currentPassword, newPassword },
      await secureHeaders(),
    )
    if (response.error) {
      throw new Error(problemMessage(response.error, '密碼變更失敗。'))
    }

    if (user.value) {
      user.value = { ...user.value, mustChangePassword: false }
    }
    initialCredentials.value = undefined
  }

  async function logout(): Promise<void> {
    await apiClient.logout(await secureHeaders())
    clearCsrfToken()
    user.value = undefined
    initialCredentials.value = undefined
  }

  return {
    user,
    requiresInitialization,
    initialCredentials,
    startupError,
    ready,
    isAuthenticated,
    bootstrap,
    setStartupError,
    initialize,
    completeInitialization,
    login,
    changePassword,
    logout,
  }
})
