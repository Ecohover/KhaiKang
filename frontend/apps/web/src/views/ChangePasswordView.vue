<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { UiButton, UiField } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const currentPassword = ref(auth.initialCredentials?.initialPassword ?? '')
const newPassword = ref('')
const confirmation = ref('')
const loading = ref(false)
const error = ref('')

async function submit(): Promise<void> {
  if (newPassword.value !== confirmation.value) {
    error.value = '新密碼與確認密碼不一致。'
    return
  }

  loading.value = true
  error.value = ''
  try {
    await auth.changePassword(currentPassword.value, newPassword.value)
    await router.push({ name: 'home' })
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '密碼變更失敗。'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>設定新密碼</h1>
    <p class="auth-panel__lead">新密碼至少需要 12 個字元。</p>

    <form class="auth-form" @submit.prevent="submit">
      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <UiField
        id="current-password"
        v-model="currentPassword"
        label="目前密碼"
        type="password"
        autocomplete="current-password"
        :disabled="loading"
      />
      <UiField
        id="new-password"
        v-model="newPassword"
        label="新密碼"
        type="password"
        autocomplete="new-password"
        :disabled="loading"
      />
      <UiField
        id="password-confirmation"
        v-model="confirmation"
        label="確認新密碼"
        type="password"
        autocomplete="new-password"
        :disabled="loading"
      />
      <div class="auth-form__actions">
        <UiButton type="submit" :loading="loading">儲存密碼</UiButton>
      </div>
    </form>
  </AuthLayout>
</template>
