<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Columns3, List, Plus } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiPagination } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  IssueMetadataResponse,
  IssueResponse,
  ProjectResponse,
} from '../api/contracts'
import { shouldWarnMissingCompletionSummary } from '../issues/issueWorkflow'
import { useSaveNotice } from '../composables/useSaveNotice'

type IssueView = 'list' | 'board'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const projectId = computed(() => String(route.params.projectId))
const project = ref<ProjectResponse>()
const issues = ref<IssueResponse[]>([])
const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const totalPages = ref(0)
const metadata = ref<IssueMetadataResponse>()
const loading = ref(true)
const error = ref('')
const actionError = ref('')
const completionWarning = ref<{ issueId: string; key: string }>()
const activeView = ref<IssueView>('list')
const updatingIssueId = ref<string>()
const draggedIssueId = ref<string>()
const { showCreated, showUpdated } = useSaveNotice()

const canCreate = computed(
  () => project.value?.status === 'active'
    && project.value.currentUserPermissions.includes('issue.create'),
)
const canChangeStatus = computed(
  () => project.value?.status === 'active'
    && project.value.currentUserPermissions.includes('issue.status.change'),
)

onMounted(async () => {
  await loadPage()
  if (route.query.savedMode === 'created' && typeof route.query.savedKey === 'string') {
    showCreated(t('projects.issues.record'), route.query.savedKey)
    await router.replace({ query: {} })
  }
})

async function loadPage(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const [projectResult, issueResult, metadataResult] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.listIssues(projectId.value, page.value, pageSize.value),
      apiClient.getIssueMetadata(projectId.value),
    ])

    if (!projectResult.data || !issueResult.data || !metadataResult.data) {
      error.value = problemMessage(
        projectResult.error ?? issueResult.error ?? metadataResult.error,
        t('projects.issues.loadFailed'),
      )
      return
    }

    project.value = projectResult.data
    issues.value = issueResult.data.items
    page.value = issueResult.data.page
    pageSize.value = issueResult.data.pageSize
    totalCount.value = issueResult.data.totalCount
    totalPages.value = issueResult.data.totalPages
    metadata.value = metadataResult.data
  } catch {
    error.value = t('projects.issues.connectionFailed')
  } finally {
    loading.value = false
  }
}

async function changePage(nextPage: number): Promise<void> {
  if (nextPage === page.value || nextPage < 1 || nextPage > totalPages.value) return

  page.value = nextPage
  await loadPage()
}

async function changePageSize(nextPageSize: number): Promise<void> {
  if (nextPageSize === pageSize.value) return

  pageSize.value = nextPageSize
  page.value = 1
  await loadPage()
}

async function changeStatus(issue: IssueResponse, statusCode: string): Promise<void> {
  if (!canChangeStatus.value || issue.statusCode === statusCode || updatingIssueId.value) return

  updatingIssueId.value = issue.id
  actionError.value = ''
  completionWarning.value = undefined
  const shouldWarnAboutResult = shouldWarnMissingCompletionSummary(issue, statusCode)
  try {
    const result = await apiClient.updateIssueStatus(
      projectId.value,
      issue.id,
      { statusCode, version: issue.version },
      await secureHeaders(),
    )

    if (!result.data) {
      actionError.value = problemMessage(result.error, t('projects.issues.statusUpdateFailed'))
      return
    }

    const index = issues.value.findIndex((item) => item.id === issue.id)
    if (index >= 0) issues.value[index] = result.data
    showUpdated(t('projects.issues.record'), result.data.key)
    if (shouldWarnAboutResult) {
      completionWarning.value = { issueId: result.data.id, key: result.data.key }
    }
  } catch {
    actionError.value = t('projects.issues.connectionFailed')
  } finally {
    updatingIssueId.value = undefined
    draggedIssueId.value = undefined
  }
}

function startDrag(issue: IssueResponse): void {
  if (canChangeStatus.value) draggedIssueId.value = issue.id
}

async function dropOnStatus(statusCode: string): Promise<void> {
  const issue = issues.value.find((item) => item.id === draggedIssueId.value)
  if (issue) await changeStatus(issue, statusCode)
}

function issuesForStatus(statusCode: string): IssueResponse[] {
  return issues.value.filter((issue) => issue.statusCode === statusCode)
}

function formatDate(value: string): string {
  return d(new Date(value), 'dateTime')
}
</script>

<template>
  <section class="issues-page">
    <header class="page-heading">
      <div>
        <p>{{ project?.code }}</p>
        <h2>{{ t('projects.issues.title') }}</h2>
        <span>{{ project?.name }}</span>
      </div>
      <UiButton
        v-if="canCreate"
        @click="router.push({ name: 'project-issue-new', params: { projectId } })"
      >
        <Plus :size="17" aria-hidden="true" />
        {{ t('projects.issues.create') }}
      </UiButton>
    </header>

    <p v-if="loading" class="page-state">{{ t('projects.issues.loading') }}</p>
    <div v-else-if="error" class="page-state page-state--error" role="alert">
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadPage">{{ t('common.actions.reload') }}</UiButton>
    </div>

    <template v-else-if="project && metadata">
      <p v-if="actionError" class="action-error" role="alert">{{ actionError }}</p>
      <div v-if="completionWarning" class="action-warning" role="status">
        <span>{{ t('projects.issues.completionWarning', { key: completionWarning.key }) }}</span>
        <RouterLink
          :to="{
            name: 'project-issue-edit',
            params: { projectId, issueId: completionWarning.issueId },
          }"
        >
          {{ t('projects.issues.addCompletion') }}
        </RouterLink>
      </div>

      <div class="issue-toolbar">
        <div class="view-switcher" role="group" :aria-label="t('projects.issues.viewMode')">
          <button type="button" :class="{ 'is-active': activeView === 'list' }" @click="activeView = 'list'">
            <List :size="16" aria-hidden="true" />{{ t('projects.issues.listView') }}
          </button>
          <button type="button" :class="{ 'is-active': activeView === 'board' }" @click="activeView = 'board'">
            <Columns3 :size="16" aria-hidden="true" />{{ t('projects.issues.boardView') }}
          </button>
        </div>
        <span>{{ t('projects.issues.count', { count: totalCount }, totalCount) }}</span>
      </div>

      <div v-if="activeView === 'list'" class="issue-list">
        <div class="issue-list__header">
          <span>{{ t('projects.issues.columns.key') }}</span><span>{{ t('projects.issues.columns.title') }}</span><span>{{ t('projects.issues.columns.status') }}</span><span>{{ t('projects.issues.columns.assignee') }}</span><span>{{ t('projects.issues.columns.updatedAt') }}</span>
        </div>
        <div v-for="issue in issues" :key="issue.id" class="issue-row">
          <strong>{{ issue.key }}</strong>
          <RouterLink
            :to="{ name: 'project-issue-edit', params: { projectId, issueId: issue.id } }"
          >
            <span class="issue-type">{{ issue.typeName }}</span>{{ issue.title }}
          </RouterLink>
          <select
            v-if="canChangeStatus"
            :value="issue.statusCode"
            :aria-label="t('projects.issues.changeStatus', { key: issue.key })"
            :disabled="updatingIssueId === issue.id"
            @change="changeStatus(issue, ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="status in metadata.statuses" :key="status.code" :value="status.code">
              {{ status.name }}
            </option>
          </select>
          <span v-else>{{ issue.statusName }}</span>
          <span>{{ issue.assigneeUsername ?? t('projects.issues.unassigned') }}</span>
          <span>{{ formatDate(issue.updatedAt) }}</span>
        </div>
        <div v-if="issues.length === 0" class="empty-state">
          <List :size="28" aria-hidden="true" /><strong>{{ t('projects.issues.emptyTitle') }}</strong><span>{{ t('projects.issues.emptyDescription') }}</span>
        </div>
      </div>

      <div v-else class="issue-board">
        <section
          v-for="status in metadata.statuses"
          :key="status.code"
          class="board-column"
          @dragover.prevent
          @drop="dropOnStatus(status.code)"
        >
          <header><strong>{{ status.name }}</strong><span>{{ issuesForStatus(status.code).length }}</span></header>
          <div class="board-dropzone">
            <article
              v-for="issue in issuesForStatus(status.code)"
              :key="issue.id"
              class="issue-card"
              :class="{ 'is-updating': updatingIssueId === issue.id }"
              :draggable="canChangeStatus"
              @dragstart="startDrag(issue)"
              @dragend="draggedIssueId = undefined"
            >
              <span>{{ issue.key }} · {{ issue.typeName }}</span>
              <RouterLink
                :to="{ name: 'project-issue-edit', params: { projectId, issueId: issue.id } }"
              >
                {{ issue.title }}
              </RouterLink>
              <footer><span>{{ issue.priorityName }}</span><span>{{ issue.assigneeUsername ?? t('projects.issues.unassigned') }}</span></footer>
              <label v-if="canChangeStatus" class="board-status-field">
                <span>{{ t('projects.issues.columns.status') }}</span>
                <select
                  :value="issue.statusCode"
                  :disabled="updatingIssueId === issue.id"
                  @change="changeStatus(issue, ($event.target as HTMLSelectElement).value)"
                >
                  <option
                    v-for="statusOption in metadata.statuses"
                    :key="statusOption.code"
                    :value="statusOption.code"
                  >
                    {{ statusOption.name }}
                  </option>
                </select>
              </label>
            </article>
            <span v-if="issuesForStatus(status.code).length === 0" class="board-empty">{{ t('projects.issues.emptyStatus') }}</span>
          </div>
        </section>
      </div>

      <UiPagination
        :page="page"
        :page-size="pageSize"
        :total-count="totalCount"
        :total-pages="totalPages"
        :disabled="loading"
        :navigation-label="t('common.pagination.navigation')"
        :summary-label="t('common.pagination.summary', { count: totalCount })"
        :page-size-label="t('common.pagination.pageSize')"
        :previous-label="t('common.pagination.previous')"
        :next-label="t('common.pagination.next')"
        :page-label="t('common.pagination.page', { page: totalPages === 0 ? 0 : page, total: totalPages })"
        @page-change="changePage"
        @page-size-change="changePageSize"
      />
    </template>

  </section>
</template>

<style scoped>
.issues-page { display: grid; gap: 22px; }
.page-heading, .issue-toolbar, .view-switcher, .board-column header, .create-panel__heading { display: flex; align-items: center; }
.page-heading, .issue-toolbar, .create-panel__heading { justify-content: space-between; gap: 16px; }
.page-heading p, .page-heading h2 { margin: 0; }
.page-heading p { color: var(--kk-accent); font-size: .75rem; font-weight: 750; letter-spacing: .08em; }
.page-heading h2 { font-size: 1.8rem; }
.page-heading span, .issue-toolbar > span { color: var(--kk-text-muted); font-size: .8rem; }
.create-panel { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; padding: 20px; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.create-panel__heading, .create-panel__actions, .field--wide { grid-column: 1 / -1; }
.create-panel__heading div { display: grid; gap: 3px; }
.create-panel__heading span { color: var(--kk-text-muted); font-size: .78rem; }
.create-panel__heading button { color: var(--kk-text-muted); background: transparent; border: 0; cursor: pointer; }
.field { display: grid; align-content: start; gap: 6px; font-size: .8rem; font-weight: 650; }
.field input, .field select, .field textarea, .issue-row select { width: 100%; padding: 9px 10px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: 6px; font: inherit; }
.field textarea { resize: vertical; }
.create-panel__actions { display: flex; justify-content: flex-end; }
.action-error { margin: 0; padding: 10px 14px; color: var(--kk-danger); background: #fff1f0; border: 1px solid #f1c2bd; border-radius: 7px; font-size: .82rem; }
.action-warning { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin: 0; padding: 10px 14px; color: #715514; background: #fff8df; border: 1px solid #e9d48a; border-radius: 7px; font-size: .82rem; }
.action-warning a { color: inherit; font-weight: 700; }
.issue-toolbar { min-height: 42px; }
.view-switcher { gap: 3px; padding: 3px; background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: 8px; }
.view-switcher button { display: flex; min-height: 34px; align-items: center; gap: 7px; padding: 6px 12px; color: var(--kk-text-muted); background: transparent; border: 0; border-radius: 6px; cursor: pointer; font-weight: 650; }
.view-switcher button.is-active { color: var(--kk-text); background: var(--kk-surface); box-shadow: 0 1px 3px rgb(27 46 35 / 9%); }
.issue-list { overflow: hidden; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.issue-list__header, .issue-row { display: grid; grid-template-columns: 90px minmax(240px, 1fr) 140px 150px 150px; gap: 12px; align-items: center; padding: 12px 16px; }
.issue-list__header { color: var(--kk-text-muted); background: var(--kk-surface-subtle); border-bottom: 1px solid var(--kk-border); font-size: .75rem; font-weight: 700; }
.issue-row { min-height: 58px; border-bottom: 1px solid var(--kk-border); font-size: .82rem; }
.issue-row:last-child { border-bottom: 0; }
.issue-row > strong { color: var(--kk-accent); }
.issue-row > a { display: flex; align-items: center; gap: 8px; color: var(--kk-text); font-weight: 650; text-decoration: none; }
.issue-row > a:hover { color: var(--kk-accent); }
.issue-type { padding: 2px 6px; color: var(--kk-text-muted); background: var(--kk-surface-subtle); border-radius: 4px; font-size: .7rem; font-weight: 500; }
.empty-state { display: grid; min-height: 260px; place-content: center; justify-items: center; gap: 8px; color: var(--kk-text-muted); text-align: center; }
.empty-state strong { color: var(--kk-text); }
.empty-state span { font-size: .82rem; }
.issue-board { display: grid; overflow-x: auto; grid-template-columns: repeat(4, minmax(250px, 1fr)); gap: 14px; padding-bottom: 8px; }
.board-column { min-height: 440px; padding: 12px; background: #eef1ef; border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.board-column header { justify-content: space-between; padding: 3px 3px 12px; font-size: .82rem; }
.board-column header span { display: grid; width: 22px; height: 22px; place-items: center; color: var(--kk-text-muted); background: var(--kk-surface); border-radius: 50%; font-size: .7rem; }
.board-dropzone { display: grid; min-height: 370px; align-content: start; gap: 9px; padding: 2px; border: 1px dashed transparent; border-radius: 7px; }
.board-dropzone:has(.board-empty) { place-items: center; border-color: var(--kk-border-strong); }
.board-empty { color: var(--kk-text-muted); font-size: .78rem; }
.issue-card { display: grid; gap: 10px; padding: 13px; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: 8px; box-shadow: 0 1px 2px rgb(27 46 35 / 5%); cursor: grab; }
.issue-card.is-updating { opacity: .55; }
.issue-card > span { color: var(--kk-text-muted); font-size: .7rem; }
.issue-card > a { color: var(--kk-text); font-size: .86rem; font-weight: 700; text-decoration: none; }
.issue-card > a:hover { color: var(--kk-accent); }
.issue-card footer { display: flex; justify-content: space-between; gap: 8px; color: var(--kk-text-muted); font-size: .72rem; }
.board-status-field { display: grid; gap: 5px; color: var(--kk-text-muted); font-size: .7rem; }
.board-status-field select { width: 100%; min-height: 34px; padding: 6px 8px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: 6px; font: inherit; }
.page-state { padding: 42px 24px; text-align: center; background: var(--kk-surface); border: 1px dashed var(--kk-border-strong); border-radius: var(--kk-radius); }
.page-state--error { color: var(--kk-danger); }
@media (max-width: 900px) { .create-panel { grid-template-columns: 1fr 1fr; } .field--wide { grid-column: 1 / -1; } }
@media (max-width: 720px) { .create-panel { grid-template-columns: 1fr; } .field, .create-panel__heading, .create-panel__actions { grid-column: 1; } .action-warning { align-items: flex-start; flex-direction: column; } .issue-list__header, .issue-row { grid-template-columns: 80px 1fr 110px; } .issue-list__header span:nth-child(n + 4), .issue-row > span:nth-child(n + 4) { display: none; } }
</style>
