<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { useSaveNotice } from '../composables/useSaveNotice'

const router = useRouter()
const { t } = useI18n()
const name = ref('')
const prefix = ref('')
const description = ref('')
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()
const prefixError = computed(() => {
  if (!prefix.value) return ''
  return /^[A-Za-z][A-Za-z0-9]{1,9}$/.test(prefix.value)
    ? ''
    : t('tests.workspace.prefixInvalid')
})

async function create(continueCreating: boolean): Promise<void> {
  if (!name.value.trim() || prefixError.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.createTestWorkspace({
      name: name.value.trim(),
      prefix: prefix.value.trim() || null,
      description: description.value.trim() || null,
    }, await secureHeaders())
    if (result.data) {
      showCreated(t('tests.workspace.createdRecord'), result.data.name)
      if (continueCreating) {
        name.value = ''
        prefix.value = ''
        description.value = ''
        await nextTick()
        document.getElementById('test-workspace-name')?.focus()
      } else {
        await router.push({ name: 'test-suites', params: { workspaceId: result.data.id } })
      }
    } else {
      error.value = result.error?.code === 'workspace_prefix_conflict'
        ? t('tests.workspace.prefixConflict')
        : problemMessage(result.error, t('tests.workspace.createFailed'))
    }
  } catch {
    error.value = t('tests.workspace.connectionFailed')
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <section class="create-page">
    <button class="back-link" type="button" @click="router.push({ name: 'test-workspaces' })">
      <ArrowLeft :size="16" />{{ t('tests.workspace.backToList') }}
    </button>
    <header>
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ t('tests.workspace.createTitle') }}</h2>
      <span>{{ t('tests.workspace.createDescription') }}</span>
    </header>
    <form class="create-form" @submit.prevent="create(false)">
      <UiField id="test-workspace-name" v-model="name" :label="t('tests.workspace.name')" :disabled="creating" />
      <UiField
        id="test-workspace-prefix"
        v-model="prefix"
        :label="t('tests.workspace.prefix')"
        :placeholder="t('tests.workspace.prefixPlaceholder')"
        :error="prefixError"
        :disabled="creating"
      />
      <small class="field-hint">{{ t('tests.workspace.prefixHint') }}</small>
      <label>
        <span>{{ t('tests.workspace.descriptionLabel') }}</span>
        <textarea v-model="description" rows="7" maxlength="4000" :disabled="creating" />
      </label>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <UiCreateActions
        :loading="creating"
        :disabled="!name.trim() || Boolean(prefixError)"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('tests.workspace.create')"
        :continue-label="t('tests.workspace.createAndContinue')"
        @cancel="router.push({ name: 'test-workspaces' })"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </form>
  </section>
</template>

<style scoped>
.create-page{display:grid;max-width:900px;gap:22px;margin:0 auto}.back-link{display:flex;width:fit-content;align-items:center;gap:6px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}.create-page header h2{margin:3px 0 7px;font-size:clamp(1.65rem,3vw,2.2rem)}.create-page header span{color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.create-form{display:grid;gap:22px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.create-form label{display:grid;gap:7px;font-size:.875rem;font-weight:650}.create-form textarea{padding:11px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:var(--kk-radius);font:inherit}.field-hint{margin-top:-16px;color:var(--kk-text-muted)}.error{color:var(--kk-danger)}
</style>
