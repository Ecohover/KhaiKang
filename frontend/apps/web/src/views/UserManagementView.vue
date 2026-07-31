<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Check, Clipboard, Pencil, Plus, RefreshCw, Save, UserRound, X } from '@lucide/vue'
import { UiActionDialog, UiButton, UiCreateActions, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { AccountResponse, AccountStatus } from '../api/contracts'
import {
  ACCOUNT_CREATE_PERMISSION,
  ACCOUNT_SUSPEND_PERMISSION,
  ACCOUNT_UPDATE_PERMISSION,
} from '../navigation'
import { useSaveNotice } from '../composables/useSaveNotice'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const { t, d } = useI18n()
const accounts = ref<AccountResponse[]>([])
const loading = ref(true)
const loadingError = ref('')
const showCreateForm = ref(false)
const username = ref('')
const creating = ref(false)
const createError = ref('')
const copied = ref(false)
const continueAfterCreate = ref(false)
const updatingAccountId = ref<string>()
const statusError = ref('')
const editingAccountId = ref<string>()
const editingUsername = ref('')
const editError = ref('')
const credentialPrompt = ref<{ username: string; initialPassword: string }>()
const { showCreated, showUpdated } = useSaveNotice()

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
    : t('system.users.usernameInvalid')
})
const editingUsernameError = computed(() => validateUsername(editingUsername.value))

onMounted(loadAccounts)

async function loadAccounts(): Promise<void> {
  loading.value = true
  loadingError.value = ''
  try {
    const result = await apiClient.listAccounts()
    if (result.error) {
      loadingError.value = problemMessage(result.error, t('system.users.loadFailed'))
      return
    }

    accounts.value = result.data ?? []
  } catch {
    loadingError.value = t('system.users.connectionFailed')
  } finally {
    loading.value = false
  }
}

function closeCreateForm(): void {
  showCreateForm.value = false
  username.value = ''
  createError.value = ''
}

async function createAccount(continueCreating = false): Promise<void> {
  if (!username.value.trim() || usernameError.value) return

  creating.value = true
  createError.value = ''
  try {
    const result = await apiClient.createAccount(
      { username: username.value.trim() },
      await secureHeaders(),
    )
    if (!result.data) {
      createError.value = problemMessage(result.error, t('system.users.createFailed'))
      return
    }

    accounts.value = [...accounts.value, result.data.account]
      .sort((left, right) => left.username.localeCompare(right.username))
    continueAfterCreate.value = continueCreating
    showCreated(t('system.users.record'), result.data.account.username)
    credentialPrompt.value = {
      username: result.data.account.username,
      initialPassword: result.data.initialPassword,
    }
  } catch {
    createError.value = t('system.users.connectionFailed')
  } finally {
    creating.value = false
  }
}

async function copyCredentials(): Promise<void> {
  if (!credentialPrompt.value) return

  await navigator.clipboard.writeText(
    t('system.users.credentialText', {
      username: credentialPrompt.value.username,
      password: credentialPrompt.value.initialPassword,
    }),
  )
  copied.value = true
  window.setTimeout(() => {
    copied.value = false
  }, 1600)
}

async function closeCredentialDialog(): Promise<void> {
  if (!credentialPrompt.value) return
  credentialPrompt.value = undefined
  copied.value = false
  if (continueAfterCreate.value) {
    username.value = ''
    createError.value = ''
    showCreateForm.value = true
    await nextTick()
    document.getElementById('account-username')?.focus()
  } else {
    closeCreateForm()
    await nextTick()
    document.querySelector('.account-list')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
  continueAfterCreate.value = false
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
      editError.value = problemMessage(result.error, t('system.users.updateFailed'))
      return
    }

    accounts.value = accounts.value
      .map((item) => item.id === result.data?.id ? result.data : item)
      .sort((left, right) => left.username.localeCompare(right.username))
    showUpdated(t('system.users.record'), result.data.username)
    cancelEditing()
  } catch {
    editError.value = t('system.users.connectionFailed')
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
      statusError.value = problemMessage(result.error, t('system.users.statusFailed'))
      return
    }

    accounts.value = accounts.value.map((item) =>
      item.id === result.data?.id ? result.data : item,
    )
    showUpdated(t('system.users.record'), result.data.username)
  } catch {
    statusError.value = t('system.users.connectionFailed')
  } finally {
    updatingAccountId.value = undefined
  }
}

function validateUsername(value: string): string {
  if (!value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/.test(value)
    ? ''
    : t('system.users.usernameInvalid')
}

function statusLabel(status: AccountStatus): string {
  return t(`system.users.statuses.${status}`)
}

function formatDate(value: string | null): string {
  if (!value) return t('system.users.neverLoggedIn')
  return d(new Date(value), 'dateTime')
}
</script>

<template>
  <section class="users-page">
    <header class="page-heading">
      <div>
        <p class="eyebrow">System administration</p>
        <h2>{{ t('system.users.title') }}</h2>
        <p>{{ t('system.users.description') }}</p>
      </div>
      <UiButton v-if="canCreate" @click="showCreateForm = true">
        <Plus :size="18" aria-hidden="true" />
        {{ t('system.users.create') }}
      </UiButton>
    </header>

    <form v-if="showCreateForm" class="create-panel" @submit.prevent="createAccount(false)">
      <div class="create-panel__header">
        <div>
          <h3>{{ t('system.users.createTitle') }}</h3>
          <p>{{ t('system.users.createDescription') }}</p>
        </div>
        <button type="button" :aria-label="t('system.users.closeCreate')" @click="closeCreateForm">
          <X :size="20" aria-hidden="true" />
        </button>
      </div>
      <UiField
        id="account-username"
        v-model="username"
        :label="t('system.users.username')"
        autocomplete="off"
        :disabled="creating"
        :error="usernameError"
      />
      <p v-if="createError" class="form-error" role="alert">{{ createError }}</p>
      <UiCreateActions
        :loading="creating"
        :disabled="!username.trim() || Boolean(usernameError)"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('system.users.create')"
        :continue-label="t('system.users.createAndContinue')"
        @cancel="closeCreateForm"
        @create="createAccount(false)"
        @create-continue="createAccount(true)"
      />
    </form>

    <p v-if="statusError || editError" class="page-error" role="alert">
      {{ statusError || editError }}
    </p>

    <p v-if="loading" class="state-panel" aria-live="polite">{{ t('system.users.loading') }}</p>
    <div v-else-if="loadingError" class="state-panel state-panel--error" role="alert">
      <p>{{ loadingError }}</p>
      <UiButton variant="secondary" @click="loadAccounts">
        <RefreshCw :size="17" aria-hidden="true" />
        {{ t('common.actions.reload') }}
      </UiButton>
    </div>
    <div v-else class="account-list">
      <article v-for="account in accounts" :key="account.id" class="account-card">
        <div class="account-card__identity">
          <span class="avatar" aria-hidden="true">{{ account.username.slice(0, 1).toUpperCase() }}</span>
          <div class="account-card__identity-body">
            <div v-if="editingAccountId === account.id" class="inline-editor">
              <label :for="`username-${account.id}`">{{ t('system.users.username') }}</label>
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
                  :title="t('system.users.saveUsername')"
                  :disabled="
                    updatingAccountId === account.id ||
                    !editingUsername.trim() ||
                    Boolean(editingUsernameError)
                  "
                  @click="saveAccount(account)"
                >
                  <Save :size="17" aria-hidden="true" />
                  <span>{{ t('common.actions.save') }}</span>
                </button>
                <button
                  type="button"
                  :title="t('system.users.cancelEdit')"
                  :disabled="updatingAccountId === account.id"
                  @click="cancelEditing"
                >
                  <X :size="17" aria-hidden="true" />
                  <span>{{ t('common.actions.cancel') }}</span>
                </button>
              </div>
              <small v-if="editingUsernameError">{{ editingUsernameError }}</small>
            </div>
            <div v-else class="account-card__name">
              <h3>{{ account.username }}</h3>
              <span v-if="account.id === auth.user?.id">{{ t('system.users.currentAccount') }}</span>
              <button
                v-if="canUpdate && account.id !== auth.user?.id"
                type="button"
                :title="t('system.users.editUser')"
                @click="startEditing(account)"
              >
                <Pencil :size="15" aria-hidden="true" />
                <span>{{ t('system.users.edit') }}</span>
              </button>
            </div>
            <p>{{ account.systemRoles.join(' · ') || t('system.users.noSystemRole') }}</p>
          </div>
        </div>
        <dl class="account-card__meta">
          <div><dt>{{ t('system.users.passwordChange') }}</dt><dd>{{ t(account.mustChangePassword ? 'system.users.required' : 'system.users.completed') }}</dd></div>
          <div><dt>{{ t('system.users.lastLogin') }}</dt><dd>{{ formatDate(account.lastLoginAt) }}</dd></div>
        </dl>
        <label class="status-field">
          <span>{{ t('system.users.status') }}</span>
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
        <p>{{ t('system.users.empty') }}</p>
      </div>
    </div>

    <UiActionDialog
      :open="Boolean(credentialPrompt)"
      :title="t('system.users.successTitle')"
      :description="credentialPrompt ? t('system.users.credentialDescription', { username: credentialPrompt.username }) : ''"
      :close-label="t('common.actions.close')"
      @close="closeCredentialDialog"
    >
      <template v-if="credentialPrompt">
        <p class="credential-hint">{{ t('system.users.passwordOnce') }}</p>
        <div class="credential-value">
          <code>{{ credentialPrompt.initialPassword }}</code>
          <button type="button" @click="copyCredentials">
            <component :is="copied ? Check : Clipboard" :size="16" aria-hidden="true" />
            {{ t(copied ? 'system.users.copied' : 'system.users.copyCredentials') }}
          </button>
        </div>
      </template>
    </UiActionDialog>
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
