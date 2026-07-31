<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { FolderKanban, Plus } from '@lucide/vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton } from '@khaikang/ui'
import { apiClient, problemMessage } from '../api/client'
import type { ProjectResponse } from '../api/contracts'
import SharedEntityCard from '../components/SharedEntityCard.vue'
import SharedEntityListPage from '../components/SharedEntityListPage.vue'
import { PROJECT_CREATE_PERMISSION } from '../navigation'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const { t, d } = useI18n()
const projects = ref<ProjectResponse[]>([])
const loading = ref(true)
const loadingError = ref('')
const viewMode = ref<'list' | 'grid'>('list')
const canCreate = computed(() => auth.user?.systemPermissions.includes(PROJECT_CREATE_PERMISSION) ?? false)

onMounted(loadProjects)

async function loadProjects(): Promise<void> {
  loading.value = true
  loadingError.value = ''
  try {
    const result = await apiClient.listProjects()
    if (result.error) loadingError.value = problemMessage(result.error, t('projects.list.loadFailed'))
    else projects.value = result.data ?? []
  } catch {
    loadingError.value = t('projects.detail.connectionError')
  } finally {
    loading.value = false
  }
}

function formatDate(value: string): string {
  return d(new Date(value), 'medium')
}
</script>

<template>
  <SharedEntityListPage
    v-model:view-mode="viewMode"
    :meta="t('projects.management')"
    :title="t('projects.list.title')"
    :description="t('projects.list.description')"
    :count-label="t('projects.list.count', { count: projects.length }, projects.length)"
    storage-key="khaikang.projects.view-mode"
    :group-label="t('common.viewMode.label')"
    :list-label="t('common.viewMode.list')"
    :grid-label="t('common.viewMode.grid')"
    :loading="loading"
    :loading-label="t('projects.list.loading')"
    :error="loadingError"
    :error-title="t('projects.list.loadFailed')"
    :reload-label="t('common.actions.reload')"
    :has-items="projects.length > 0"
    :empty-title="t('projects.list.emptyTitle')"
    :empty-description="t(canCreate ? 'projects.list.emptyCanCreate' : 'projects.list.emptyCannotCreate')"
    :empty-icon="FolderKanban"
    @reload="loadProjects"
  >
    <template #action>
      <UiButton v-if="canCreate" @click="router.push({ name: 'project-new' })">
        <Plus :size="18" aria-hidden="true" />
        {{ t('projects.list.create') }}
      </UiButton>
    </template>

    <SharedEntityCard
      v-for="project in projects"
        :key="project.id"
        :to="{ name: 'project-detail', params: { projectId: project.id } }"
      :icon="FolderKanban"
      :eyebrow="project.code"
      :title="project.name"
      :description="project.description || t('projects.list.noDescription')"
      :status="project.status"
      :status-label="t(`projects.detail.status.${project.status}`)"
      :view-mode="viewMode"
    >
      <template #meta>
          <span>{{ project.currentUserRoles.join(' · ') }}</span>
          <span>{{ t('projects.list.updatedAt', { date: formatDate(project.updatedAt) }) }}</span>
      </template>
    </SharedEntityCard>
  </SharedEntityListPage>
</template>
