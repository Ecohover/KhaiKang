<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ChevronDown, ChevronRight, CornerDownRight, Play, Plus } from '@lucide/vue'
import { UiButton, UiPagination, UiStatusBadge, UiTable, UiTableContainer } from '@khaikang/ui'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestRunResponse, TestWorkspaceResponse } from '../api/contracts'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const workspaceId = computed(() => String(route.params.workspaceId))
const runs = ref<TestRunResponse[]>([])
const workspace = ref<TestWorkspaceResponse>()
const loading = ref(true)
const error = ref('')
const page = ref(1)
const pageSize = ref(10)
const rerunningId = ref('')
const expandedPlanIds = ref<Set<string>>(new Set())
const runGroups = computed(() => {
  const grouped = new Map<string, TestRunResponse[]>()
  for (const run of runs.value) grouped.set(run.planId, [...(grouped.get(run.planId) ?? []), run])
  return [...grouped.values()].map((items) => ({ latest: items[0]!, history: items.slice(1) }))
})
const totalPages = computed(() => Math.max(1, Math.ceil(runGroups.value.length / pageSize.value)))
const paginatedGroups = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return runGroups.value.slice(start, start + pageSize.value)
})

function progressPercent(value: number, total: number): number {
  return total ? Math.round((value / total) * 100) : 0
}

function failedCount(run: TestRunResponse): number {
  return run.progress.failed + run.progress.blocked
}

function pendingCount(run: TestRunResponse): number {
  return run.progress.total - run.progress.passed - failedCount(run)
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, runResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestRuns(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  runs.value = runResult.data ?? []
  error.value = problemMessage(workspaceResult.error ?? runResult.error, '')
  loading.value = false
}

function rerunnableRun(group: { latest: TestRunResponse; history: TestRunResponse[] }): TestRunResponse | undefined {
  return [group.latest, ...group.history].find((run) =>
    run.status === 'completed' || run.status === 'cancelled')
}

function changePage(next: number): void {
  if (next >= 1 && next <= totalPages.value) page.value = next
}

function changePageSize(next: number): void {
  pageSize.value = next
  page.value = 1
}

function toggleGroup(planId: string): void {
  const next = new Set(expandedPlanIds.value)
  next.has(planId) ? next.delete(planId) : next.add(planId)
  expandedPlanIds.value = next
}

async function rerun(run: TestRunResponse): Promise<void> {
  if (rerunningId.value || !['completed', 'cancelled'].includes(run.status)) return
  rerunningId.value = run.id
  const result = await apiClient.rerunTestRun(workspaceId.value, run.id, await secureHeaders())
  if (result.data) {
    await router.push({ name: 'test-run-detail', params: { workspaceId: workspaceId.value, runId: result.data.id } })
  } else {
    error.value = problemMessage(result.error, t('tests.run.rerunFailed'))
  }
  rerunningId.value = ''
}

onMounted(load)
</script>

<template>
  <TestWorkspaceSectionFrame v-if="workspace" :workspace="workspace" active-section="runs">
    <template #action>
      <UiButton @click="router.push({ name: 'test-run-new', params: { workspaceId } })"><Plus :size="18" />{{ t('tests.run.create') }}</UiButton>
    </template>
    <SharedStateBanner v-if="loading" type="loading" :title="t('tests.run.loading')" />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="t('tests.run.loadFailed')"
      :description="error"
      :reload-label="t('common.actions.reload')"
      @reload="load"
    />
    <SharedStateBanner
      v-else-if="!runs.length"
      :icon="Play"
      :title="t('tests.run.emptyTitle')"
      :description="t('tests.run.emptyDescription')"
    />
    <UiTableContainer v-else>
      <template #header><strong>{{ t('tests.run.title') }}</strong><span>{{ t('tests.run.count', { count: runs.length }) }}</span></template>
      <UiTable interactive>
          <thead><tr><th class="group-column"></th><th>{{ t('tests.run.code') }}</th><th>{{ t('tests.run.name') }}</th><th>{{ t('tests.run.statusLabel') }}</th><th>{{ t('tests.run.resultSummary') }}</th><th>{{ t('tests.run.updatedAt') }}</th><th>{{ t('common.actions.actions') }}</th></tr></thead>
          <tbody>
            <template v-for="group in paginatedGroups" :key="group.latest.planId">
            <tr
              tabindex="0"
              @click="router.push({ name: 'test-run-detail', params: { workspaceId, runId: group.latest.id } })"
              @keydown.enter="router.push({ name: 'test-run-detail', params: { workspaceId, runId: group.latest.id } })"
            >
              <td class="group-column" @click.stop><button v-if="group.history.length" type="button" class="history-toggle" @click="toggleGroup(group.latest.planId)"><ChevronDown v-if="expandedPlanIds.has(group.latest.planId)" :size="16" /><ChevronRight v-else :size="16" />{{ group.history.length + 1 }}</button></td>
              <td><code>{{ group.latest.code }}</code></td>
              <td><strong>{{ group.latest.name }}</strong><small>{{ group.latest.summary || t('tests.run.snapshotHint') }}</small></td>
              <td><UiStatusBadge :variant="['in_progress', 'completed'].includes(group.latest.status) ? 'success' : 'neutral'">{{ t(`tests.run.status.${group.latest.status}`) }}</UiStatusBadge></td>
              <td class="progress-cell">
                <div class="run-progress" :aria-label="`${group.latest.progress.passed}/${group.latest.progress.total}`">
                  <span class="passed" :style="{ width: `${progressPercent(group.latest.progress.passed, group.latest.progress.total)}%` }" />
                  <span class="failed" :style="{ width: `${progressPercent(failedCount(group.latest), group.latest.progress.total)}%` }" />
                  <span class="pending" :style="{ width: `${progressPercent(pendingCount(group.latest), group.latest.progress.total)}%` }" />
                </div>
                <small class="progress-summary">
                  <span class="passed">{{ group.latest.progress.passed }} {{ t('tests.run.result.passed') }}</span>
                  <span class="failed">{{ failedCount(group.latest) }} {{ t('tests.run.result.failed') }}</span>
                  <span class="pending">{{ pendingCount(group.latest) }} {{ t('tests.run.result.not_run') }}</span>
                </small>
              </td>
              <td>{{ d(new Date(group.latest.updatedAt), 'medium') }}</td>
              <td @click.stop>
                <UiButton
                  v-if="rerunnableRun(group)"
                  variant="secondary"
                  :loading="rerunningId === rerunnableRun(group)?.id"
                  @click="rerun(rerunnableRun(group)!)"
                >{{ t('tests.run.rerun') }}</UiButton>
              </td>
            </tr>
            <tr v-for="(previous, index) in expandedPlanIds.has(group.latest.planId) ? group.history : []" :key="previous.id" class="history-row" :class="{ 'history-row--last': index === group.history.length - 1 }" @click="router.push({ name: 'test-run-detail', params: { workspaceId, runId: previous.id } })">
              <td class="group-column history-marker"><CornerDownRight :size="16" /></td><td><code>{{ previous.code }}</code></td><td>{{ previous.name }}</td><td><UiStatusBadge :variant="['in_progress', 'completed'].includes(previous.status) ? 'success' : 'neutral'">{{ t(`tests.run.status.${previous.status}`) }}</UiStatusBadge></td><td class="progress-cell"><div class="run-progress" :aria-label="`${previous.progress.passed}/${previous.progress.total}`"><span class="passed" :style="{ width: `${progressPercent(previous.progress.passed, previous.progress.total)}%` }" /><span class="failed" :style="{ width: `${progressPercent(failedCount(previous), previous.progress.total)}%` }" /><span class="pending" :style="{ width: `${progressPercent(pendingCount(previous), previous.progress.total)}%` }" /></div><small class="progress-summary"><span class="passed">{{ previous.progress.passed }} {{ t('tests.run.result.passed') }}</span><span class="failed">{{ failedCount(previous) }} {{ t('tests.run.result.failed') }}</span><span class="pending">{{ pendingCount(previous) }} {{ t('tests.run.result.not_run') }}</span></small></td><td>{{ d(new Date(previous.updatedAt), 'medium') }}</td><td></td>
            </tr>
            </template>
          </tbody>
      </UiTable>
      <template #footer><UiPagination
        :page="page"
        :page-size="pageSize"
        :total-count="runGroups.length"
        :total-pages="totalPages"
        :navigation-label="t('common.pagination.navigation')"
        :summary-label="t('common.pagination.summary', { count: runs.length })"
        :page-size-label="t('common.pagination.pageSize')"
        :previous-label="t('common.pagination.previous')"
        :next-label="t('common.pagination.next')"
        :page-label="t('common.pagination.page', { page, total: totalPages })"
        @page-change="changePage"
        @page-size-change="changePageSize"
      /></template>
    </UiTableContainer>
  </TestWorkspaceSectionFrame>
  <SharedStateBanner v-else type="loading" :title="t('tests.run.loading')" />
</template>

<style scoped>
.progress-cell{min-width:190px}.run-progress{display:flex;width:100%;height:8px;overflow:hidden;background:#edf0ee;border-radius:999px}.run-progress span{display:block;min-width:0;height:100%}.run-progress .passed{background:#2d9b62}.run-progress .failed{background:#df6256}.run-progress .pending{background:#c9d0cc}.progress-summary{display:flex!important;gap:9px;margin-top:6px;color:var(--kk-text-muted);font-size:.72rem}.progress-summary .passed{color:#18794e}.progress-summary .failed{color:#b42318}.progress-summary .pending{color:#596560}.group-column{width:56px;padding-right:0!important}.history-row{background:#fbfcfb}.history-row--last{background:#e1eee6}.history-row--last td{border-bottom-color:#9fc2aa}.history-marker{color:var(--kk-text-muted);padding-left:24px!important}.history-toggle{display:inline-flex;align-items:center;gap:4px;padding:6px 8px;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}
</style>
