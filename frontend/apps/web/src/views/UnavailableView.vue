<script setup lang="ts">
import { RefreshCw } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'
import { useI18n } from 'vue-i18n'

const auth = useAuthStore()
const { t } = useI18n()

function retry(): void {
  window.location.assign('/')
}
</script>

<template>
  <AuthLayout>
    <h1>{{ t('system.pages.unavailableTitle') }}</h1>
    <p class="auth-panel__lead">{{ auth.startupError }}</p>
    <div class="auth-form__actions">
      <UiButton @click="retry">
        <RefreshCw :size="17" aria-hidden="true" />
        {{ t('system.pages.retry') }}
      </UiButton>
    </div>
  </AuthLayout>
</template>
