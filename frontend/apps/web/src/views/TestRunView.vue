<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Play, Plus } from '@lucide/vue'
import { UiActionDialog, UiButton, UiPagination } from '@khaikang/ui'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestPlanResponse, TestRunResponse, TestWorkspaceResponse } from '../api/contracts'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const { showCreated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const runs = ref<TestRunResponse[]>([])
const plans = ref<TestPlanResponse[]>([])
const workspace = ref<TestWorkspaceResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const page = ref(1)
const pageSize = ref(10)
const dialogOpen = ref(false)
const form = ref({ planId: '', name: '' })
const activePlans = computed(() => plans.value.filter((plan) => plan.status === 'active'))
const totalPages = computed(() => Math.max(1, Math.ceil(runs.value.length / pageSize.value)))
const paginatedRuns = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return runs.value.slice(start, start + pageSize.value)
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
  const [workspaceResult, runResult, planResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestRuns(workspaceId.value),
    apiClient.listTestPlans(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  runs.value = runResult.data ?? []
  plans.value = planResult.data ?? []
  error.value = problemMessage(workspaceResult.error ?? runResult.error ?? planResult.error, '')
  loading.value = false
  if (typeof route.query.planId === 'string') openCreate(route.query.planId)
}

function changePage(next: number): void {
  if (next >= 1 && next <= totalPages.value) page.value = next
}

function changePageSize(next: number): void {
  pageSize.value = next
  page.value = 1
}

function openCreate(preferredPlanId?: string): void {
  const preferred = activePlans.value.find((plan) => plan.id === preferredPlanId)
  const selected = preferred ?? activePlans.value[0]
  form.value = {
    planId: selected?.id ?? '',
    name: selected ? `${selected.name} Run` : '',
  }
  dialogOpen.value = true
}

async function createRun(): Promise<void> {
  if (!form.value.planId || !form.value.name.trim()) return
  saving.value = true
  const result = await apiClient.createTestRun(
    workspaceId.value,
    { planId: form.value.planId, name: form.value.name.trim() },
    await secureHeaders(),
  )
  if (result.data) {
    showCreated(t('tests.run.record'), result.data.name)
    await router.push({
      name: 'test-run-detail',
      params: { workspaceId: workspaceId.value, runId: result.data.id },
    })
  } else {
    error.value = problemMessage(result.error, t('tests.run.createFailed'))
  }
  saving.value = false
}

onMounted(load)
</script>

<template>
  <TestWorkspaceSectionFrame v-if="workspace" :workspace="workspace" active-section="runs">
    <template #action>
      <UiButton @click="openCreate()"><Plus :size="18" />{{ t('tests.run.create') }}</UiButton>
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
    <section v-else class="list-panel">
      <header><strong>{{ t('tests.run.title') }}</strong><span>{{ t('tests.run.count', { count: runs.length }) }}</span></header>
      <div class="table-wrap">
        <table>
          <thead><tr><th>{{ t('tests.run.code') }}</th><th>{{ t('tests.run.name') }}</th><th>{{ t('tests.run.statusLabel') }}</th><th>{{ t('tests.run.resultSummary') }}</th><th>{{ t('tests.run.updatedAt') }}</th></tr></thead>
          <tbody>
            <tr
              v-for="run in paginatedRuns"
              :key="run.id"
              tabindex="0"
              @click="router.push({ name: 'test-run-detail', params: { workspaceId, runId: run.id } })"
              @keydown.enter="router.push({ name: 'test-run-detail', params: { workspaceId, runId: run.id } })"
            >
              <td><code>{{ run.code }}</code></td>
              <td><strong>{{ run.name }}</strong><small>{{ run.summary || t('tests.run.snapshotHint') }}</small></td>
              <td><span class="status-pill" :class="run.status">{{ t(`tests.run.status.${run.status}`) }}</span></td>
              <td class="progress-cell">
                <div class="run-progress" :aria-label="`${run.progress.passed}/${run.progress.total}`">
                  <span class="passed" :style="{ width: `${progressPercent(run.progress.passed, run.progress.total)}%` }" />
                  <span class="failed" :style="{ width: `${progressPercent(failedCount(run), run.progress.total)}%` }" />
                  <span class="pending" :style="{ width: `${progressPercent(pendingCount(run), run.progress.total)}%` }" />
                </div>
                <small class="progress-summary">
                  <span class="passed">{{ run.progress.passed }} {{ t('tests.run.result.passed') }}</span>
                  <span class="failed">{{ failedCount(run) }} {{ t('tests.run.result.failed') }}</span>
                  <span class="pending">{{ pendingCount(run) }} {{ t('tests.run.result.not_run') }}</span>
                </small>
              </td>
              <td>{{ d(new Date(run.updatedAt), 'medium') }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <UiPagination
        :page="page"
        :page-size="pageSize"
        :total-count="runs.length"
        :total-pages="totalPages"
        :navigation-label="t('common.pagination.navigation')"
        :summary-label="t('common.pagination.summary', { count: runs.length })"
        :page-size-label="t('common.pagination.pageSize')"
        :previous-label="t('common.pagination.previous')"
        :next-label="t('common.pagination.next')"
        :page-label="t('common.pagination.page', { page, total: totalPages })"
        @page-change="changePage"
        @page-size-change="changePageSize"
      />
    </section>
  </TestWorkspaceSectionFrame>
  <SharedStateBanner v-else type="loading" :title="t('tests.run.loading')" />

  <UiActionDialog
    :open="dialogOpen"
    :title="t('tests.run.create')"
    :description="t('tests.run.snapshotHint')"
    :close-label="t('common.actions.cancel')"
    @close="dialogOpen = false"
  >
    <div class="run-form">
      <label>{{ t('tests.run.plan') }}<select v-model="form.planId">
        <option
          v-for="plan in plans"
          :key="plan.id"
          :value="plan.id"
          :disabled="plan.status !== 'active'"
        >
          {{ plan.code }} · {{ plan.name }} · {{ t(`tests.plan.status.${plan.status}`) }}
        </option>
      </select></label>
      <p v-if="!activePlans.length" class="plan-hint">
        {{ t('tests.run.noActivePlan') }}
        <RouterLink :to="{ name: 'test-plans', params: { workspaceId } }">
          {{ t('tests.run.managePlans') }}
        </RouterLink>
      </p>
      <label>{{ t('tests.run.name') }}<input v-model="form.name" maxlength="200" /></label>
    </div>
    <template #actions>
      <UiButton variant="secondary" @click="dialogOpen = false">{{ t('common.actions.cancel') }}</UiButton>
      <UiButton :disabled="saving || !form.planId || !form.name.trim()" @click="createRun">{{ t('tests.run.create') }}</UiButton>
    </template>
  </UiActionDialog>
</template>

<style scoped>
.list-panel{display:grid;overflow:hidden;background:white;border:1px solid var(--kk-border);border-radius:8px}.list-panel>header{display:flex;justify-content:space-between;padding:14px 18px;border-bottom:1px solid var(--kk-border)}.list-panel>header span{color:var(--kk-text-muted);font-size:.82rem}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:12px 16px;text-align:left;border-bottom:1px solid var(--kk-border)}th{color:var(--kk-text-muted);background:var(--kk-surface-subtle);font-size:.76rem}tbody tr{cursor:pointer}tbody tr:hover{background:var(--kk-accent-soft)}td small{display:block;margin-top:4px;color:var(--kk-text-muted)}code{color:var(--kk-accent);font-weight:700}.status-pill{display:inline-flex;min-height:28px;align-items:center;padding:3px 9px;border-radius:999px;background:var(--kk-surface-subtle);font-size:.75rem;font-weight:700}.status-pill.in_progress,.status-pill.completed{color:var(--kk-accent);background:var(--kk-accent-soft)}.run-form{display:grid;gap:14px}.run-form label{display:grid;gap:6px;font-size:.85rem;font-weight:650}.run-form input,.run-form select{padding:9px;border:1px solid var(--kk-border);border-radius:6px;background:white}.plan-hint{margin:0;color:var(--kk-text-muted);font-size:.84rem}.plan-hint a{color:var(--kk-accent);font-weight:700}
.progress-cell{min-width:190px}.run-progress{display:flex;width:100%;height:8px;overflow:hidden;background:#edf0ee;border-radius:999px}.run-progress span{display:block;min-width:0;height:100%}.run-progress .passed{background:#2d9b62}.run-progress .failed{background:#df6256}.run-progress .pending{background:#c9d0cc}.progress-summary{display:flex!important;gap:9px;margin-top:6px;color:var(--kk-text-muted);font-size:.72rem}.progress-summary .passed{color:#18794e}.progress-summary .failed{color:#b42318}.progress-summary .pending{color:#596560}
</style>
