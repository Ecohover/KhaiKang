<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { UiButton, UiCheckbox, UiField } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const loading = ref(false)
const error = ref('')

async function submit(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    await auth.login(username.value, password.value, rememberMe.value)
    await router.push(auth.user?.mustChangePassword ? { name: 'change-password' } : { name: 'home' })
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '登入失敗。'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>登入</h1>
    <p class="auth-panel__lead">使用 KhaiKang 本機帳號繼續。</p>

    <form class="auth-form" @submit.prevent="submit">
      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <UiField
        id="username"
        v-model="username"
        label="帳號"
        autocomplete="username"
        :disabled="loading"
      />
      <UiField
        id="password"
        v-model="password"
        label="密碼"
        type="password"
        autocomplete="current-password"
        :disabled="loading"
      />
      <UiCheckbox id="remember-me" v-model="rememberMe" label="記住此裝置" :disabled="loading" />
      <div class="auth-form__actions">
        <UiButton type="submit" :loading="loading">登入</UiButton>
      </div>
    </form>
  </AuthLayout>
</template>
