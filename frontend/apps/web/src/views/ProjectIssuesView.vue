<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ArrowDown, ArrowUp, ArrowUpDown, Columns3, List, Plus } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiAlert, UiButton, UiEmptyState, UiPagination, UiSelect } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  IssueMetadataResponse,
  IssueListQuery,
  IssueResponse,
  ProjectMemberResponse,
  ProjectResponse,
} from '../api/contracts'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import SharedCardSection from '../components/SharedCardSection.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import SharedSearchField from '../components/SharedSearchField.vue'
import SharedFilterToolbar from '../components/SharedFilterToolbar.vue'
import SharedViewTabs from '../components/SharedViewTabs.vue'
import { shouldWarnMissingCompletionSummary } from '../issues/issueWorkflow'
import { useSaveNotice } from '../composables/useSaveNotice'

type IssueView = 'list' | 'board'
type IssueSortBy = NonNullable<IssueListQuery['sortBy']>
type IssueSortDirection = NonNullable<IssueListQuery['sortDirection']>

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
const members = ref<ProjectMemberResponse[]>([])
const loading = ref(true)
const error = ref('')
const actionError = ref('')
const completionWarning = ref<{ issueId: string; key: string }>()
const searchQuery = ref('')
const filterType = ref('')
const filterStatus = ref('')
const filterPriority = ref('')
const filterAssignee = ref('')
const sortBy = ref<IssueSortBy>('updatedAt')
const sortDirection = ref<IssueSortDirection>('desc')
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

watch(activeView, async (view) => {
  if (view !== 'board') return

  searchQuery.value = ''
  filterType.value = ''
  filterStatus.value = ''
  filterPriority.value = ''
  filterAssignee.value = ''
  sortBy.value = 'updatedAt'
  sortDirection.value = 'desc'
  page.value = 1
  await loadPage()
})

async function loadPage(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const [projectResult, issueResult, metadataResult, membersResult] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.listIssues(projectId.value, page.value, pageSize.value, currentFilters()),
      apiClient.getIssueMetadata(projectId.value),
      apiClient.listProjectMembers(projectId.value),
    ])

    if (!projectResult.data || !issueResult.data || !metadataResult.data || !membersResult.data) {
      error.value = problemMessage(
        projectResult.error ?? issueResult.error ?? metadataResult.error ?? membersResult.error,
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
    members.value = membersResult.data.filter((member) => member.status === 'active')
  } catch {
    error.value = t('projects.issues.connectionFailed')
  } finally {
    loading.value = false
  }
}

function currentFilters(): IssueListQuery {
  return {
    ...(searchQuery.value.trim() && { search: searchQuery.value.trim() }),
    ...(filterType.value && { typeCode: filterType.value }),
    ...(filterStatus.value && { statusCode: filterStatus.value }),
    ...(filterPriority.value && { priorityCode: filterPriority.value }),
    ...(filterAssignee.value && filterAssignee.value !== 'unassigned' && {
      assigneeAccountId: filterAssignee.value,
    }),
    ...(filterAssignee.value === 'unassigned' && { unassigned: true }),
    sortBy: sortBy.value,
    sortDirection: sortDirection.value,
  }
}

async function applyFilters(): Promise<void> {
  page.value = 1
  await loadPage()
}

async function clearFilters(): Promise<void> {
  searchQuery.value = ''
  filterType.value = ''
  filterStatus.value = ''
  filterPriority.value = ''
  filterAssignee.value = ''
  sortBy.value = 'updatedAt'
  sortDirection.value = 'desc'
  await applyFilters()
}

async function toggleSort(column: IssueSortBy): Promise<void> {
  if (sortBy.value === column) {
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortBy.value = column
    sortDirection.value = column === 'issueNo' ? 'asc' : 'desc'
  }
  await applyFilters()
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
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'projects' }"
      :back-label="t('projects.create.back')"
      :items="[
        { label: t('projects.list.title'), to: { name: 'projects' } },
        { label: project?.name || t('projects.record'), to: { name: 'project-detail', params: { projectId } } },
        { label: t('projects.issues.title'), active: true },
      ]"
    />
    <ResourcePageHeader
      v-if="project"
      :meta="`${project.code} · PROJECT`"
      :title="project.name"
      :subtitle="t('projects.issues.title')"
    >
      <UiButton
        v-if="canCreate"
        @click="router.push({ name: 'project-issue-new', params: { projectId } })"
      >
        <Plus :size="17" aria-hidden="true" />
        {{ t('projects.issues.create') }}
      </UiButton>
    </ResourcePageHeader>

    <SharedStateBanner
      v-if="loading"
      type="loading"
      :title="t('projects.issues.loading')"
    />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="t('projects.detail.loadError')"
      :description="error"
      show-reload
      @reload="loadPage"
    />

    <template v-else-if="project && metadata">
      <UiAlert v-if="actionError" variant="error">{{ actionError }}</UiAlert>
      <UiAlert v-if="completionWarning" variant="warning">
        <span>{{ t('projects.issues.completionWarning', { key: completionWarning.key }) }}</span>
        <RouterLink
          :to="{
            name: 'project-issue-edit',
            params: { projectId, issueId: completionWarning.issueId },
          }"
        >
          {{ t('projects.issues.addCompletion') }}
        </RouterLink>
      </UiAlert>

      <!-- VIEW TABS (分頁標籤列) -->
      <SharedViewTabs
        v-model="activeView"
        :tabs="[
          { key: 'list', label: t('projects.issues.listView'), icon: List },
          { key: 'board', label: t('projects.issues.boardView'), icon: Columns3 }
        ]"
      />

      <SharedCardSection
        :title="t('projects.issues.title')"
        :description="t('projects.issues.createDescription')"
      >
        <template #headerRight>
          <span class="count-badge">{{ t('projects.issues.count', { count: totalCount }, totalCount) }}</span>
        </template>

        <!-- TOOLBAR: SEARCH & FILTER AREA -->
        <form v-if="activeView === 'list'" class="issue-filter-form" @submit.prevent="applyFilters">
        <SharedFilterToolbar align="start" class="issue-filter-toolbar">
          <SharedSearchField
            v-model="searchQuery"
            placeholder="搜尋任務編號或標題..."
            :clear-label="t('common.search.clear')"
          />
          <UiSelect v-model="filterType" aria-label="任務類型" @change="applyFilters">
            <option value="">所有類型</option>
            <option v-for="type in metadata.types" :key="type.code" :value="type.code">
              {{ type.name }}
            </option>
          </UiSelect>
          <UiSelect v-model="filterStatus" aria-label="任務狀態" @change="applyFilters">
            <option value="">所有狀態</option>
            <option v-for="s in metadata.statuses" :key="s.code" :value="s.code">
              {{ s.name }}
            </option>
          </UiSelect>
          <UiSelect v-model="filterPriority" aria-label="任務優先級" @change="applyFilters">
            <option value="">所有優先級</option>
            <option v-for="priority in metadata.priorities" :key="priority.code" :value="priority.code">
              {{ priority.name }}
            </option>
          </UiSelect>
          <UiSelect v-model="filterAssignee" aria-label="處理人" @change="applyFilters">
            <option value="">所有處理人</option>
            <option value="unassigned">未指派</option>
            <option v-for="member in members" :key="member.accountId" :value="member.accountId">
              {{ member.username }}
            </option>
          </UiSelect>
          <UiButton type="submit" variant="secondary">搜尋</UiButton>
          <UiButton type="button" variant="ghost" @click="clearFilters">清除</UiButton>
        </SharedFilterToolbar>
        </form>

      <div v-if="activeView === 'list'" class="issue-list">
        <div class="issue-list__header">
          <button
            type="button"
            class="column-sort"
            :class="{ 'is-active': sortBy === 'issueNo' }"
            :aria-label="t('projects.issues.sortByColumn', { column: t('projects.issues.columns.key') })"
            :aria-pressed="sortBy === 'issueNo'"
            @click="toggleSort('issueNo')"
          >
            <span>{{ t('projects.issues.columns.key') }}</span>
            <ArrowUp v-if="sortBy === 'issueNo' && sortDirection === 'asc'" :size="14" aria-hidden="true" />
            <ArrowDown v-else-if="sortBy === 'issueNo'" :size="14" aria-hidden="true" />
            <ArrowUpDown v-else :size="14" aria-hidden="true" />
          </button>
          <span>{{ t('projects.issues.columns.title') }}</span>
          <span>{{ t('projects.issues.columns.status') }}</span>
          <span>{{ t('projects.issues.columns.assignee') }}</span>
          <button
            type="button"
            class="column-sort"
            :class="{ 'is-active': sortBy === 'updatedAt' }"
            :aria-label="t('projects.issues.sortByColumn', { column: t('projects.issues.columns.updatedAt') })"
            :aria-pressed="sortBy === 'updatedAt'"
            @click="toggleSort('updatedAt')"
          >
            <span>{{ t('projects.issues.columns.updatedAt') }}</span>
            <ArrowUp v-if="sortBy === 'updatedAt' && sortDirection === 'asc'" :size="14" aria-hidden="true" />
            <ArrowDown v-else-if="sortBy === 'updatedAt'" :size="14" aria-hidden="true" />
            <ArrowUpDown v-else :size="14" aria-hidden="true" />
          </button>
        </div>
        <div v-for="issue in issues" :key="issue.id" class="issue-row">
          <strong>{{ issue.key }}</strong>
          <RouterLink
            :to="{ name: 'project-issue-edit', params: { projectId, issueId: issue.id } }"
          >
            <span class="issue-type">{{ issue.typeName }}</span>{{ issue.title }}
          </RouterLink>
          <UiSelect
            v-if="canChangeStatus"
            :model-value="issue.statusCode"
            :aria-label="t('projects.issues.changeStatus', { key: issue.key })"
            :disabled="updatingIssueId === issue.id"
            @change="changeStatus(issue, ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="status in metadata.statuses" :key="status.code" :value="status.code">
              {{ status.name }}
            </option>
          </UiSelect>
          <span v-else>{{ issue.statusName }}</span>
          <span>{{ issue.assigneeUsername ?? t('projects.issues.unassigned') }}</span>
          <span>{{ formatDate(issue.updatedAt) }}</span>
        </div>
        <UiEmptyState v-if="issues.length === 0" :icon="List" :title="t('projects.issues.emptyTitle')" :description="t('projects.issues.emptyDescription')" />
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
                <UiSelect
                  :model-value="issue.statusCode"
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
                </UiSelect>
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
      </SharedCardSection>
    </template>

  </section>
</template>

<style scoped>
.issues-page {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}
.board-column header { display: flex; align-items: center; }
.issue-row :deep(.ui-select) { width: 100%; }
.ui-alert a { color: inherit; font-weight: 700; }
.count-badge { font-size: 0.82rem; color: var(--kk-text-muted); }
.issue-filter-toolbar { margin-bottom: 16px; }
.issue-list { overflow: hidden; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); }
.issue-list__header, .issue-row { display: grid; grid-template-columns: 90px minmax(240px, 1fr) 140px 150px 150px; gap: 12px; align-items: center; padding: 12px 16px; }
.issue-list__header { color: var(--kk-text-muted); background: var(--kk-surface-subtle); border-bottom: 1px solid var(--kk-border); font-size: .75rem; font-weight: 700; }
.column-sort { display: inline-flex; width: max-content; align-items: center; gap: 5px; padding: 0; color: inherit; background: transparent; border: 0; font: inherit; cursor: pointer; }
.column-sort:hover, .column-sort.is-active { color: var(--kk-accent); }
.issue-row { min-height: 58px; border-bottom: 1px solid var(--kk-border); font-size: .82rem; }
.issue-row:last-child { border-bottom: 0; }
.issue-row > strong { color: var(--kk-accent); }
.issue-row > a { display: flex; align-items: center; gap: 8px; color: var(--kk-text); font-weight: 650; text-decoration: none; }
.issue-row > a:hover { color: var(--kk-accent); }
.issue-type { padding: 2px 6px; color: var(--kk-text-muted); background: var(--kk-surface-subtle); border-radius: 4px; font-size: .7rem; font-weight: 500; }
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
.board-status-field :deep(.ui-select) { width: 100%; }
@media (max-width: 720px) { .action-warning { align-items: flex-start; flex-direction: column; } .issue-list__header, .issue-row { grid-template-columns: 80px 1fr 110px; } .issue-list__header span:nth-child(n + 4), .issue-row > span:nth-child(n + 4) { display: none; } }
</style>
