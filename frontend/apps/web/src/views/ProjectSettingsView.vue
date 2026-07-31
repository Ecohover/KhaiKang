<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Save } from '@lucide/vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ProjectResponse } from '../api/contracts'
import { PROJECT_DEACTIVATE_PERMISSION, PROJECT_UPDATE_PERMISSION } from '../navigation'
import { useAuthStore } from '../stores/auth'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const { t } = useI18n()
const auth = useAuthStore()
const project = ref<ProjectResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const saved = ref(false)
const name = ref('')
const description = ref('')
const status = ref<'active' | 'inactive'>('active')
const { showUpdated } = useSaveNotice()

const canEdit = computed(() =>
  project.value?.currentUserPermissions.includes(PROJECT_UPDATE_PERMISSION) ?? false,
)
const canChangeStatus = computed(() =>
  auth.user?.systemPermissions.includes(PROJECT_DEACTIVATE_PERMISSION) ?? false,
)

onMounted(loadProject)

async function loadProject(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const result = await apiClient.getProject(String(route.params.projectId))
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.detail.loadError'))
      return
    }
    project.value = result.data
    name.value = result.data.name
    description.value = result.data.description ?? ''
    status.value = result.data.status
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    loading.value = false
  }
}

async function saveProject(): Promise<void> {
  if (!project.value || !name.value.trim()) return

  saving.value = true
  error.value = ''
  saved.value = false
  try {
    const result = await apiClient.updateProject(
      project.value.id,
      {
        name: name.value.trim(),
        description: description.value.trim() || null,
        status: status.value,
        version: project.value.version,
      },
      await secureHeaders(),
    )
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.settings.saveFailed'))
      return
    }
    project.value = result.data
    saved.value = true
    showUpdated(t('projects.record'), result.data.code)
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="settings-page">
    <header class="page-heading">
      <p>{{ project?.code }}</p>
      <h2>{{ t('projects.settings.title') }}</h2>
      <span>{{ project?.name }}</span>
    </header>

    <p v-if="loading" class="page-state">{{ t('projects.settings.loading') }}</p>
    <form v-else-if="project" class="settings-form" @submit.prevent="saveProject">
      <div class="settings-form__heading">
        <div>
          <h3>{{ t('projects.settings.sectionTitle') }}</h3>
          <p>{{ t('projects.settings.sectionDescription') }}</p>
        </div>
        <span>{{ t('projects.settings.version', { version: project.version }) }}</span>
      </div>

      <UiField
        id="project-settings-name"
        v-model="name"
        :label="t('projects.settings.name')"
        :disabled="saving || !canEdit"
      />
      <label class="form-field">
        <span>{{ t('projects.settings.code') }}</span>
        <input :value="project.code" disabled />
        <small>{{ t('projects.settings.codeImmutable') }}</small>
      </label>
      <label class="form-field">
        <span>{{ t('projects.settings.description') }}</span>
        <textarea
          v-model="description"
          rows="7"
          maxlength="4000"
          :disabled="saving || !canEdit"
        />
      </label>
      <label class="form-field">
        <span>{{ t('projects.settings.status') }}</span>
        <select v-model="status" :disabled="saving || !canEdit || !canChangeStatus">
          <option value="active">{{ t('projects.detail.status.active') }}</option>
          <option value="inactive">{{ t('projects.detail.status.inactive') }}</option>
        </select>
        <small v-if="!canChangeStatus">{{ t('projects.settings.statusPermission') }}</small>
      </label>

      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <p v-if="saved" class="form-success" role="status">{{ t('projects.settings.saved') }}</p>
      <div class="settings-form__actions">
        <span v-if="!canEdit">{{ t('projects.settings.noEditPermission') }}</span>
        <UiButton
          v-if="canEdit"
          type="submit"
          :loading="saving"
          :disabled="!name.trim()"
        >
          <Save :size="17" aria-hidden="true" />
          {{ t('projects.settings.save') }}
        </UiButton>
      </div>
    </form>
    <div v-else class="page-state page-state--error" role="alert">
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadProject">{{ t('common.actions.reload') }}</UiButton>
    </div>
  </section>
</template>

<style scoped>
.settings-page {
  display: grid;
  gap: 22px;
}

.page-heading p,
.page-heading h2,
.settings-form h3,
.settings-form p {
  margin: 0;
}

.page-heading p {
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 750;
  letter-spacing: 0.08em;
}

.page-heading h2 {
  font-size: 1.8rem;
}

.page-heading span,
.settings-form__heading p,
.settings-form__heading > span,
.form-field small,
.settings-form__actions > span {
  color: var(--kk-text-muted);
  font-size: 0.8rem;
}

.settings-form {
  display: grid;
  max-width: 780px;
  gap: 20px;
  padding: 24px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.settings-form__heading,
.settings-form__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.form-field {
  display: grid;
  gap: 7px;
  font-size: 0.875rem;
  font-weight: 650;
}

.form-field input,
.form-field textarea,
.form-field select {
  width: 100%;
  padding: 10px 11px;
  resize: vertical;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
}

.form-field input:disabled,
.form-field textarea:disabled,
.form-field select:disabled {
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
}

.form-error {
  color: var(--kk-danger);
}

.form-success {
  color: var(--kk-accent);
}

.page-state {
  padding: 42px 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.page-state--error {
  color: var(--kk-danger);
}
</style>
