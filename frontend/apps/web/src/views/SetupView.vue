<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Copy, Check } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const { t } = useI18n()
const router = useRouter()
const loading = ref(false)
const error = ref('')
const copied = ref(false)

async function initialize(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    await auth.initialize()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : t('system.auth.setup.failed')
  } finally {
    loading.value = false
  }
}

async function copyPassword(): Promise<void> {
  const password = auth.initialCredentials?.initialPassword
  if (!password) {
    return
  }

  await navigator.clipboard.writeText(password)
  copied.value = true
}

async function continueToAccount(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    await auth.completeInitialization()
    await router.push({ name: 'change-password' })
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : t('system.auth.setup.loginFailed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>{{ t('system.auth.setup.title') }}</h1>
    <p class="auth-panel__lead">{{ t('system.auth.setup.description') }}</p>

    <p v-if="error" class="form-error" role="alert">{{ error }}</p>

    <template v-if="auth.initialCredentials">
      <div class="credential-block" aria-live="polite">
        <div class="credential-row">
          <span>{{ t('system.auth.setup.username') }}</span>
          <code>{{ auth.initialCredentials.username }}</code>
        </div>
        <div class="credential-row credential-row--password">
          <div>
            <span>{{ t('system.auth.setup.initialPassword') }}</span>
            <code>{{ auth.initialCredentials.initialPassword }}</code>
          </div>
          <button
            class="icon-button"
            type="button"
            :title="t(copied ? 'system.auth.setup.copied' : 'system.auth.setup.copyPassword')"
            :aria-label="t(copied ? 'system.auth.setup.copied' : 'system.auth.setup.copyPassword')"
            @click="copyPassword"
          >
            <Check v-if="copied" :size="18" />
            <Copy v-else :size="18" />
          </button>
        </div>
      </div>
      <p class="security-note">{{ t('system.auth.setup.securityNote') }}</p>
      <div class="auth-form__actions">
        <UiButton :loading="loading" @click="continueToAccount">{{ t('system.auth.setup.continue') }}</UiButton>
      </div>
    </template>

    <div v-else class="auth-form__actions">
      <UiButton :loading="loading" @click="initialize">{{ t('system.auth.setup.create') }}</UiButton>
    </div>
  </AuthLayout>
</template>

<style scoped>
.credential-row--password {
  grid-template-columns: 1fr 38px;
  align-items: center;
}

.credential-row--password > div {
  display: grid;
  gap: 4px;
  min-width: 0;
}

.icon-button {
  display: grid;
  width: 38px;
  height: 38px;
  padding: 0;
  place-items: center;
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  cursor: pointer;
}

.icon-button:hover {
  color: var(--kk-text);
  border-color: var(--kk-border-strong);
}

.security-note {
  color: var(--kk-text-muted);
  font-size: 0.8125rem;
  line-height: 1.5;
}
</style>
