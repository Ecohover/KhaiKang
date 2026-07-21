<script setup lang="ts">
import { RefreshCw } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

function retry(): void {
  window.location.assign('/')
}
</script>

<template>
  <AuthLayout>
    <h1>服務暫時無法使用</h1>
    <p class="auth-panel__lead">{{ auth.startupError }}</p>
    <div class="auth-form__actions">
      <UiButton @click="retry">
        <RefreshCw :size="17" aria-hidden="true" />
        重試
      </UiButton>
    </div>
  </AuthLayout>
</template>
