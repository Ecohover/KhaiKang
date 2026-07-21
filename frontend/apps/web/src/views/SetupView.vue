<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Copy, Check } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import AuthLayout from '../components/AuthLayout.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
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
    error.value = reason instanceof Error ? reason.message : '系統初始化失敗。'
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
    error.value = reason instanceof Error ? reason.message : '無法登入初始管理員帳號。'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <h1>初始化管理員</h1>
    <p class="auth-panel__lead">建立此安裝環境的第一個系統管理員帳號。</p>

    <p v-if="error" class="form-error" role="alert">{{ error }}</p>

    <template v-if="auth.initialCredentials">
      <div class="credential-block" aria-live="polite">
        <div class="credential-row">
          <span>帳號</span>
          <code>{{ auth.initialCredentials.username }}</code>
        </div>
        <div class="credential-row credential-row--password">
          <div>
            <span>一次性初始密碼</span>
            <code>{{ auth.initialCredentials.initialPassword }}</code>
          </div>
          <button
            class="icon-button"
            type="button"
            :title="copied ? '已複製' : '複製初始密碼'"
            :aria-label="copied ? '已複製' : '複製初始密碼'"
            @click="copyPassword"
          >
            <Check v-if="copied" :size="18" />
            <Copy v-else :size="18" />
          </button>
        </div>
      </div>
      <p class="security-note">此密碼只顯示一次，繼續後必須立即設定新密碼。</p>
      <div class="auth-form__actions">
        <UiButton :loading="loading" @click="continueToAccount">繼續</UiButton>
      </div>
    </template>

    <div v-else class="auth-form__actions">
      <UiButton :loading="loading" @click="initialize">建立管理員</UiButton>
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
