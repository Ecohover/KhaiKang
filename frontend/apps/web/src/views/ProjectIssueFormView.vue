<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { ArrowLeft, CheckCircle2, ClipboardList, FileText, RefreshCw, Target } from '@lucide/vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { UiButton, UiSaveToast, UiSaveToastStack } from '@khaikang/ui'
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
const { saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()
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

  return window.confirm('尚有未儲存的任務內容，確定要離開嗎？')
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
        '找不到任務，或你沒有檢視權限。',
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
    error.value = '無法連線到伺服器，請稍後再試。'
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
      error.value = problemMessage(result.error, '儲存任務失敗，請稍後再試。')
      versionConflict.value = result.error?.code === 'issue_version_conflict'
      return
    }

    if (wasEditing) {
      applyIssue(result.data)
      markClean()
      showUpdated(result.data.key)
    } else if (continueAfterCreate) {
      resetCreateForm()
      await nextTick()
      markClean()
      showCreated(result.data.key)
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
    error.value = '無法連線到伺服器，請稍後再試。'
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
      error.value = problemMessage(result.error, '更新處理人失敗，請重新載入後再試。')
      versionConflict.value = result.error?.code === 'issue_version_conflict'
      return
    }

    applyIssue(result.data)
    markClean()
    showUpdated(result.data.key)
  } catch {
    form.assigneeAccountId = previousAssigneeAccountId
    error.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    savingAssignee.value = false
  }
}

async function reloadIssue(): Promise<void> {
  if (!issueId.value) return

  await loadPage()
}

function formatDateTime(value: string | null): string {
  if (!value) return '尚未完成'
  return new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
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
        <ArrowLeft :size="16" aria-hidden="true" />返回任務列表
      </button>
      <div class="heading-row">
        <div>
          <p>{{ project?.code }}</p>
          <h2>{{ isEditing ? issue?.key ?? '編輯任務' : '新增任務' }}</h2>
          <span>{{ isEditing ? '編輯任務內容與處理紀錄' : '建立新的專案任務' }}</span>
        </div>
        <div class="heading-actions">
          <UiButton :disabled="!canSave || saving" @click="save()">
            {{ saving ? '儲存中…' : isEditing ? '儲存變更' : '建立任務' }}
          </UiButton>
          <UiButton
            v-if="!isEditing"
            variant="secondary"
            :disabled="!canSave || saving"
            @click="save(true)"
          >
            建立任務並繼續
          </UiButton>
        </div>
      </div>
    </header>

    <p v-if="loading" class="page-state">正在載入任務資料…</p>
    <div
      v-else-if="error && (!metadata || (isEditing && !issue))"
      class="page-state page-state--error"
      role="alert"
    >
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadPage">重新載入</UiButton>
    </div>

    <dl v-if="issue && isEditing" class="record-info">
      <div><dt>建立人</dt><dd>{{ issue.reporterUsername }}</dd></div>
      <div><dt>建立時間</dt><dd>{{ formatDateTime(issue.createdAt) }}</dd></div>
      <div><dt>更新時間</dt><dd>{{ formatDateTime(issue.updatedAt) }}</dd></div>
      <div><dt>完成時間</dt><dd>{{ formatDateTime(issue.completedAt) }}</dd></div>
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
          重新載入最新內容
        </UiButton>
      </div>
      <p v-if="!isActiveProject" class="readonly-notice">
        此專案已停用，任務目前為唯讀狀態。
      </p>

      <section class="form-section">
        <header><ClipboardList :size="19" aria-hidden="true" /><div><h3>基本資料</h3><p>任務識別、分類、負責人與背景說明。</p></div></header>
        <div class="field-grid">
          <label class="field field--full" for="issue-title"><span>標題 *</span><input id="issue-title" v-model="form.title" required maxlength="200" placeholder="輸入任務標題" :disabled="contentReadOnly || !isActiveProject" /></label>
          <label class="field"><span>類型 *</span><select v-model="form.typeCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.types" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>優先順序 *</span><select v-model="form.priorityCode" required :disabled="contentReadOnly || !isActiveProject"><option v-for="item in metadata.priorities" :key="item.code" :value="item.code">{{ item.name }}</option></select></label>
          <label class="field"><span>處理人</span><select
            v-model="form.assigneeAccountId"
            :disabled="!canAssign || !isActiveProject || savingAssignee"
            @change="isEditing && changeAssignee(($event.target as HTMLSelectElement).value || null)"
          ><option :value="null">未指派</option><option v-for="member in members" :key="member.id" :value="member.accountId">{{ member.username }}</option></select></label>
          <div v-if="issue" class="field"><span>目前狀態</span><div class="read-value">{{ issue.statusName }}</div></div>
          <label class="field field--full"><span>說明</span><textarea v-model="form.description" rows="5" maxlength="20000" placeholder="補充任務背景、範圍或實作說明" :disabled="contentReadOnly || !isActiveProject" /></label>
        </div>
      </section>

      <section class="form-section">
        <header><FileText :size="19" aria-hidden="true" /><div><h3>User Story</h3><p>描述使用者、需求與預期價值。</p></div></header>
        <label class="field"><textarea v-model="form.userStory" rows="8" maxlength="20000" placeholder="As a... I want... So that..." :disabled="contentReadOnly || !isActiveProject" /></label>
      </section>

      <section class="form-section">
        <header><Target :size="19" aria-hidden="true" /><div><h3>完成定義</h3><p>列出可驗證的完成條件與品質標準。</p></div></header>
        <label class="field"><textarea v-model="form.definitionOfDone" rows="8" maxlength="20000" placeholder="逐項列出完成條件" :disabled="contentReadOnly || !isActiveProject" /></label>
      </section>

      <section class="form-section">
        <header><CheckCircle2 :size="19" aria-hidden="true" /><div><h3>處理結果</h3><p>記錄實際完成內容、差異與後續事項。</p></div></header>
        <label class="field"><textarea v-model="form.completionSummary" rows="8" maxlength="20000" :disabled="!isEditing || contentReadOnly || !isActiveProject" :placeholder="isEditing ? '填寫處理結果與交付內容' : '建立任務後即可填寫處理結果'" /></label>
      </section>

      <footer class="form-actions">
        <UiButton variant="secondary" type="button" @click="router.push({ name: 'project-issues', params: { projectId } })">取消</UiButton>
        <UiButton type="submit" :disabled="!canSave || saving">{{ saving ? '儲存中…' : isEditing ? '儲存變更' : '建立任務' }}</UiButton>
        <UiButton
          v-if="!isEditing"
          variant="secondary"
          type="button"
          :disabled="!canSave || saving"
          @click="save(true)"
        >
          建立任務並繼續
        </UiButton>
      </footer>
    </form>

    <UiSaveToastStack>
      <UiSaveToast
        v-for="notice in saveNotices"
        :key="notice.id"
        inline
        :mode="notice.mode"
        record-label="任務編號"
        :record-key="notice.recordKey"
        @close="clearSaveNotice(notice.id)"
      />
    </UiSaveToastStack>
  </section>
</template>

<style scoped>
.issue-form-page { display: grid; gap: 22px; max-width: 1050px; margin: 0 auto; }
.page-heading { display: grid; gap: 14px; }
.back-link { display: flex; width: fit-content; align-items: center; gap: 6px; padding: 0; color: var(--kk-text-muted); background: transparent; border: 0; cursor: pointer; }
.heading-row { display: flex; align-items: center; justify-content: space-between; gap: 20px; }
.heading-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 9px; }
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
.form-actions { display: flex; justify-content: flex-end; gap: 10px; padding: 4px 0 20px; }
.action-error { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin: 0; padding: 10px 14px; color: var(--kk-danger); background: #fff1f0; border: 1px solid #f1c2bd; border-radius: 7px; font-size: .82rem; }
.readonly-notice { margin: 0; padding: 10px 14px; color: var(--kk-text-muted); background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: 7px; font-size: .82rem; }
.page-state { padding: 42px 24px; text-align: center; background: var(--kk-surface); border: 1px dashed var(--kk-border-strong); border-radius: var(--kk-radius); }
.page-state--error { color: var(--kk-danger); }
@media (max-width: 720px) { .heading-row, .action-error { align-items: stretch; flex-direction: column; } .heading-actions { justify-content: flex-start; } .record-info { grid-template-columns: 1fr 1fr; } .field-grid { grid-template-columns: 1fr; } .field--full { grid-column: 1; } }
</style>
