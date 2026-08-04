<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { ArrowLeft, CheckCircle2, ClipboardList, FileText, RefreshCw, Target } from '@lucide/vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiCreateActions, UiFormActionBar } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  IssueMetadataResponse,
  IssueResponse,
  ProjectMemberResponse,
  ProjectResponse,
} from '../api/contracts'
import { useFormDirtyState } from '../composables/useFormDirtyState'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const projectId = computed(() => String(route.params.projectId))
const issueId = computed(() => route.params.issueId ? String(route.params.issueId) : undefined)
const isEditing = computed(() => Boolean(issueId.value))
const project = ref<ProjectResponse>()
const issue = ref<IssueResponse>()
const metadata = ref<IssueMetadataResponse>()
const members = ref<ProjectMemberResponse[]>([])
const loading = ref(true)
const saving = ref(false)
const savingAssignee = ref(false)
const error = ref('')
const versionConflict = ref(false)
const allowNavigation = ref(false)

const form = reactive({
  title: '',
  typeCode: 'task',
  priorityCode: 'medium',
  assigneeAccountId: null as string | null,
  description: '',
  userStory: '',
  definitionOfDone: '',
  completionSummary: '',
})
const { showCreated, showUpdated } = useSaveNotice()
const { isDirty, markClean } = useFormDirtyState(() => ({ ...form }))

const isActiveProject = computed(() => project.value?.status === 'active')
const canEdit = computed(
  () => project.value?.currentUserPermissions.includes('issue.update') ?? false,
)
const canCreate = computed(
  () => project.value?.currentUserPermissions.includes('issue.create') ?? false,
)
const canAssign = computed(
  () => project.value?.currentUserPermissions.includes('issue.assignee.change') ?? false,
)
const contentReadOnly = computed(() => isEditing.value && !canEdit.value)
const canSave = computed(
  () => isActiveProject.value
    && (isEditing.value ? canEdit.value : canCreate.value)
    && form.title.trim().length > 0,
)

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload)
  void loadPage()
})
onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
onBeforeRouteLeave(() => {
  if (allowNavigation.value || !isDirty.value || saving.value || savingAssignee.value) {
    return true
  }

  return window.confirm(t('projects.issues.unsavedConfirm'))
})

function handleBeforeUnload(event: BeforeUnloadEvent): void {
  if (!isDirty.value || allowNavigation.value || saving.value || savingAssignee.value) return

  event.preventDefault()
  event.returnValue = ''
}

async function loadPage(): Promise<void> {
  loading.value = true
  error.value = ''
  versionConflict.value = false
  try {
    const [projectResult, metadataResult, memberResult, issueResult] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.getIssueMetadata(projectId.value),
      apiClient.listProjectMembers(projectId.value),
      issueId.value ? apiClient.getIssue(projectId.value, issueId.value) : Promise.resolve(undefined),
    ])

    if (!projectResult.data || !metadataResult.data || (isEditing.value && !issueResult?.data)) {
      error.value = problemMessage(
        projectResult.error ?? metadataResult.error ?? issueResult?.error,
        t('projects.issues.issueNotFound'),
      )
      return
    }

    project.value = projectResult.data
    metadata.value = metadataResult.data
    members.value = memberResult.data ?? []
    if (issueResult?.data) {
      applyIssue(issueResult.data)
    } else {
      form.typeCode = metadataResult.data.types.find((item) => item.code === 'task')?.code
        ?? metadataResult.data.types[0]?.code
        ?? 'task'
      form.priorityCode = metadataResult.data.priorities.find((item) => item.code === 'medium')?.code
        ?? metadataResult.data.priorities[0]?.code
        ?? 'medium'
    }
    await nextTick()
    markClean()
  } catch {
    error.value = t('projects.issues.connectionFailed')
  } finally {
    loading.value = false
  }
}

function applyIssue(value: IssueResponse): void {
  issue.value = value
  form.title = value.title
  form.typeCode = value.typeCode
  form.priorityCode = value.priorityCode
  form.assigneeAccountId = value.assigneeAccountId
  form.description = value.description ?? ''
  form.userStory = value.userStory ?? ''
  form.definitionOfDone = value.definitionOfDone ?? ''
  form.completionSummary = value.completionSummary ?? ''
}

function resetCreateForm(): void {
  issue.value = undefined
  form.title = ''
  form.typeCode = metadata.value?.types.find((item) => item.code === 'task')?.code
    ?? metadata.value?.types[0]?.code
    ?? 'task'
  form.priorityCode = metadata.value?.priorities.find((item) => item.code === 'medium')?.code
    ?? metadata.value?.priorities[0]?.code
    ?? 'medium'
  form.assigneeAccountId = null
  form.description = ''
  form.userStory = ''
  form.definitionOfDone = ''
  form.completionSummary = ''
}

function nullable(value: string): string | null {
  return value.trim() || null
}

async function save(continueAfterCreate = false): Promise<void> {
  if (!canSave.value || saving.value) return
  saving.value = true
  error.value = ''
  versionConflict.value = false
  try {
    const headers = await secureHeaders()
    const wasEditing = Boolean(issue.value && issueId.value)
    const details = {
      title: form.title.trim(),
      typeCode: form.typeCode,
      priorityCode: form.priorityCode,
      description: nullable(form.description),
      userStory: nullable(form.userStory),
      definitionOfDone: nullable(form.definitionOfDone),
    }
    const result = issue.value && issueId.value
      ? await apiClient.updateIssue(
          projectId.value,
          issueId.value,
          {
            ...details,
            completionSummary: nullable(form.completionSummary),
            version: issue.value.version,
          },
          headers,
        )
      : await apiClient.createIssue(
          projectId.value,
          {
            ...details,
            assigneeAccountId: canAssign.value ? form.assigneeAccountId : null,
          },
          headers,
        )

    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.issues.saveFailed'))
      versionConflict.value = result.error?.code === 'issue_version_conflict'
      return
    }

    if (wasEditing) {
      applyIssue(result.data)
      markClean()
      showUpdated(t('projects.issues.record'), result.data.key)
    } else if (continueAfterCreate) {
      resetCreateForm()
      await nextTick()
      markClean()
      showCreated(t('projects.issues.record'), result.data.key)
      document.getElementById('issue-title')?.focus()
    } else {
      markClean()
      allowNavigation.value = true
      await router.push({
        name: 'project-issues',
        params: { projectId: projectId.value },
        query: {
          savedMode: 'created',
          savedKey: result.data.key,
        },
      })
    }
  } catch {
    error.value = t('projects.issues.connectionFailed')
  } finally {
    saving.value = false
  }
}

async function changeAssignee(assigneeAccountId: string | null): Promise<void> {
  if (!issue.value || !issueId.value || !canAssign.value || !isActiveProject.value) return

  const previousAssigneeAccountId = issue.value.assigneeAccountId
  if (previousAssigneeAccountId === assigneeAccountId) return

  savingAssignee.value = true
  error.value = ''
  try {
    const result = await apiClient.updateIssueAssignee(
      projectId.value,
      issueId.value,
      { assigneeAccountId, version: issue.value.version },
      await secureHeaders(),
    )
    if (!result.data) {
      form.assigneeAccountId = previousAssigneeAccountId
      error.value = problemMessage(result.error, t('projects.issues.assigneeUpdateFailed'))
      versionConflict.value = result.error?.code === 'issue_version_conflict'
      return
    }

    applyIssue(result.data)
    markClean()
    showUpdated(t('projects.issues.record'), result.data.key)
  } catch {
    form.assigneeAccountId = previousAssigneeAccountId
    error.value = t('projects.issues.connectionFailed')
  } finally {
    savingAssignee.value = false
  }
}

async function reloadIssue(): Promise<void> {
  if (!issueId.value) return

  await loadPage()
}

function formatDateTime(value: string | null): string {
  if (!value) return t('projects.issues.notCompleted')
  return d(new Date(value), 'dateTime')
}
</script>

<template>
  <section class="issue-form-page">
    <header class="page-heading">
      <button
        type="button"
        class="back-link"
        @click="router.push({ name: 'project-issues', params: { projectId } })"
      >
        <ArrowLeft :size="16" aria-hidden="true" />{{ t('projects.issues.back') }}
      </button>
      <div class="heading-row">
        <div>
          <p>{{ project?.code }}</p>
          <h2>{{ isEditing ? issue?.key ?? t('projects.issues.edit') : t('projects.issues.create') }}</h2>
          <span>{{ t(isEditing ? 'projects.issues.editDescription' : 'projects.issues.createDescription') }}</span>
        </div>
      </div>
    </header>

    <p v-if="loading" class="page-state">{{ t('projects.issues.loadingDetail') }}</p>
    <div
      v-else-if="error && (!metadata || (isEditing && !issue))"
      class="page-state page-state--error"
      role="alert"
    >
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadPage">{{ t('common.actions.reload') }}</UiButton>
    </div>

    <dl v-if="issue && isEditing" class="record-info">
      <div><dt>{{ t('projects.issues.metadata.reporter') }}</dt><dd>{{ issue.reporterUsername }}</dd></div>
      <div><dt>{{ t('projects.issues.metadata.createdAt') }}</dt><dd>{{ formatDateTime(issue.createdAt) }}</dd></div>
      <div><dt>{{ t('projects.issues.metadata.updatedAt') }}</dt><dd>{{ formatDateTime(issue.updatedAt) }}</dd></div>
      <div><dt>{{ t('projects.issues.metadata.completedAt') }}</dt><dd>{{ formatDateTime(issue.completedAt) }}</dd></div>
    </dl>

    <form
      v-if="!loading && project && metadata && (!isEditing || issue)"
      class="issue-form"
      @submit.prevent="save()"
    >
      <div v-if="error" class="action-error" role="alert">
        <span>{{ error }}</span>
        <UiButton v-if="versionConflict" variant="secondary" type="button" @click="reloadIssue">
          <RefreshCw :size="16" aria-hidden="true" />
          {{ t('projects.issues.reloadLatest') }}
        </UiButton>
      </div>
      <p v-if="!isActiveProject" class="readonly-notice">
        {{ t('projects.issues.inactiveProject') }}
      </p>

      <section class="form-section">
        <header><ClipboardList :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.basic.title') }}</h3><p>{{ t('projects.issues.basic.description') }}</p></div></header>
        <div class="field-grid">
          <label class="field field--full" for="issue-title"><span>{{ t('projects.issues.fields.title') }}</span><input id="issue-title" v-model="form.title" required maxlength="200" :placeholder="t('projects.issues.fields.titlePlaceholder')" :disabled="contentReadOnly || !isActiveProject" /></label>
          <label class="field"><span>{{ t('projects.issues.fields.type') }}</span><select v-model="form.typeCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.types" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>{{ t('projects.issues.fields.priority') }}</span><select v-model="form.priorityCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.priorities" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>{{ t('projects.issues.fields.assignee') }}</span><select
            v-model="form.assigneeAccountId"
            :disabled="!canAssign || !isActiveProject || savingAssignee"
            @change="isEditing && changeAssignee(($event.target as HTMLSelectElement).value || null)"
          ><option :value="null">{{ t('projects.issues.unassigned') }}</option><option v-for="member in members" :key="member.id" :value="member.accountId">{{ member.username }}</option></select></label>
          <div v-if="issue" class="field"><span>{{ t('projects.issues.fields.currentStatus') }}</span><div class="read-value">{{ issue.statusName }}</div></div>
          <label class="field field--full"><span>{{ t('projects.issues.fields.description') }}</span><textarea v-model="form.description" rows="5" maxlength="20000" :placeholder="t('projects.issues.fields.descriptionPlaceholder')" :disabled="contentReadOnly || !isActiveProject" /></label>
        </div>
      </section>

      <section class="form-section">
        <header><FileText :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.userStory.title') }}</h3><p>{{ t('projects.issues.userStory.description') }}</p></div></header>
        <label class="field"><textarea v-model="form.userStory" rows="8" maxlength="20000" placeholder="As a... I want... So that..." :disabled="contentReadOnly || !isActiveProject" /></label>
      </section>

      <section class="form-section">
        <header><Target :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.definition.title') }}</h3><p>{{ t('projects.issues.definition.description') }}</p></div></header>
        <label class="field"><textarea v-model="form.definitionOfDone" rows="8" maxlength="20000" :placeholder="t('projects.issues.definition.placeholder')" :disabled="contentReadOnly || !isActiveProject" /></label>
      </section>

      <section class="form-section">
        <header><CheckCircle2 :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.completion.title') }}</h3><p>{{ t('projects.issues.completion.description') }}</p></div></header>
        <label class="field"><textarea v-model="form.completionSummary" rows="8" maxlength="20000" :disabled="!isEditing || contentReadOnly || !isActiveProject" :placeholder="t(isEditing ? 'projects.issues.completion.editPlaceholder' : 'projects.issues.completion.createPlaceholder')" /></label>
      </section>

      <UiFormActionBar mode="floating">
        <template v-if="isEditing">
          <UiButton variant="secondary" type="button" @click="router.push({ name: 'project-issues', params: { projectId } })">{{ t('common.actions.cancel') }}</UiButton>
          <UiButton type="submit" :disabled="!canSave || saving">{{ t(saving ? 'projects.issues.saving' : 'projects.issues.saveChanges') }}</UiButton>
        </template>
        <UiCreateActions
          v-else
          :loading="saving"
          :disabled="!canSave"
          :cancel-label="t('common.actions.cancel')"
          :create-label="t('projects.issues.create')"
          :continue-label="t('projects.issues.createAndContinue')"
          @cancel="router.push({ name: 'project-issues', params: { projectId } })"
          @create="save(false)"
          @create-continue="save(true)"
        />
      </UiFormActionBar>
    </form>

  </section>
</template>

<style scoped>
.issue-form-page { display: grid; gap: 22px; max-width: 1050px; margin: 0 auto; }
.page-heading { display: grid; gap: 14px; }
.back-link { display: flex; width: fit-content; align-items: center; gap: 6px; padding: 0; color: var(--kk-text-muted); background: transparent; border: 0; cursor: pointer; }
.heading-row { display: flex; align-items: center; justify-content: space-between; gap: 20px; }
.heading-row p, .heading-row h2 { margin: 0; }
.heading-row p { color: var(--kk-accent); font-size: .75rem; font-weight: 750; letter-spacing: .08em; }
.heading-row h2 { font-size: 1.8rem; }
.heading-row span { color: var(--kk-text-muted); font-size: .82rem; }
.record-info { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin: 0; padding: 15px 18px; background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.record-info div { display: grid; gap: 4px; }
.record-info dt { color: var(--kk-text-muted); font-size: .72rem; }
.record-info dd { margin: 0; font-size: .8rem; font-weight: 650; }
.issue-form { display: grid; gap: 18px; }
.form-section { display: grid; gap: 18px; padding: 22px; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.form-section > header { display: flex; align-items: flex-start; gap: 10px; padding-bottom: 14px; border-bottom: 1px solid var(--kk-border); }
.form-section > header svg { margin-top: 2px; color: var(--kk-accent); }
.form-section h3, .form-section p { margin: 0; }
.form-section h3 { font-size: 1rem; }
.form-section p { color: var(--kk-text-muted); font-size: .78rem; }
.field-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }
.field { display: grid; align-content: start; gap: 7px; font-size: .8rem; font-weight: 650; }
.field--full { grid-column: 1 / -1; }
.field input, .field select, .field textarea, .read-value { width: 100%; padding: 10px 11px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: 6px; font: inherit; box-sizing: border-box; }
.field textarea { line-height: 1.65; resize: vertical; }
.field textarea:disabled { color: var(--kk-text-muted); background: var(--kk-surface-subtle); }
.read-value { min-height: 39px; background: var(--kk-surface-subtle); }
.action-error { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin: 0; padding: 10px 14px; color: var(--kk-danger); background: #fff1f0; border: 1px solid #f1c2bd; border-radius: 7px; font-size: .82rem; }
.readonly-notice { margin: 0; padding: 10px 14px; color: var(--kk-text-muted); background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: 7px; font-size: .82rem; }
.page-state { padding: 42px 24px; text-align: center; background: var(--kk-surface); border: 1px dashed var(--kk-border-strong); border-radius: var(--kk-radius); }
.page-state--error { color: var(--kk-danger); }
@media (max-width: 720px) { .heading-row, .action-error { align-items: stretch; flex-direction: column; } .heading-actions { justify-content: flex-start; } .record-info { grid-template-columns: 1fr 1fr; } .field-grid { grid-template-columns: 1fr; } .field--full { grid-column: 1; } }
</style>
