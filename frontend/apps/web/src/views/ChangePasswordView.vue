<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { UiButton, UiField } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const { t } = useI18n()
const router = useRouter()
const currentPassword = ref(auth.initialCredentials?.initialPassword ?? '')
const newPassword = ref('')
const confirmation = ref('')
const loading = ref(false)
const error = ref('')

async function submit(): Promise<void> {
  if (newPassword.value !== confirmation.value) {
    error.value = t('system.auth.password.mismatch')
    return
  }

  loading.value = true
  error.value = ''
  try {
    await auth.changePassword(currentPassword.value, newPassword.value)
    await router.push({ name: 'home' })
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : t('system.auth.password.failed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>{{ t('system.auth.password.title') }}</h1>
    <p class="auth-panel__lead">{{ t('system.auth.password.description') }}</p>

    <form class="auth-form" @submit.prevent="submit">
      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <UiField
        id="current-password"
        v-model="currentPassword"
        :label="t('system.auth.password.current')"
        type="password"
        autocomplete="current-password"
        :disabled="loading"
      />
      <UiField
        id="new-password"
        v-model="newPassword"
        :label="t('system.auth.password.new')"
        type="password"
        autocomplete="new-password"
        :disabled="loading"
      />
      <UiField
        id="password-confirmation"
        v-model="confirmation"
        :label="t('system.auth.password.confirm')"
        type="password"
        autocomplete="new-password"
        :disabled="loading"
      />
      <div class="auth-form__actions">
        <UiButton type="submit" :loading="loading">{{ t('system.auth.password.save') }}</UiButton>
      </div>
    </form>
  </AuthLayout>
</template>
