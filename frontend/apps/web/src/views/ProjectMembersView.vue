<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { List } from '@lucide/vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import ResourceMemberManager from '../components/ResourceMemberManager.vue'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import SharedViewTabs from '../components/SharedViewTabs.vue'
import { apiClient } from '../api/client'
import type { ProjectResponse } from '../api/contracts'
import {
  PROJECT_MEMBER_ADD_PERMISSION,
  PROJECT_MEMBER_REMOVE_PERMISSION,
  PROJECT_ROLE_ASSIGN_PERMISSION,
} from '../navigation'

const route = useRoute()
const { t } = useI18n()
const projectId = computed(() => String(route.params.projectId))
const project = ref<ProjectResponse>()

const canAddMember = computed(() =>
  project.value?.currentUserPermissions.includes(PROJECT_MEMBER_ADD_PERMISSION) === true &&
  project.value.currentUserPermissions.includes(PROJECT_ROLE_ASSIGN_PERMISSION),
)
const canAssignRoles = computed(() =>
  project.value?.currentUserPermissions.includes(PROJECT_ROLE_ASSIGN_PERMISSION) ?? false,
)
const canRemoveMember = computed(() =>
  project.value?.currentUserPermissions.includes(PROJECT_MEMBER_REMOVE_PERMISSION) ?? false,
)

onMounted(loadPage)

async function loadPage(): Promise<void> {
  const result = await apiClient.getProject(projectId.value)
  if (result.data) {
    project.value = result.data
  }
}
</script>

<template>
  <section class="members-page">
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'projects' }"
      :back-label="t('projects.create.back')"
      :items="[
        { label: t('projects.list.title'), to: { name: 'projects' } },
        { label: project?.name || t('projects.record'), to: { name: 'project-detail', params: { projectId } } },
        { label: t('projects.members.title'), active: true },
      ]"
    />

    <ResourcePageHeader
      v-if="project"
      :meta="`${project.code} · PROJECT`"
      :title="project.name"
      :subtitle="t('projects.members.title')"
      :status="project.status"
    />

    <!-- VIEW TABS (分頁標籤列) -->
    <SharedViewTabs
      model-value="list"
      :tabs="[
        { key: 'list', label: '列表', icon: List }
      ]"
    />

    <ResourceMemberManager
      resource-type="project"
      :resource-id="projectId"
      :title="t('projects.members.record')"
      :description="t('projects.members.sectionDescription', '管理本專案之成員權限與角色組')"
      :can-add="canAddMember"
      :can-edit-role="canAssignRoles"
      :can-remove="canRemoveMember"
    />
  </section>
</template>

<style scoped>
.members-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}
</style>
