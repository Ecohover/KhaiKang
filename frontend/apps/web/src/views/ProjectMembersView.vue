<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Save, Trash2, UserPlus, Users } from '@lucide/vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ProjectMemberResponse, ProjectResponse, ProjectRoleResponse } from '../api/contracts'
import {
  PROJECT_MEMBER_ADD_PERMISSION,
  PROJECT_MEMBER_REMOVE_PERMISSION,
  PROJECT_ROLE_ASSIGN_PERMISSION,
} from '../navigation'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const { t, d } = useI18n()
const projectId = computed(() => String(route.params.projectId))
const project = ref<ProjectResponse>()
const members = ref<ProjectMemberResponse[]>([])
const roles = ref<ProjectRoleResponse[]>([])
const memberRoles = ref<Record<string, string[]>>({})
const loading = ref(true)
const error = ref('')
const username = ref('')
const newMemberRoleCodes = ref<string[]>(['contributor'])
const addingMember = ref(false)
const savingMemberId = ref('')
const removingMemberId = ref('')
const { showCreated, showUpdated } = useSaveNotice()

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
  loading.value = true
  error.value = ''
  try {
    const [projectResult, memberResult, roleResult] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.listProjectMembers(projectId.value),
      apiClient.listProjectRoles(projectId.value),
    ])
    if (!projectResult.data || !memberResult.data || !roleResult.data) {
      error.value = problemMessage(
        projectResult.error ?? memberResult.error ?? roleResult.error,
        t('projects.members.loadFailed'),
      )
      return
    }

    project.value = projectResult.data
    members.value = memberResult.data
    roles.value = roleResult.data
    memberRoles.value = Object.fromEntries(
      members.value.map((member) => [member.id, [...member.roleCodes]]),
    )
  } catch {
    error.value = t('projects.members.connectionFailed')
  } finally {
    loading.value = false
  }
}

async function addMember(): Promise<void> {
  if (!username.value.trim() || newMemberRoleCodes.value.length === 0) return

  addingMember.value = true
  error.value = ''
  try {
    const result = await apiClient.addProjectMember(
      projectId.value,
      { username: username.value.trim(), roleCodes: newMemberRoleCodes.value },
      await secureHeaders(),
    )
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.members.addFailed'))
      return
    }
    username.value = ''
    newMemberRoleCodes.value = ['contributor']
    showCreated(t('projects.members.record'), result.data.username)
    await loadPage()
  } catch {
    error.value = t('projects.members.connectionFailed')
  } finally {
    addingMember.value = false
  }
}

async function saveMemberRoles(member: ProjectMemberResponse): Promise<void> {
  const roleCodes = memberRoles.value[member.id] ?? []
  if (roleCodes.length === 0) {
    error.value = t('projects.members.atLeastOneRole')
    return
  }

  savingMemberId.value = member.id
  error.value = ''
  try {
    const result = await apiClient.updateProjectMemberRoles(
      projectId.value,
      member.id,
      { roleCodes, version: member.version },
      await secureHeaders(),
    )
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.members.updateFailed'))
      return
    }
    showUpdated(t('projects.members.record'), result.data.username)
    await loadPage()
  } catch {
    error.value = t('projects.members.connectionFailed')
  } finally {
    savingMemberId.value = ''
  }
}

async function removeMember(member: ProjectMemberResponse): Promise<void> {
  if (!window.confirm(t('projects.members.removeConfirm', { username: member.username }))) return

  removingMemberId.value = member.id
  error.value = ''
  try {
    const result = await apiClient.removeProjectMember(
      projectId.value,
      member.id,
      member.version,
      await secureHeaders(),
    )
    if (result.error) {
      error.value = problemMessage(result.error, t('projects.members.removeFailed'))
      return
    }
    await loadPage()
  } catch {
    error.value = t('projects.members.connectionFailed')
  } finally {
    removingMemberId.value = ''
  }
}

function formatDate(value: string): string {
  return d(new Date(value), 'dateTime')
}
</script>

<template>
  <section class="members-page">
    <header class="page-heading">
      <div>
        <p>{{ project?.code }}</p>
        <h2>{{ t('projects.members.title') }}</h2>
        <span>{{ project?.name }}</span>
      </div>
      <strong v-if="!loading">{{ t('projects.members.count', { count: members.length }, members.length) }}</strong>
    </header>

    <p v-if="loading" class="page-state">{{ t('projects.members.loading') }}</p>
    <template v-else>
      <form v-if="canAddMember" class="member-add" @submit.prevent="addMember">
        <UiField
          id="new-member-username"
          v-model="username"
          :label="t('projects.members.username')"
          :placeholder="t('projects.members.usernamePlaceholder')"
          :disabled="addingMember"
        />
        <fieldset class="role-options">
          <legend>{{ t('projects.members.roles') }}</legend>
          <label v-for="role in roles" :key="role.code">
            <input
              v-model="newMemberRoleCodes"
              type="checkbox"
              :value="role.code"
              :disabled="addingMember"
            />
            <span><strong>{{ role.name }}</strong><small>{{ role.description }}</small></span>
          </label>
        </fieldset>
        <UiButton
          type="submit"
          :loading="addingMember"
          :disabled="!username.trim() || newMemberRoleCodes.length === 0"
        >
          <UserPlus :size="17" aria-hidden="true" />
          {{ t('projects.members.add') }}
        </UiButton>
      </form>

      <p v-if="error" class="form-error" role="alert">{{ error }}</p>

      <div class="member-panel">
        <div class="member-panel__title">
          <Users :size="18" aria-hidden="true" />
          <h3>{{ t('projects.members.record') }}</h3>
        </div>
        <article v-for="member in members" :key="member.id" class="member-card">
          <div class="member-card__identity">
            <span>{{ member.username.slice(0, 1).toUpperCase() }}</span>
            <div>
              <strong>{{ member.username }}</strong>
              <small>{{ t('projects.members.joinedAt', { date: formatDate(member.joinedAt) }) }}</small>
            </div>
          </div>
          <div class="member-card__roles">
            <label v-for="role in roles" :key="role.code">
              <input
                v-model="memberRoles[member.id]"
                type="checkbox"
                :value="role.code"
                :disabled="!canAssignRoles || savingMemberId === member.id"
              />
              {{ role.name }}
            </label>
          </div>
          <div class="member-card__actions">
            <UiButton
              v-if="canAssignRoles"
              variant="secondary"
              :loading="savingMemberId === member.id"
              :disabled="(memberRoles[member.id]?.length ?? 0) === 0"
              @click="saveMemberRoles(member)"
            >
              <Save :size="16" aria-hidden="true" />
              {{ t('projects.members.saveRoles') }}
            </UiButton>
            <UiButton
              v-if="canRemoveMember"
              variant="ghost"
              :loading="removingMemberId === member.id"
              @click="removeMember(member)"
            >
              <Trash2 :size="16" aria-hidden="true" />
              {{ t('projects.members.remove') }}
            </UiButton>
          </div>
        </article>
      </div>
    </template>
  </section>
</template>

<style scoped>
.members-page {
  display: grid;
  gap: 22px;
}

.page-heading,
.member-panel__title,
.member-card,
.member-card__identity,
.member-card__actions {
  display: flex;
  align-items: center;
}

.page-heading {
  justify-content: space-between;
}

.page-heading p,
.page-heading h2,
.member-panel h3 {
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
.page-heading > strong,
.member-card small {
  color: var(--kk-text-muted);
  font-size: 0.8rem;
}

.member-add,
.member-panel {
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.member-add {
  display: grid;
  grid-template-columns: minmax(180px, 1fr) minmax(360px, 2fr) auto;
  align-items: end;
  gap: 14px;
  padding: 18px;
}

.role-options {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 16px;
  margin: 0;
  padding: 0;
  border: 0;
}

.role-options legend {
  width: 100%;
  margin-bottom: 2px;
  font-size: 0.875rem;
  font-weight: 650;
}

.role-options label,
.member-card__roles label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.8rem;
}

.role-options label span,
.member-card__identity > div {
  display: grid;
}

.role-options small {
  color: var(--kk-text-muted);
  font-size: 0.7rem;
}

.member-panel {
  padding: 20px;
}

.member-panel__title {
  gap: 8px;
  margin-bottom: 10px;
}

.member-card {
  justify-content: space-between;
  gap: 18px;
  padding: 15px 0;
  border-top: 1px solid var(--kk-border);
}

.member-card__identity {
  min-width: 180px;
  gap: 10px;
}

.member-card__identity > span {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 50%;
  font-weight: 750;
}

.member-card__roles {
  display: flex;
  flex: 1;
  flex-wrap: wrap;
  gap: 9px 14px;
}

.member-card__actions {
  gap: 6px;
}

.page-state {
  padding: 42px 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.form-error {
  color: var(--kk-danger);
}

@media (max-width: 820px) {
  .member-add {
    grid-template-columns: 1fr;
  }

  .member-card {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
