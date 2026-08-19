<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { CheckCircle2, ClipboardList, FileText, Paperclip, RefreshCw, Target, Trash2, Upload } from '@lucide/vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiAttachmentLink, UiButton, UiCreateActions, UiFormGrid, UiFormSection, UiInput } from '@khaikang/ui'
import AppMarkdown from '../components/AppMarkdown.vue'
import IssueRelationsPanel from '../components/IssueRelationsPanel.vue'
import ResourceFormLayout from '../components/ResourceFormLayout.vue'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  IssueMetadataResponse,
  IssueAttachmentResponse,
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
const attachments = ref<IssueAttachmentResponse[]>([])
const loading = ref(true)
const saving = ref(false)
const savingAssignee = ref(false)
const savingStatus = ref(false)
const uploadingAttachment = ref(false)
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
const statusCode = ref('')
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
const canChangeStatus = computed(() => Boolean(
  isActiveProject.value && project.value?.currentUserPermissions.includes('issue.status.change'),
))
const canUploadAttachments = computed(() => Boolean(
  issueId.value && isActiveProject.value &&
  project.value?.currentUserPermissions.includes('issue.attachment.upload'),
))
const canDeleteAttachments = computed(() => Boolean(
  isActiveProject.value && project.value?.currentUserPermissions.includes('issue.attachment.delete'),
))
const canCreateRelations = computed(() => Boolean(
  isActiveProject.value && project.value?.currentUserPermissions.includes('issue.relation.create'),
))
const canDeleteRelations = computed(() => Boolean(
  isActiveProject.value && project.value?.currentUserPermissions.includes('issue.update'),
))
const attachmentDialogLabels = computed(() => ({
  attachmentDialog: t('common.markdown.attachmentDialog'),
  downloadAttachment: t('common.markdown.downloadAttachment'),
  close: t('common.actions.close'),
}))
const contentReadOnly = computed(() => isEditing.value && !canEdit.value)
const canSave = computed(
  () => isActiveProject.value
    && (isEditing.value ? canEdit.value : canCreate.value)
    && form.title.trim().length > 0,
)
const canRenderForm = computed(() => Boolean(
  !loading.value && project.value && metadata.value && (!isEditing.value || issue.value),
))
const hasLoadError = computed(() => Boolean(error.value && !canRenderForm.value))

onMounted(() => {
  window.addEventListener('beforeunload', handleBeforeUnload)
  void loadPage()
})
onBeforeUnmount(() => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
})
onBeforeRouteLeave(() => {
  if (allowNavigation.value || !isDirty.value || saving.value || savingAssignee.value || savingStatus.value) {
    return true
  }

  return window.confirm(t('projects.issues.unsavedConfirm'))
})

function handleBeforeUnload(event: BeforeUnloadEvent): void {
  if (!isDirty.value || allowNavigation.value || saving.value || savingAssignee.value || savingStatus.value) return

  event.preventDefault()
  event.returnValue = ''
}

async function loadPage(): Promise<void> {
  loading.value = true
  error.value = ''
  versionConflict.value = false
  try {
    const [projectResult, metadataResult, memberResult, issueResult, attachmentResult] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.getIssueMetadata(projectId.value),
      apiClient.listProjectMembers(projectId.value),
      issueId.value ? apiClient.getIssue(projectId.value, issueId.value) : Promise.resolve(undefined),
      issueId.value ? apiClient.listIssueAttachments(projectId.value, issueId.value) : Promise.resolve(undefined),
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
    attachments.value = attachmentResult?.data ?? []
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
  statusCode.value = value.statusCode
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

async function changeStatus(nextStatusCode: string): Promise<void> {
  if (!issue.value || !issueId.value || !canChangeStatus.value) return

  const previousStatusCode = issue.value.statusCode
  if (previousStatusCode === nextStatusCode) return

  savingStatus.value = true
  error.value = ''
  try {
    const result = await apiClient.updateIssueStatus(
      projectId.value,
      issueId.value,
      { statusCode: nextStatusCode, version: issue.value.version },
      await secureHeaders(),
    )
    if (!result.data) {
      statusCode.value = previousStatusCode
      error.value = problemMessage(result.error, t('projects.issues.statusUpdateFailed'))
      versionConflict.value = result.error?.code === 'issue_version_conflict'
      return
    }

    issue.value = result.data
    statusCode.value = result.data.statusCode
    showUpdated(t('projects.issues.record'), result.data.key)
  } catch {
    statusCode.value = previousStatusCode
    error.value = t('projects.issues.connectionFailed')
  } finally {
    savingStatus.value = false
  }
}

async function uploadIssueAttachment(file: File): Promise<IssueAttachmentResponse> {
  if (!issueId.value || !canUploadAttachments.value) throw new Error('Attachment upload is unavailable.')
  const result = await apiClient.uploadIssueAttachment(
    projectId.value,
    issueId.value,
    file,
    await secureHeaders(),
  )
  if (!result.data) throw new Error(problemMessage(result.error, t('projects.issues.attachments.uploadFailed')))
  attachments.value = [result.data, ...attachments.value.filter(item => item.id !== result.data!.id)]
  return result.data
}

async function uploadIssueImage(file: File): Promise<{ src: string, alt: string }> {
  if (!file.type.startsWith('image/')) throw new Error('An image file is required.')
  const attachment = await uploadIssueAttachment(file)
  return {
    src: apiClient.issueAttachmentContentUrl(projectId.value, attachment.issueId, attachment.id, true),
    alt: attachment.originalFileName,
  }
}

async function uploadIssueFile(file: File): Promise<{ src: string, name: string }> {
  const attachment = await uploadIssueAttachment(file)
  return {
    src: apiClient.issueAttachmentContentUrl(projectId.value, attachment.issueId, attachment.id),
    name: attachment.originalFileName,
  }
}

async function selectAttachment(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || uploadingAttachment.value) return
  uploadingAttachment.value = true
  error.value = ''
  try {
    await uploadIssueAttachment(file)
  } catch (uploadError) {
    error.value = uploadError instanceof Error ? uploadError.message : t('projects.issues.attachments.uploadFailed')
  } finally {
    uploadingAttachment.value = false
    input.value = ''
  }
}

async function deleteAttachment(attachment: IssueAttachmentResponse): Promise<void> {
  if (!issueId.value || !canDeleteAttachments.value ||
      !window.confirm(t('projects.issues.attachments.deleteConfirm'))) return
  error.value = ''
  const result = await apiClient.deleteIssueAttachment(
    projectId.value,
    issueId.value,
    attachment.id,
    await secureHeaders(),
  )
  if (result.error) {
    error.value = problemMessage(result.error, t('projects.issues.attachments.deleteFailed'))
    return
  }
  attachments.value = attachments.value.filter(item => item.id !== attachment.id)
}

function attachmentSize(fileSize: number): string {
  return t('projects.issues.attachments.size', { size: Math.max(0.1, fileSize / 1024).toFixed(1) })
}
</script>

<template>
  <ResourceFormLayout
    :back-to="{ name: 'project-issues', params: { projectId } }"
    :back-label="t('projects.issues.back')"
    :meta="project?.code ?? ''"
    :title="isEditing ? issue?.key ?? t('projects.issues.edit') : t('projects.issues.create')"
    :description="t(isEditing ? 'projects.issues.editDescription' : 'projects.issues.createDescription')"
    :loading="loading"
    :loading-label="t('projects.issues.loadingDetail')"
    :error="canRenderForm ? error : ''"
    :show-error-slot="canRenderForm && Boolean(error)"
    :show-actions="canRenderForm"
  >
    <template v-if="canRenderForm && error" #error>
      <div class="action-error">
        <span>{{ error }}</span>
        <UiButton v-if="versionConflict" variant="secondary" type="button" @click="reloadIssue">
          <RefreshCw :size="16" aria-hidden="true" />
          {{ t('projects.issues.reloadLatest') }}
        </UiButton>
      </div>
    </template>

    <div
      v-if="hasLoadError"
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
      v-if="canRenderForm && project && metadata && (!isEditing || issue)"
      class="issue-form"
      @submit.prevent="save()"
    >
      <p v-if="!isActiveProject" class="readonly-notice">
        {{ t('projects.issues.inactiveProject') }}
      </p>

      <UiFormSection>
        <template #header><ClipboardList :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.basic.title') }}</h3><p>{{ t('projects.issues.basic.description') }}</p></div></template>
        <UiFormGrid :columns="3">
          <label class="field field--full ui-form-grid__full" for="issue-title"><span>{{ t('projects.issues.fields.title') }}</span><UiInput id="issue-title" v-model="form.title" required maxlength="200" :placeholder="t('projects.issues.fields.titlePlaceholder')" :disabled="contentReadOnly || !isActiveProject" /></label>
          <label class="field"><span>{{ t('projects.issues.fields.type') }}</span><select v-model="form.typeCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.types" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>{{ t('projects.issues.fields.priority') }}</span><select v-model="form.priorityCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.priorities" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>{{ t('projects.issues.fields.assignee') }}</span><select
            v-model="form.assigneeAccountId"
            :disabled="!canAssign || !isActiveProject || savingAssignee"
            @change="isEditing && changeAssignee(($event.target as HTMLSelectElement).value || null)"
          ><option :value="null">{{ t('projects.issues.unassigned') }}</option><option v-for="member in members" :key="member.id" :value="member.accountId">{{ member.username }}</option></select></label>
          <label v-if="issue" class="field"><span>{{ t('projects.issues.fields.currentStatus') }}</span><select
            v-model="statusCode"
            :disabled="!canChangeStatus || savingStatus"
            @change="changeStatus(($event.target as HTMLSelectElement).value)"
          ><option v-for="status in metadata.statuses" :key="status.code" :value="status.code">{{ status.name }}</option></select></label>
          <div class="field field--full ui-form-grid__full"><span>{{ t('projects.issues.fields.description') }}</span><AppMarkdown v-model="form.description" :mode="contentReadOnly ? 'display' : 'edit'" :placeholder="t('projects.issues.fields.descriptionPlaceholder')" :disabled="!isActiveProject" :upload-image="canUploadAttachments ? uploadIssueImage : undefined" :upload-attachment="canUploadAttachments ? uploadIssueFile : undefined" /></div>
        </UiFormGrid>
      </UiFormSection>

      <IssueRelationsPanel
        v-if="isEditing && issueId"
        :project-id="projectId"
        :issue-id="issueId"
        :can-create="canCreateRelations"
        :can-delete="canDeleteRelations"
      />

      <UiFormSection><template #header><FileText :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.userStory.title') }}</h3><p>{{ t('projects.issues.userStory.description') }}</p></div></template><div class="field"><AppMarkdown v-model="form.userStory" :mode="contentReadOnly ? 'display' : 'edit'" placeholder="As a... I want... So that..." :disabled="!isActiveProject" :upload-image="canUploadAttachments ? uploadIssueImage : undefined" :upload-attachment="canUploadAttachments ? uploadIssueFile : undefined" /></div></UiFormSection>

      <UiFormSection><template #header><Target :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.definition.title') }}</h3><p>{{ t('projects.issues.definition.description') }}</p></div></template><div class="field"><AppMarkdown v-model="form.definitionOfDone" :mode="contentReadOnly ? 'display' : 'edit'" :placeholder="t('projects.issues.definition.placeholder')" :disabled="!isActiveProject" :upload-image="canUploadAttachments ? uploadIssueImage : undefined" :upload-attachment="canUploadAttachments ? uploadIssueFile : undefined" /></div></UiFormSection>

      <UiFormSection><template #header><CheckCircle2 :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.completion.title') }}</h3><p>{{ t('projects.issues.completion.description') }}</p></div></template><div class="field"><AppMarkdown v-model="form.completionSummary" :mode="!isEditing || contentReadOnly ? 'display' : 'edit'" :disabled="!isActiveProject" :placeholder="t(isEditing ? 'projects.issues.completion.editPlaceholder' : 'projects.issues.completion.createPlaceholder')" :upload-image="canUploadAttachments ? uploadIssueImage : undefined" :upload-attachment="canUploadAttachments ? uploadIssueFile : undefined" /></div></UiFormSection>

      <UiFormSection v-if="isEditing">
        <template #header><Paperclip :size="19" aria-hidden="true" /><div><h3>{{ t('projects.issues.attachments.title') }}</h3><p>{{ t('projects.issues.attachments.description') }}</p></div></template>
        <label v-if="canUploadAttachments" class="attachment-upload">
          <Upload :size="16" aria-hidden="true" />
          <span>{{ t(uploadingAttachment ? 'projects.issues.attachments.uploading' : 'projects.issues.attachments.upload') }}</span>
          <input type="file" :disabled="uploadingAttachment" @change="selectAttachment" />
        </label>
        <p v-if="attachments.length === 0" class="attachment-empty">{{ t('projects.issues.attachments.empty') }}</p>
        <ul v-else class="attachment-list">
          <li v-for="attachment in attachments" :key="attachment.id">
            <div><UiAttachmentLink :href="apiClient.issueAttachmentContentUrl(projectId, attachment.issueId, attachment.id)" :file-name="attachment.originalFileName" :labels="attachmentDialogLabels" /><small>{{ attachmentSize(attachment.fileSize) }} · {{ attachment.uploadedByUsername }}</small></div>
            <UiButton v-if="canDeleteAttachments" type="button" variant="secondary" :aria-label="t('projects.issues.attachments.delete')" @click="deleteAttachment(attachment)"><Trash2 :size="15" aria-hidden="true" /></UiButton>
          </li>
        </ul>
      </UiFormSection>

    </form>
    <template #actions>
      <template v-if="isEditing">
        <UiButton variant="secondary" type="button" @click="router.push({ name: 'project-issues', params: { projectId } })">{{ t('common.actions.cancel') }}</UiButton>
        <UiButton type="button" :disabled="!canSave || saving" @click="save()">{{ t(saving ? 'projects.issues.saving' : 'projects.issues.saveChanges') }}</UiButton>
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
    </template>
  </ResourceFormLayout>
</template>

<style scoped>
.record-info { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin: 0; padding: 15px 18px; background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.record-info div { display: grid; gap: 4px; }
.record-info dt { color: var(--kk-text-muted); font-size: .72rem; }
.record-info dd { margin: 0; font-size: .8rem; font-weight: 650; }
.field { display: grid; align-content: start; gap: 7px; font-size: .8rem; font-weight: 650; }
.field input, .field select, .field textarea, .read-value { width: 100%; padding: 10px 11px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: 6px; font: inherit; box-sizing: border-box; }
.field textarea { line-height: 1.65; resize: vertical; }
.field textarea:disabled { color: var(--kk-text-muted); background: var(--kk-surface-subtle); }
.read-value { min-height: 39px; background: var(--kk-surface-subtle); }
.action-error { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin: 0; padding: 10px 14px; color: var(--kk-danger); background: #fff1f0; border: 1px solid #f1c2bd; border-radius: 7px; font-size: .82rem; }
.readonly-notice { margin: 0; padding: 10px 14px; color: var(--kk-text-muted); background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: 7px; font-size: .82rem; }
.attachment-upload { position: relative; display: inline-flex; width: fit-content; min-height: 34px; align-items: center; gap: 7px; padding: 7px 11px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: 6px; font-size: .82rem; font-weight: 650; cursor: pointer; }
.attachment-upload input { position: absolute; width: 1px; height: 1px; overflow: hidden; opacity: 0; }
.attachment-empty { margin: 0; color: var(--kk-text-muted); font-size: .84rem; }
.attachment-list { display: grid; gap: 8px; margin: 0; padding: 0; list-style: none; }
.attachment-list li { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 12px; background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: 6px; }
.attachment-list li > div { display: grid; min-width: 0; gap: 3px; }
.attachment-list a { overflow: hidden; color: var(--kk-accent); font-size: .86rem; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.attachment-list small { color: var(--kk-text-muted); font-size: .72rem; }
.page-state { padding: 42px 24px; text-align: center; background: var(--kk-surface); border: 1px dashed var(--kk-border-strong); border-radius: var(--kk-radius); }
.page-state--error { color: var(--kk-danger); }
@media (max-width: 720px) { .action-error { align-items: stretch; flex-direction: column; } .record-info { grid-template-columns: 1fr 1fr; } }
</style>
