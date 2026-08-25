<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Bot, Clipboard, KeyRound, Plus, RefreshCw, ShieldCheck } from '@lucide/vue'
import { UiActionDialog, UiButton, UiCreateActions, UiStatusBadge } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders, type ApiResult } from '../api/client'
import type { AccountResponse, AiAccountResponse, AiFixedRole } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'
import { useAuthStore } from '../stores/auth'
import { aiAccountActions } from '../features/aiAccounts/aiAccountAccess'
import { showTokenCreatedNotice, showTokenRecreatedNotice } from '../features/aiAccounts/tokenMutationNotice'

const auth = useAuthStore()
const { t, d } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()
const accounts = ref<AiAccountResponse[]>([])
const humanAccounts = ref<AccountResponse[]>([])
const transferOwners = ref<Record<string, string>>({})
const loading = ref(true)
const savingId = ref('')
const error = ref('')
const creating = ref(false)
const username = ref('ai-')
const fixedRole = ref<AiFixedRole>('development_ai')
const credentialDisclosure = ref<{ username: string, initialPassword: string }>()
const tokenDisclosure = ref<{ accountName: string, token: string }>()
const disclosureCopied = ref(false)

const isAdministrator = computed(() => auth.user?.systemRoles.includes('System Admin') ?? false)
const canCreate = computed(() => auth.user?.systemPermissions.includes('ai_account.create') ?? false)
const activeHumanAccounts = computed(() => humanAccounts.value.filter((account) => account.accountType === 'human' && account.status === 'active'))
const roles: AiFixedRole[] = ['pm_ai', 'development_ai', 'qa_ai', 'reviewer_ai', 'devops_ai']

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const [aiResult, humanResult] = await Promise.all([
      apiClient.listAiAccounts(),
      isAdministrator.value
        ? apiClient.listAccounts()
        : Promise.resolve<ApiResult<AccountResponse[]>>({ data: [] }),
    ])
    accounts.value = aiResult.data ?? []
    humanAccounts.value = humanResult.data ?? []
    error.value = problemMessage(aiResult.error ?? humanResult.error, '')
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    loading.value = false
  }
}

function replaceAccount(updated: AiAccountResponse): void {
  accounts.value = accounts.value.map((account) => account.id === updated.id ? updated : account)
}

async function createAccount(continueCreating: boolean): Promise<void> {
  if (!canCreate.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.createAiAccount({ username: username.value.trim(), fixedRole: fixedRole.value }, await secureHeaders())
    if (!result.data) {
      error.value = problemMessage(result.error, t('system.aiAccounts.errors.create'))
      return
    }
    accounts.value = [...accounts.value, result.data.account].sort((left, right) => left.username.localeCompare(right.username))
    credentialDisclosure.value = { username: result.data.account.username, initialPassword: result.data.initialPassword }
    showCreated(t('system.aiAccounts.record'), result.data.account.username)
    if (continueCreating) {
      username.value = 'ai-'
      fixedRole.value = 'development_ai'
    }
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    creating.value = false
  }
}

async function setApiEnabled(account: AiAccountResponse, enabled: boolean): Promise<void> {
  savingId.value = account.id
  error.value = ''
  try {
    const result = await apiClient.setAiAccountApiEnabled(account.id, { enabled, version: account.version }, await secureHeaders())
    if (result.data) {
      replaceAccount(result.data)
      showUpdated(t('system.aiAccounts.record'), account.username)
    } else error.value = problemMessage(result.error, t('system.aiAccounts.errors.update'))
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    savingId.value = ''
  }
}

async function discloseToken(account: AiAccountResponse, recreate: boolean): Promise<void> {
  const user = auth.user
  if (!user || !aiAccountActions(account, user).canHandleTokenPlaintext) return
  if (recreate && !window.confirm(t('system.aiAccounts.confirmRecreate'))) return
  savingId.value = account.id
  disclosureCopied.value = false
  error.value = ''
  try {
    const result = recreate || !account.hasActiveToken
      ? await apiClient.createOrRecreateAiAccountToken(account.id, await secureHeaders())
      : await apiClient.viewAiAccountToken(account.id)
    if (result.data) {
      tokenDisclosure.value = { accountName: account.username, token: result.data.token }
      if (recreate) {
        showTokenRecreatedNotice(account.username, t)
      } else if (!account.hasActiveToken) {
        replaceAccount({ ...account, hasActiveToken: true })
        showTokenCreatedNotice(account.username, t)
      }
    } else error.value = problemMessage(result.error, t('system.aiAccounts.errors.token'))
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    savingId.value = ''
  }
}

async function revokeToken(account: AiAccountResponse): Promise<void> {
  const user = auth.user
  if (!user || !aiAccountActions(account, user).canRevokeToken || !window.confirm(t('system.aiAccounts.confirmRevoke'))) return
  savingId.value = account.id
  error.value = ''
  try {
    const result = await apiClient.revokeAiAccountToken(account.id, await secureHeaders())
    if (!result.error) {
      replaceAccount({ ...account, hasActiveToken: false })
      showUpdated(t('system.aiAccounts.record'), account.username)
    } else error.value = problemMessage(result.error, t('system.aiAccounts.errors.token'))
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    savingId.value = ''
  }
}

async function transferOwner(account: AiAccountResponse): Promise<void> {
  const ownerAccountId = transferOwners.value[account.id]
  const user = auth.user
  if (!ownerAccountId || !user || !aiAccountActions(account, user).canTransferOwner || !window.confirm(t('system.aiAccounts.confirmTransfer'))) return
  savingId.value = account.id
  error.value = ''
  try {
    const result = await apiClient.transferAiAccountOwner(account.id, { ownerAccountId, version: account.version }, await secureHeaders())
    if (result.data) {
      replaceAccount(result.data)
      transferOwners.value[account.id] = ''
      showUpdated(t('system.aiAccounts.record'), account.username)
    } else error.value = problemMessage(result.error, t('system.aiAccounts.errors.transfer'))
  } catch {
    error.value = t('system.aiAccounts.errors.connection')
  } finally {
    savingId.value = ''
  }
}

async function copyDisclosure(value: string): Promise<void> {
  try {
    await navigator.clipboard.writeText(value)
    disclosureCopied.value = true
  } catch {
    error.value = t('system.aiAccounts.errors.copy')
  }
}

function closeTokenDisclosure(): void {
  tokenDisclosure.value = undefined
  disclosureCopied.value = false
}

function closeCredentialDisclosure(): void {
  credentialDisclosure.value = undefined
  disclosureCopied.value = false
}

onMounted(load)
</script>

<template>
  <section class="ai-page">
    <header class="page-header"><div><p>{{ t('system.aiAccounts.eyebrow') }}</p><h2>{{ t('system.aiAccounts.title') }}</h2><span>{{ t('system.aiAccounts.description') }}</span></div><UiButton variant="secondary" :loading="loading" @click="load"><RefreshCw :size="16" />{{ t('common.actions.reload') }}</UiButton></header>
    <p v-if="error" class="error" role="alert">{{ error }}</p>

    <form v-if="canCreate" class="create-card" @submit.prevent="createAccount(false)">
      <header><Plus :size="20" /><div><h3>{{ t('system.aiAccounts.create') }}</h3><p>{{ t('system.aiAccounts.createDescription') }}</p></div></header>
      <label><span>{{ t('system.aiAccounts.username') }}</span><input v-model="username" required pattern="ai-[A-Za-z0-9._-]+" maxlength="100" :disabled="creating" /></label>
      <label><span>{{ t('system.aiAccounts.fixedRole') }}</span><select v-model="fixedRole" :disabled="creating"><option v-for="role in roles" :key="role" :value="role">{{ t(`system.aiAccounts.roles.${role}`) }}</option></select></label>
      <UiCreateActions :loading="creating" :disabled="!/^ai-[A-Za-z0-9._-]+$/.test(username)" :cancel-label="t('common.actions.cancel')" :create-label="t('system.aiAccounts.create')" :continue-label="t('common.actions.createAndContinue')" @cancel="username = 'ai-'" @create="createAccount(false)" @create-continue="createAccount(true)" />
    </form>

    <div v-if="loading" class="state">{{ t('system.aiAccounts.loading') }}</div>
    <div v-else-if="!accounts.length" class="state">{{ t('system.aiAccounts.empty') }}</div>
    <div v-else class="account-list">
      <article v-for="account in accounts" :key="account.id" class="account-card">
        <header><div class="identity"><Bot :size="21" /><div><h3>{{ account.username }}</h3><p>{{ t(`system.aiAccounts.roles.${account.fixedRole}`) }} · {{ t('system.aiAccounts.owner', { owner: account.ownerDisplayName }) }}</p></div></div><UiStatusBadge :variant="account.aiApiEnabled ? 'success' : 'inactive'">{{ t(account.aiApiEnabled ? 'system.aiAccounts.enabled' : 'system.aiAccounts.disabled') }}</UiStatusBadge></header>
        <dl><div><dt>{{ t('system.aiAccounts.token') }}</dt><dd>{{ t(account.hasActiveToken ? 'system.aiAccounts.tokenActive' : 'system.aiAccounts.tokenAbsent') }}</dd></div><div><dt>{{ t('system.aiAccounts.updatedAt') }}</dt><dd>{{ d(new Date(account.updatedAt), 'medium') }}</dd></div></dl>
        <div v-if="auth.user" class="account-actions">
          <UiButton v-if="aiAccountActions(account, auth.user).canSetApiEnabled" variant="secondary" :loading="savingId === account.id" @click="setApiEnabled(account, !account.aiApiEnabled)">{{ t(account.aiApiEnabled ? 'system.aiAccounts.disable' : 'system.aiAccounts.enable') }}</UiButton>
          <template v-if="aiAccountActions(account, auth.user).canHandleTokenPlaintext">
            <UiButton v-if="account.aiApiEnabled && !account.hasActiveToken" :loading="savingId === account.id" @click="discloseToken(account, false)"><KeyRound :size="16" />{{ t('system.aiAccounts.createToken') }}</UiButton>
            <UiButton v-if="account.hasActiveToken" variant="secondary" :loading="savingId === account.id" @click="discloseToken(account, false)">{{ t('system.aiAccounts.viewToken') }}</UiButton>
            <UiButton v-if="account.aiApiEnabled && account.hasActiveToken" variant="secondary" :loading="savingId === account.id" @click="discloseToken(account, true)">{{ t('system.aiAccounts.recreateToken') }}</UiButton>
          </template>
          <UiButton v-if="account.hasActiveToken && aiAccountActions(account, auth.user).canRevokeToken" variant="secondary" :loading="savingId === account.id" @click="revokeToken(account)">{{ t('system.aiAccounts.revokeToken') }}</UiButton>
        </div>
        <div v-if="auth.user && aiAccountActions(account, auth.user).canTransferOwner" class="transfer-owner">
          <label><span>{{ t('system.aiAccounts.transferOwner') }}</span><select v-model="transferOwners[account.id]" :disabled="savingId === account.id"><option value="">{{ t('system.aiAccounts.selectOwner') }}</option><option v-for="owner in activeHumanAccounts" :key="owner.id" :value="owner.id">{{ owner.username }}</option></select></label>
          <UiButton variant="secondary" :disabled="!transferOwners[account.id]" :loading="savingId === account.id" @click="transferOwner(account)">{{ t('system.aiAccounts.transfer') }}</UiButton>
        </div>
      </article>
    </div>
  </section>

  <UiActionDialog :open="Boolean(tokenDisclosure)" :title="t('system.aiAccounts.tokenTitle')" :description="tokenDisclosure ? t('system.aiAccounts.tokenDescription', { account: tokenDisclosure.accountName }) : ''" :close-label="t('common.actions.close')" :close-on-backdrop="false" @close="closeTokenDisclosure">
    <div v-if="tokenDisclosure" class="secret-disclosure"><ShieldCheck :size="20" /><code>{{ tokenDisclosure.token }}</code><p>{{ t('system.aiAccounts.tokenSecurity') }}</p></div>
    <template #actions><UiButton v-if="tokenDisclosure" variant="secondary" @click="copyDisclosure(tokenDisclosure.token)"><Clipboard :size="16" />{{ t(disclosureCopied ? 'system.aiAccounts.copied' : 'system.aiAccounts.copyToken') }}</UiButton><UiButton @click="closeTokenDisclosure">{{ t('common.actions.close') }}</UiButton></template>
  </UiActionDialog>

  <UiActionDialog :open="Boolean(credentialDisclosure)" :title="t('system.aiAccounts.credentialsTitle')" :description="credentialDisclosure ? t('system.aiAccounts.credentialsDescription', { account: credentialDisclosure.username }) : ''" :close-label="t('common.actions.close')" :close-on-backdrop="false" @close="closeCredentialDisclosure">
    <div v-if="credentialDisclosure" class="secret-disclosure"><code>{{ credentialDisclosure.initialPassword }}</code><p>{{ t('system.aiAccounts.credentialsSecurity') }}</p></div>
    <template #actions><UiButton v-if="credentialDisclosure" variant="secondary" @click="copyDisclosure(`${credentialDisclosure.username}\n${credentialDisclosure.initialPassword}`)"><Clipboard :size="16" />{{ t(disclosureCopied ? 'system.aiAccounts.copied' : 'system.aiAccounts.copyCredentials') }}</UiButton><UiButton @click="closeCredentialDisclosure">{{ t('common.actions.close') }}</UiButton></template>
  </UiActionDialog>
</template>

<style scoped>
.ai-page{display:grid;max-width:1000px;gap:18px;margin:0 auto}.page-header,.page-header>div,.create-card>header,.account-card>header,.identity,.account-actions,.transfer-owner{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-header>div{display:block}.page-header p{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;text-transform:uppercase;letter-spacing:.08em}.page-header h2{margin:4px 0}.page-header span,.create-card p,.identity p{color:var(--kk-text-muted)}.create-card,.account-card{display:grid;gap:16px;padding:20px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.create-card>header{justify-content:flex-start;align-items:flex-start}.create-card h3,.create-card p,.identity h3,.identity p{margin:0}.create-card label,.transfer-owner label{display:grid;min-width:0;flex:1;gap:6px;font-size:.82rem;font-weight:650}.create-card input,.create-card select,.transfer-owner select{width:100%;padding:9px 10px;background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:6px;font:inherit;box-sizing:border-box}.account-list{display:grid;gap:12px}.identity{justify-content:flex-start}.account-card dl{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin:0}.account-card dl div{display:grid;gap:3px;padding:10px;background:var(--kk-surface-subtle);border-radius:6px}.account-card dt{color:var(--kk-text-muted);font-size:.72rem}.account-card dd{margin:0;font-size:.84rem;font-weight:650}.account-actions{justify-content:flex-start;flex-wrap:wrap}.transfer-owner{padding-top:12px;border-top:1px solid var(--kk-border)}.state,.error{padding:24px;text-align:center;border-radius:6px;background:var(--kk-surface-subtle)}.error{color:var(--kk-danger);background:#fff1f0}.secret-disclosure{display:grid;gap:10px}.secret-disclosure code{padding:10px;overflow-wrap:anywhere;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:6px;user-select:all}.secret-disclosure p{margin:0;color:var(--kk-text-muted);font-size:.82rem}@media(max-width:680px){.page-header,.account-card>header,.transfer-owner{align-items:stretch;flex-direction:column}.account-card dl{grid-template-columns:1fr}.account-actions>*{width:100%}}
</style>
