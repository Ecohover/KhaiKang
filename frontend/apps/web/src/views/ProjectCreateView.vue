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
const code = ref('')
const name = ref('')
const description = ref('')
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()
const codeError = computed(() => {
  if (!code.value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$/.test(code.value) ? '' : t('projects.create.codeInvalid')
})
const disabled = computed(() => !code.value.trim() || !name.value.trim() || Boolean(codeError.value))

async function create(continueCreating: boolean): Promise<void> {
  if (disabled.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.createProject({
      code: code.value.trim(),
      name: name.value.trim(),
      description: description.value.trim() || null,
    }, await secureHeaders())
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.create.failed'))
      return
    }
    showCreated(t('projects.record'), result.data.code)
    if (continueCreating) {
      code.value = ''
      name.value = ''
      description.value = ''
      await nextTick()
      document.getElementById('project-code')?.focus()
    } else {
      await router.push({ name: 'project-detail', params: { projectId: result.data.id } })
    }
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <section class="create-page">
    <button class="back-link" type="button" @click="router.push({ name: 'projects' })">
      <ArrowLeft :size="16" />{{ t('projects.create.back') }}
    </button>
    <header>
      <p class="eyebrow">{{ t('projects.management') }}</p>
      <h2>{{ t('projects.create.title') }}</h2>
      <span>{{ t('projects.create.description') }}</span>
    </header>
    <form class="create-form" @submit.prevent="create(false)">
      <div class="fields">
        <UiField
          id="project-code"
          v-model="code"
          :label="t('projects.create.code')"
          :disabled="creating"
          :error="codeError"
        />
        <UiField id="project-name" v-model="name" :label="t('projects.create.name')" :disabled="creating" />
        <label>
          <span>{{ t('projects.create.descriptionLabel') }}</span>
          <textarea v-model="description" rows="7" maxlength="4000" :disabled="creating" />
        </label>
      </div>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <UiCreateActions
        :loading="creating"
        :disabled="disabled"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('projects.create.submit')"
        :continue-label="t('projects.create.submitAndContinue')"
        @cancel="router.push({ name: 'projects' })"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </form>
  </section>
</template>

<style scoped>
.create-page{display:grid;max-width:900px;gap:22px;margin:0 auto}.back-link{display:flex;width:fit-content;align-items:center;gap:6px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}.create-page header h2{margin:3px 0 7px;font-size:clamp(1.65rem,3vw,2.2rem)}.create-page header span{color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.create-form{display:grid;gap:22px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.fields{display:grid;grid-template-columns:minmax(180px,.7fr) minmax(240px,1.3fr);gap:18px}.fields label{display:grid;grid-column:1/-1;gap:7px;font-size:.875rem;font-weight:650}.fields textarea{padding:11px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:var(--kk-radius);font:inherit}.error{color:var(--kk-danger)}@media(max-width:620px){.fields{grid-template-columns:1fr}}
</style>
