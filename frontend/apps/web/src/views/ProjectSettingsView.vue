<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Save } from '@lucide/vue'
import { useRoute } from 'vue-router'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ProjectResponse } from '../api/contracts'
import { PROJECT_DEACTIVATE_PERMISSION, PROJECT_UPDATE_PERMISSION } from '../navigation'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const auth = useAuthStore()
const project = ref<ProjectResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const saved = ref(false)
const name = ref('')
const description = ref('')
const status = ref<'active' | 'inactive'>('active')

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
      error.value = problemMessage(result.error, '找不到專案，或你沒有檢視權限。')
      return
    }
    project.value = result.data
    name.value = result.data.name
    description.value = result.data.description ?? ''
    status.value = result.data.status
  } catch {
    error.value = '無法連線到伺服器，請稍後再試。'
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
      error.value = problemMessage(result.error, '儲存失敗，請重新載入後再試。')
      return
    }
    project.value = result.data
    saved.value = true
  } catch {
    error.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="settings-page">
    <header class="page-heading">
      <p>{{ project?.code }}</p>
      <h2>專案設定</h2>
      <span>{{ project?.name }}</span>
    </header>

    <p v-if="loading" class="page-state">正在載入專案設定…</p>
    <form v-else-if="project" class="settings-form" @submit.prevent="saveProject">
      <div class="settings-form__heading">
        <div>
          <h3>基本資料</h3>
          <p>調整專案名稱、說明與目前狀態。</p>
        </div>
        <span>版本 {{ project.version }}</span>
      </div>

      <UiField
        id="project-settings-name"
        v-model="name"
        label="專案名稱"
        :disabled="saving || !canEdit"
      />
      <label class="form-field">
        <span>專案代號</span>
        <input :value="project.code" disabled />
        <small>專案代號建立後不可修改。</small>
      </label>
      <label class="form-field">
        <span>專案說明</span>
        <textarea
          v-model="description"
          rows="7"
          maxlength="4000"
          :disabled="saving || !canEdit"
        />
      </label>
      <label class="form-field">
        <span>專案狀態</span>
        <select v-model="status" :disabled="saving || !canEdit || !canChangeStatus">
          <option value="active">啟用中</option>
          <option value="inactive">已停用</option>
        </select>
        <small v-if="!canChangeStatus">需要 project.deactivate 系統權限才能變更狀態。</small>
      </label>

      <p v-if="error" class="form-error" role="alert">{{ error }}</p>
      <p v-if="saved" class="form-success" role="status">專案設定已儲存。</p>
      <div class="settings-form__actions">
        <span v-if="!canEdit">你沒有修改專案設定的權限。</span>
        <UiButton
          v-if="canEdit"
          type="submit"
          :loading="saving"
          :disabled="!name.trim()"
        >
          <Save :size="17" aria-hidden="true" />
          儲存設定
        </UiButton>
      </div>
    </form>
    <div v-else class="page-state page-state--error" role="alert">
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadProject">重新載入</UiButton>
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
