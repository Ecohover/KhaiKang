<script setup lang="ts">
import { List } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import type { TestWorkspaceResponse } from '../api/contracts'
import ResourcePageHeader from './ResourcePageHeader.vue'
import SharedBreadcrumb from './SharedBreadcrumb.vue'
import SharedViewTabs from './SharedViewTabs.vue'

defineProps<{
  workspace: TestWorkspaceResponse
  activeSection: 'plans' | 'runs'
}>()

const { t } = useI18n()
</script>

<template>
  <section class="workspace-section-frame">
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'test-workspaces' }"
      :back-label="t('shell.navigation.backToWorkspaces')"
      :items="[
        { label: t('shell.navigation.workspaceList'), to: { name: 'test-workspaces' } },
        { label: workspace.name, to: { name: 'test-home', params: { workspaceId: workspace.id } } },
        { label: activeSection === 'plans' ? t('tests.plan.title') : t('tests.run.title'), active: true },
      ]"
    />
    <ResourcePageHeader
      :meta="`${workspace.prefix} · TEST WORKSPACE · ${workspace.currentUserRole}`"
      :title="workspace.name"
      :subtitle="workspace.description || t('tests.workspace.defaultDescription')"
      :status="workspace.status"
    >
      <slot name="action" />
    </ResourcePageHeader>
    <SharedViewTabs
      model-value="list"
      :tabs="[
        { key: 'list', label: t('common.views.list'), icon: List },
      ]"
    />
    <slot />
  </section>
</template>

<style scoped>
.workspace-section-frame{display:grid;width:100%;gap:20px}
</style>
