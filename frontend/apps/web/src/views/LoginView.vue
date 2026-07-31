<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { UiButton, UiCheckbox, UiField } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const { t } = useI18n()
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
    error.value = reason instanceof Error ? reason.message : t('system.auth.login.failed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>{{ t('system.auth.login.title') }}</h1>
    <p class="auth-panel__lead">{{ t('system.auth.login.description') }}</p>

    <form class="auth-form" @submit.prevent="submit">
      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <UiField
        id="username"
        v-model="username"
        :label="t('system.auth.login.username')"
        autocomplete="username"
        :disabled="loading"
      />
      <UiField
        id="password"
        v-model="password"
        :label="t('system.auth.login.password')"
        type="password"
        autocomplete="current-password"
        :disabled="loading"
      />
      <UiCheckbox id="remember-me" v-model="rememberMe" :label="t('system.auth.login.remember')" :disabled="loading" />
      <div class="auth-form__actions">
        <UiButton type="submit" :loading="loading">{{ t('system.auth.login.submit') }}</UiButton>
      </div>
    </form>
  </AuthLayout>
</template>
