<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { List } from '@lucide/vue'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import SharedResourceSettings from '../components/SharedResourceSettings.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import SharedViewTabs from '../components/SharedViewTabs.vue'
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
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    loading.value = false
  }
}

async function handleSaveSettings(payload: {
  name: string
  description: string
  status: 'active' | 'inactive'
}): Promise<void> {
  if (!project.value) return

  saving.value = true
  error.value = ''
  saved.value = false
  try {
    const result = await apiClient.updateProject(
      project.value.id,
      {
        name: payload.name,
        description: payload.description || null,
        status: payload.status,
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
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'projects' }"
      :back-label="t('projects.create.back')"
      :items="[
        { label: t('projects.list.title'), to: { name: 'projects' } },
        { label: project?.name || t('projects.record'), to: { name: 'project-detail', params: { projectId: String(route.params.projectId) } } },
        { label: t('projects.settings.title'), active: true },
      ]"
    />

    <SharedStateBanner v-if="loading" type="loading" :title="t('projects.settings.loading')" />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="t('projects.detail.loadError')"
      :description="error"
      show-reload
      @reload="loadProject"
    />

    <template v-else-if="project">
      <ResourcePageHeader
        :meta="`${project.code} · PROJECT`"
        :title="project.name"
        :subtitle="t('projects.settings.title')"
        :status="project.status"
      />

      <!-- VIEW TABS (分頁標籤列) -->
      <SharedViewTabs
        model-value="settings"
        :tabs="[
          { key: 'settings', label: '列表', icon: List }
        ]"
      />

      <SharedResourceSettings
        :title="t('projects.settings.title')"
        :section-description="t('projects.settings.sectionDescription')"
        :version="project.version"
        :name="project.name"
        :code-or-prefix="project.code"
        :code-label="t('projects.settings.code')"
        :description="project.description ?? ''"
        :status="project.status"
        :can-edit="canEdit"
        :can-change-status="canChangeStatus"
        :loading="loading"
        :saving="saving"
        :error="error"
        :saved="saved"
        @save="handleSaveSettings"
      />
    </template>
  </section>
</template>

<style scoped>
.settings-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}
</style>
