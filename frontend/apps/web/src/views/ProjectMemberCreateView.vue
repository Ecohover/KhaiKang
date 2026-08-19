<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions } from '@khaikang/ui'
import ProjectMemberCreateFormFields from '../components/ProjectMemberCreateFormFields.vue'
import ResourceFormLayout from '../components/ResourceFormLayout.vue'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ApiProblem, ProjectResponse, ProjectRoleResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'
import {
  PROJECT_MEMBER_ADD_PERMISSION,
  PROJECT_ROLE_ASSIGN_PERMISSION,
} from '../navigation'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const projectId = computed(() => String(route.params.projectId))
const project = ref<ProjectResponse>()
const roles = ref<ProjectRoleResponse[]>([])
const username = ref('')
const roleCode = ref('')
const loading = ref(true)
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()

const canAddMember = computed(() =>
  project.value?.currentUserPermissions.includes(PROJECT_MEMBER_ADD_PERMISSION) === true &&
  project.value.currentUserPermissions.includes(PROJECT_ROLE_ASSIGN_PERMISSION),
)
const disabled = computed(() => !username.value.trim() || !roleCode.value || !canAddMember.value)

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [projectResult, roleResult] = await Promise.all([
    apiClient.getProject(projectId.value),
    apiClient.listProjectRoles(projectId.value),
  ])
  project.value = projectResult.data
  roles.value = roleResult.data ?? []
  if (!roleCode.value && roles.value[0]) roleCode.value = roles.value[0].code
  if (!project.value || !roleResult.data) {
    error.value = problemMessage(projectResult.error ?? roleResult.error, t('projects.members.loadFailed'))
  }
  loading.value = false
}

function addMemberError(problem: unknown): string {
  const code = (problem as ApiProblem | undefined)?.code
  if (code === 'project_member_account_not_found') {
    return t('projects.members.create.accountNotFound', { username: username.value.trim() })
  }
  return problemMessage(problem, t('projects.members.addFailed'))
}

async function create(continueCreating: boolean): Promise<void> {
  if (disabled.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.addProjectMember(
      projectId.value,
      { username: username.value.trim(), roleCodes: [roleCode.value] },
      await secureHeaders(),
    )
    if (!result.data) {
      error.value = addMemberError(result.error)
      return
    }

    showCreated(t('projects.members.record'), result.data.username)
    if (continueCreating) {
      username.value = ''
      await nextTick()
      document.getElementById('project-member-username')?.focus()
    } else {
      await router.push({ name: 'project-members', params: { projectId: projectId.value } })
    }
  } catch {
    error.value = t('projects.members.connectionFailed')
  } finally {
    creating.value = false
  }
}

onMounted(load)
</script>

<template>
  <ResourceFormLayout
    :back-to="{ name: 'project-members', params: { projectId } }"
    :back-label="t('projects.members.create.back')"
    :meta="project ? `${project.code} · PROJECT` : t('projects.management')"
    :title="t('projects.members.create.title')"
    :description="t('projects.members.create.description')"
    :loading="loading"
    :loading-label="t('projects.members.loading')"
    :error="!canAddMember && project ? t('projects.members.addFailed') : error"
    :show-actions="Boolean(project)"
  >
    <form v-if="project" @submit.prevent="create(false)">
      <ProjectMemberCreateFormFields
        v-model:username="username"
        v-model:role-code="roleCode"
        :roles="roles"
        :disabled="creating || !canAddMember"
        :labels="{ sectionTitle: t('projects.members.create.basicTitle'), sectionDescription: t('projects.members.create.basicDescription'), username: t('projects.members.username'), usernamePlaceholder: t('projects.members.usernamePlaceholder'), roles: t('projects.members.roles') }"
      />
    </form>
    <template #actions>
      <UiCreateActions
        :loading="creating"
        :disabled="disabled"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('projects.members.create.submit')"
        :continue-label="t('projects.members.create.submitAndContinue')"
        @cancel="router.push({ name: 'project-members', params: { projectId } })"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </template>
  </ResourceFormLayout>
</template>
