<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions, UiField } from '@khaikang/ui'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
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
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'projects' }"
      :back-label="t('projects.create.back')"
      :items="[
        { label: t('projects.list.title'), to: { name: 'projects' } },
        { label: t('projects.create.title'), active: true },
      ]"
    />

    <form class="form-card" @submit.prevent="create(false)">
      <header>
        <h2>{{ t('projects.create.title') }}</h2>
        <p>{{ t('projects.create.description') }}</p>
      </header>

      <p v-if="error" class="error-banner">{{ error }}</p>

      <div class="field-grid">
        <UiField
          id="project-code"
          v-model="code"
          :label="t('projects.create.code')"
          required
          :disabled="creating"
          :error="codeError"
        />
        <UiField
          id="project-name"
          v-model="name"
          :label="t('projects.create.name')"
          required
          :disabled="creating"
        />
        <UiField
          id="project-description"
          v-model="description"
          :label="t('projects.create.descriptionLabel')"
          multiline
          :disabled="creating"
        />
      </div>

      <UiCreateActions
        :loading="creating"
        :disabled="disabled"
        :create-label="t('projects.create.submit')"
        :continue-label="t('projects.create.submitAndContinue')"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </form>
  </section>
</template>

<style scoped>
.create-page {
  display: grid;
  max-width: 960px;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}

.form-card {
  display: grid;
  gap: 20px;
  background: white;
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  padding: 24px;
  width: 100%;
  box-sizing: border-box;
}

.form-card :deep(.ui-create-actions) {
  position: sticky;
  bottom: 0;
  justify-content: flex-end;
  padding: 14px 0 0;
  background: var(--kk-surface);
  border-top: 1px solid var(--kk-border);
}

.form-card header h2 {
  margin: 0 0 4px;
  font-size: 1.4rem;
}
.form-card header p {
  margin: 0;
  color: var(--kk-text-muted);
  font-size: 0.88rem;
}

.error-banner {
  padding: 10px 14px;
  background: #fef2f2;
  color: #dc2626;
  border-radius: 6px;
  font-size: 0.85rem;
}

.field-grid {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
