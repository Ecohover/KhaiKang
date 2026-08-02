<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ClipboardCheck, Plus } from '@lucide/vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton } from '@khaikang/ui'
import { apiClient, problemMessage } from '../api/client'
import type { TestWorkspaceResponse } from '../api/contracts'
import SharedEntityCard from '../components/SharedEntityCard.vue'
import SharedEntityListPage from '../components/SharedEntityListPage.vue'

const workspaces = ref<TestWorkspaceResponse[]>([])
const router = useRouter()
const { t, d } = useI18n()
const loading = ref(true)
const error = ref('')
const viewMode = ref<'list' | 'grid'>('list')

async function load(): Promise<void> {
  loading.value = true
  const result = await apiClient.listTestWorkspaces()
  error.value = result.error ? problemMessage(result.error, t('tests.workspace.loadFailed')) : ''
  workspaces.value = result.data ?? []
  loading.value = false
}

function formatDate(value: string): string {
  return d(new Date(value), 'medium')
}

onMounted(load)
</script>

<template>
  <SharedEntityListPage
    v-model:view-mode="viewMode"
    :meta="t('tests.management')"
    :title="t('tests.workspace.title')"
    :description="t('tests.workspace.description')"
    :count-label="t('tests.workspace.count', { count: workspaces.length }, workspaces.length)"
    storage-key="khaikang.test-workspaces.view-mode"
    :group-label="t('common.viewMode.label')"
    :list-label="t('common.viewMode.list')"
    :grid-label="t('common.viewMode.grid')"
    :loading="loading"
    :loading-label="t('tests.workspace.loading')"
    :error="error"
    :error-title="t('tests.workspace.loadFailed')"
    :reload-label="t('common.actions.reload')"
    :has-items="workspaces.length > 0"
    :empty-title="t('tests.workspace.emptyTitle')"
    :empty-description="t('tests.workspace.emptyDescription')"
    :empty-icon="ClipboardCheck"
    @reload="load"
  >
    <template #action>
      <UiButton @click="router.push({ name: 'test-workspace-new' })">
        <Plus :size="18" aria-hidden="true" />
        {{ t('tests.workspace.create') }}
      </UiButton>
    </template>

    <SharedEntityCard
      v-for="workspace in workspaces"
        :key="workspace.id"
        :to="{ name: 'test-suites', params: { workspaceId: workspace.id } }"
      :icon="ClipboardCheck"
      :eyebrow="workspace.prefix"
      :title="workspace.name"
      :description="workspace.description || t('tests.workspace.noDescription')"
      :status="workspace.status"
      :status-label="t(`tests.workspace.${workspace.status}`)"
      :view-mode="viewMode"
    >
      <template #meta>
          <span>{{ workspace.currentUserRole }}</span>
          <span>{{ t('tests.workspace.updatedAt', { date: formatDate(workspace.updatedAt) }) }}</span>
      </template>
    </SharedEntityCard>
  </SharedEntityListPage>
</template>
