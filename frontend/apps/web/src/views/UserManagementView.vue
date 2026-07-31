<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { Check, Clipboard, Pencil, Plus, RefreshCw, Save, UserRound, X } from '@lucide/vue'
import { UiButton, UiField, UiSaveToast, UiSaveToastStack } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { AccountResponse, AccountStatus } from '../api/contracts'
import {
  ACCOUNT_CREATE_PERMISSION,
  ACCOUNT_SUSPEND_PERMISSION,
  ACCOUNT_UPDATE_PERMISSION,
} from '../navigation'
import { useSaveNotice, type SaveNotice } from '../composables/useSaveNotice'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const accounts = ref<AccountResponse[]>([])
const loading = ref(true)
const loadingError = ref('')
const showCreateForm = ref(false)
const username = ref('')
const creating = ref(false)
const createError = ref('')
const copied = ref(false)
const updatingAccountId = ref<string>()
const statusError = ref('')
const editingAccountId = ref<string>()
const editingUsername = ref('')
const editError = ref('')
const { saveNotice, saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()

const canCreate = computed(() =>
  auth.user?.systemPermissions.includes(ACCOUNT_CREATE_PERMISSION) ?? false,
)
const canChangeStatus = computed(() =>
  auth.user?.systemPermissions.includes(ACCOUNT_SUSPEND_PERMISSION) ?? false,
)
const canUpdate = computed(() =>
  auth.user?.systemPermissions.includes(ACCOUNT_UPDATE_PERMISSION) ?? false,
)
const usernameError = computed(() => {
  if (!username.value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/.test(username.value)
    ? ''
    : '請以英文字母或數字開頭，並只使用英文、數字、句點、底線或連字號。'
})
const editingUsernameError = computed(() => validateUsername(editingUsername.value))

onMounted(loadAccounts)

async function loadAccounts(): Promise<void> {
  loading.value = true
  loadingError.value = ''
  try {
    const result = await apiClient.listAccounts()
    if (result.error) {
      loadingError.value = problemMessage(result.error, '無法載入使用者，請稍後再試。')
      return
    }

    accounts.value = result.data ?? []
  } catch {
    loadingError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    loading.value = false
  }
}

function closeCreateForm(): void {
  showCreateForm.value = false
  username.value = ''
  createError.value = ''
  if (saveNotice.value?.mode === 'created') {
    clearSaveNotice()
  }
}

async function createAccount(): Promise<void> {
  if (!username.value.trim() || usernameError.value) return

  creating.value = true
  createError.value = ''
  try {
    const result = await apiClient.createAccount(
      { username: username.value.trim() },
      await secureHeaders(),
    )
    if (!result.data) {
      createError.value = problemMessage(result.error, '建立使用者失敗，請稍後再試。')
      return
    }

    accounts.value = [...accounts.value, result.data.account]
      .sort((left, right) => left.username.localeCompare(right.username))
    showCreated(result.data.account.username, result.data.initialPassword)
  } catch {
    createError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    creating.value = false
  }
}

async function copyCredentials(notice: SaveNotice): Promise<void> {
  if (!notice.initialPassword) return

  await navigator.clipboard.writeText(
    `帳號：${notice.recordKey}\n初始密碼：${notice.initialPassword}`,
  )
  copied.value = true
  window.setTimeout(() => {
    copied.value = false
  }, 1600)
}

async function continueCreating(noticeId: number): Promise<void> {
  clearSaveNotice(noticeId)
  copied.value = false
  username.value = ''
  createError.value = ''
  showCreateForm.value = true
  await nextTick()
  document.getElementById('account-username')?.focus()
}

async function finishCreating(noticeId: number): Promise<void> {
  clearSaveNotice(noticeId)
  copied.value = false
  closeCreateForm()
  await nextTick()
  document.querySelector('.account-list')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

function startEditing(account: AccountResponse): void {
  editingAccountId.value = account.id
  editingUsername.value = account.username
  editError.value = ''
}

function cancelEditing(): void {
  editingAccountId.value = undefined
  editingUsername.value = ''
  editError.value = ''
}

async function saveAccount(account: AccountResponse): Promise<void> {
  if (!editingUsername.value.trim() || editingUsernameError.value) return

  updatingAccountId.value = account.id
  editError.value = ''
  try {
    const result = await apiClient.updateAccount(
      account.id,
      { username: editingUsername.value.trim(), version: account.version },
      await secureHeaders(),
    )
    if (!result.data) {
      editError.value = problemMessage(result.error, '修改使用者失敗，請重新載入後再試。')
      return
    }

    accounts.value = accounts.value
      .map((item) => item.id === result.data?.id ? result.data : item)
      .sort((left, right) => left.username.localeCompare(right.username))
    showUpdated(result.data.username)
    cancelEditing()
  } catch {
    editError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    updatingAccountId.value = undefined
  }
}

async function changeStatus(account: AccountResponse, status: AccountStatus): Promise<void> {
  if (account.status === status || account.id === auth.user?.id) return

  updatingAccountId.value = account.id
  statusError.value = ''
  try {
    const result = await apiClient.updateAccountStatus(
      account.id,
      { status, version: account.version },
      await secureHeaders(),
    )
    if (!result.data) {
      statusError.value = problemMessage(result.error, '更新帳號狀態失敗，請重新載入後再試。')
      return
    }

    accounts.value = accounts.value.map((item) =>
      item.id === result.data?.id ? result.data : item,
    )
    showUpdated(result.data.username)
  } catch {
    statusError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    updatingAccountId.value = undefined
  }
}

function validateUsername(value: string): string {
  if (!value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/.test(value)
    ? ''
    : '請以英文字母或數字開頭，並只使用英文、數字、句點、底線或連字號。'
}

function statusLabel(status: AccountStatus): string {
  return {
    active: '啟用',
    suspended: '停權',
    disabled: '停用',
  }[status]
}

function formatDate(value: string | null): string {
  if (!value) return '尚未登入'
  return new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
</script>

<template>
  <section class="users-page">
    <header class="page-heading">
      <div>
        <p class="eyebrow">System administration</p>
        <h2>使用者管理</h2>
        <p>建立本機帳號並管理登入狀態。專案角色仍在各專案的成員管理中設定。</p>
      </div>
      <UiButton v-if="canCreate" @click="showCreateForm = true">
        <Plus :size="18" aria-hidden="true" />
        建立使用者
      </UiButton>
    </header>

    <form v-if="showCreateForm" class="create-panel" @submit.prevent="createAccount">
      <div class="create-panel__header">
        <div>
          <h3>建立一般使用者</h3>
          <p>帳號預設套用 User 角色，初始密碼只會顯示一次。</p>
        </div>
        <button type="button" aria-label="關閉建立表單" @click="closeCreateForm">
          <X :size="20" aria-hidden="true" />
        </button>
      </div>
      <UiField
        id="account-username"
        v-model="username"
        label="帳號名稱"
        autocomplete="off"
        :disabled="creating"
        :error="usernameError"
      />
      <p v-if="createError" class="form-error" role="alert">{{ createError }}</p>
      <div class="create-panel__actions">
        <UiButton type="button" variant="secondary" :disabled="creating" @click="closeCreateForm">
          取消
        </UiButton>
        <UiButton
          type="submit"
          :loading="creating"
          :disabled="!username.trim() || Boolean(usernameError)"
        >
          建立帳號
        </UiButton>
      </div>
    </form>

    <p v-if="statusError || editError" class="page-error" role="alert">
      {{ statusError || editError }}
    </p>

    <p v-if="loading" class="state-panel" aria-live="polite">正在載入使用者…</p>
    <div v-else-if="loadingError" class="state-panel state-panel--error" role="alert">
      <p>{{ loadingError }}</p>
      <UiButton variant="secondary" @click="loadAccounts">
        <RefreshCw :size="17" aria-hidden="true" />
        重新載入
      </UiButton>
    </div>
    <div v-else class="account-list">
      <article v-for="account in accounts" :key="account.id" class="account-card">
        <div class="account-card__identity">
          <span class="avatar" aria-hidden="true">{{ account.username.slice(0, 1).toUpperCase() }}</span>
          <div class="account-card__identity-body">
            <div v-if="editingAccountId === account.id" class="inline-editor">
              <label :for="`username-${account.id}`">帳號名稱</label>
              <div>
                <input
                  :id="`username-${account.id}`"
                  v-model="editingUsername"
                  maxlength="100"
                  :disabled="updatingAccountId === account.id"
                  :aria-invalid="Boolean(editingUsernameError)"
                  @keydown.escape="cancelEditing"
                  @keydown.enter.prevent="saveAccount(account)"
                />
                <button
                  type="button"
                  title="儲存帳號名稱"
                  :disabled="
                    updatingAccountId === account.id ||
                    !editingUsername.trim() ||
                    Boolean(editingUsernameError)
                  "
                  @click="saveAccount(account)"
                >
                  <Save :size="17" aria-hidden="true" />
                  <span>儲存</span>
                </button>
                <button
                  type="button"
                  title="取消編輯"
                  :disabled="updatingAccountId === account.id"
                  @click="cancelEditing"
                >
                  <X :size="17" aria-hidden="true" />
                  <span>取消</span>
                </button>
              </div>
              <small v-if="editingUsernameError">{{ editingUsernameError }}</small>
            </div>
            <div v-else class="account-card__name">
              <h3>{{ account.username }}</h3>
              <span v-if="account.id === auth.user?.id">目前帳號</span>
              <button
                v-if="canUpdate && account.id !== auth.user?.id"
                type="button"
                title="編輯使用者"
                @click="startEditing(account)"
              >
                <Pencil :size="15" aria-hidden="true" />
                <span>編輯</span>
              </button>
            </div>
            <p>{{ account.systemRoles.join(' · ') || '無系統角色' }}</p>
          </div>
        </div>
        <dl class="account-card__meta">
          <div><dt>首次改密碼</dt><dd>{{ account.mustChangePassword ? '需要' : '已完成' }}</dd></div>
          <div><dt>最後登入</dt><dd>{{ formatDate(account.lastLoginAt) }}</dd></div>
        </dl>
        <label class="status-field">
          <span>帳號狀態</span>
          <select
            :value="account.status"
            :disabled="
              !canChangeStatus ||
              account.id === auth.user?.id ||
              updatingAccountId === account.id
            "
            @change="changeStatus(account, ($event.target as HTMLSelectElement).value as AccountStatus)"
          >
            <option value="active">{{ statusLabel('active') }}</option>
            <option value="suspended">{{ statusLabel('suspended') }}</option>
            <option value="disabled">{{ statusLabel('disabled') }}</option>
          </select>
        </label>
      </article>
      <div v-if="accounts.length === 0" class="state-panel">
        <UserRound :size="28" aria-hidden="true" />
        <p>目前沒有使用者。</p>
      </div>
    </div>

    <UiSaveToastStack>
      <UiSaveToast
        v-for="notice in saveNotices"
        :key="notice.id"
        inline
        :mode="notice.mode"
        record-label="使用者帳號"
        :record-key="notice.recordKey"
        :auto-close="notice.mode !== 'created'"
        :allow-continue="notice.mode === 'created'"
        @continue="continueCreating(notice.id)"
        @finish="finishCreating(notice.id)"
        @close="clearSaveNotice(notice.id)"
      >
        <template v-if="notice.initialPassword">
          <p class="credential-hint">初始密碼只顯示一次，請立即保存。</p>
          <div class="credential-value">
            <code>{{ notice.initialPassword }}</code>
            <button type="button" @click="copyCredentials(notice)">
              <component :is="copied ? Check : Clipboard" :size="16" aria-hidden="true" />
              {{ copied ? '已複製' : '複製帳密' }}
            </button>
          </div>
        </template>
      </UiSaveToast>
    </UiSaveToastStack>
  </section>
</template>

<style scoped>
.users-page {
  display: grid;
  gap: 24px;
}

.page-heading,
.create-panel__header,
.create-panel__actions,
.account-card,
.account-card__identity,
.account-card__name {
  display: flex;
}

.page-heading {
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
}

.page-heading h2,
.create-panel h3,
.account-card h3 {
  margin: 0;
}

.page-heading h2 {
  margin-top: 3px;
  font-size: clamp(1.65rem, 3vw, 2.2rem);
}

.page-heading > div > p:last-child,
.create-panel__header p,
.account-card p {
  margin: 7px 0 0;
  color: var(--kk-text-muted);
}

.eyebrow {
  margin: 0;
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 750;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.create-panel {
  display: grid;
  gap: 20px;
  padding: 22px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  box-shadow: var(--kk-shadow);
}

.create-panel__header {
  justify-content: space-between;
  gap: 12px;
}

.create-panel__header > button {
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
}

.create-panel__actions {
  justify-content: flex-end;
  gap: 10px;
}

.account-card__meta > div {
  display: grid;
  gap: 4px;
}

dt {
  color: var(--kk-text-muted);
  font-size: 0.75rem;
}

dd {
  margin: 0;
}

.account-list {
  display: grid;
  gap: 12px;
}

.account-card {
  align-items: center;
  gap: 24px;
  padding: 18px 20px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.account-card__identity {
  align-items: center;
  min-width: 290px;
  gap: 12px;
}

.account-card__identity-body {
  min-width: 0;
  flex: 1;
}

.avatar {
  display: grid;
  width: 42px;
  height: 42px;
  flex: 0 0 auto;
  place-items: center;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 50%;
  font-weight: 750;
}

.account-card__name {
  align-items: center;
  gap: 8px;
}

.account-card__name > span {
  padding: 3px 7px;
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
  border-radius: 999px;
  font-size: 0.7rem;
}

.account-card__name button,
.inline-editor button,
.credential-value button {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
  border-radius: var(--kk-radius);
  cursor: pointer;
  font: inherit;
}

.account-card__name button {
  padding: 4px 7px;
  font-size: 0.75rem;
}

.account-card__name button:hover,
.inline-editor button:hover:not(:disabled),
.credential-value button:hover {
  color: var(--kk-text);
  background: var(--kk-surface-subtle);
}

.inline-editor {
  display: grid;
  gap: 6px;
}

.inline-editor > label {
  color: var(--kk-text-muted);
  font-size: 0.72rem;
}

.inline-editor > div {
  display: flex;
  align-items: center;
  gap: 5px;
}

.inline-editor input {
  width: min(220px, 100%);
  min-height: 36px;
  padding: 7px 9px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.inline-editor input[aria-invalid='true'] {
  border-color: var(--kk-danger);
}

.inline-editor button {
  min-height: 34px;
  padding: 5px 7px;
  font-size: 0.75rem;
}

.inline-editor button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.inline-editor small {
  color: var(--kk-danger);
}

.account-card__meta {
  display: grid;
  grid-template-columns: repeat(2, minmax(120px, 1fr));
  flex: 1;
  gap: 18px;
  margin: 0;
}

.status-field {
  display: grid;
  min-width: 125px;
  gap: 6px;
  color: var(--kk-text-muted);
  font-size: 0.75rem;
}

.status-field select {
  min-height: 38px;
  padding: 7px 32px 7px 10px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
}

.state-panel {
  display: grid;
  margin: 0;
  padding: 36px 20px;
  place-items: center;
  gap: 10px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.state-panel--error,
.form-error,
.page-error {
  color: var(--kk-danger);
}

.form-error,
.page-error {
  margin: 0;
}

.credential-hint {
  margin: 0 0 8px;
  color: var(--kk-text-muted);
  font-size: 0.8rem;
}

.credential-value {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.credential-value code {
  overflow-wrap: anywhere;
  font-size: 0.9rem;
  font-weight: 700;
}

.credential-value button {
  flex: 0 0 auto;
  padding: 5px 7px;
  font-size: 0.78rem;
}

@media (max-width: 780px) {
  .page-heading,
  .account-card {
    align-items: stretch;
    flex-direction: column;
  }

  .account-card__meta {
    width: 100%;
  }

  .status-field {
    width: 100%;
  }
}

@media (max-width: 520px) {
  .account-card__meta {
    grid-template-columns: 1fr;
  }

  .inline-editor > div {
    align-items: stretch;
    flex-wrap: wrap;
  }

  .inline-editor input {
    width: 100%;
  }
}
</style>
