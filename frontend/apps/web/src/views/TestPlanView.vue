<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ClipboardList, Plus } from '@lucide/vue'
import { UiActionDialog, UiButton, UiPagination } from '@khaikang/ui'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestCaseResponse, TestPlanResponse, TestPlanStatus, TestWorkspaceResponse } from '../api/contracts'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const plans = ref<TestPlanResponse[]>([])
const cases = ref<TestCaseResponse[]>([])
const workspace = ref<TestWorkspaceResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const page = ref(1)
const pageSize = ref(10)
const dialogOpen = ref(false)
const editing = ref<TestPlanResponse>()
const form = ref({
  name: '',
  description: '',
  status: 'draft' as TestPlanStatus,
  caseIds: [] as string[],
})

const activeCases = computed(() => cases.value.filter((item) => item.status === 'active'))
const totalPages = computed(() => Math.max(1, Math.ceil(plans.value.length / pageSize.value)))
const paginatedPlans = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return plans.value.slice(start, start + pageSize.value)
})

function openCreate(): void {
  editing.value = undefined
  form.value = { name: '', description: '', status: 'draft', caseIds: [] }
  dialogOpen.value = true
}

function openEdit(plan: TestPlanResponse): void {
  editing.value = plan
  form.value = {
    name: plan.name,
    description: plan.description ?? '',
    status: plan.status,
    caseIds: plan.items.map((item) => item.caseId),
  }
  dialogOpen.value = true
}

function createRunFromPlan(plan: TestPlanResponse): void {
  void router.push({
    name: 'test-runs',
    params: { workspaceId: workspaceId.value },
    query: { planId: plan.id },
  })
}

function toggleCase(caseId: string): void {
  form.value.caseIds = form.value.caseIds.includes(caseId)
    ? form.value.caseIds.filter((id) => id !== caseId)
    : [...form.value.caseIds, caseId]
}

function move(caseId: string, offset: number): void {
  const values = [...form.value.caseIds]
  const index = values.indexOf(caseId)
  const target = index + offset
  if (index < 0 || target < 0 || target >= values.length) return
  ;[values[index], values[target]] = [values[target]!, values[index]!]
  form.value.caseIds = values
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, planResult, caseResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestPlans(workspaceId.value),
    apiClient.listTestCases(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  plans.value = planResult.data ?? []
  cases.value = caseResult.data ?? []
  error.value = problemMessage(workspaceResult.error ?? planResult.error ?? caseResult.error, '')
  loading.value = false
  const selectedId = typeof route.query.planId === 'string' ? route.query.planId : ''
  const selected = plans.value.find((item) => item.id === selectedId)
  if (selected) openEdit(selected)
}

function changePage(next: number): void {
  if (next >= 1 && next <= totalPages.value) page.value = next
}

function changePageSize(next: number): void {
  pageSize.value = next
  page.value = 1
}

async function save(): Promise<void> {
  if (form.value.status === 'active' && !form.value.caseIds.length) return
  saving.value = true
  error.value = ''
  const body = {
    name: form.value.name.trim(),
    description: form.value.description.trim() || null,
    caseIds: form.value.caseIds,
  }
  const result = editing.value
    ? await apiClient.updateTestPlan(
        workspaceId.value,
        editing.value.id,
        { ...body, status: form.value.status, version: editing.value.version },
        await secureHeaders(),
      )
    : await apiClient.createTestPlan(workspaceId.value, body, await secureHeaders())
  if (result.data) {
    editing.value ? showUpdated(t('tests.plan.record'), result.data.name) : showCreated(t('tests.plan.record'), result.data.name)
    dialogOpen.value = false
    await router.replace({ name: 'test-plans', params: { workspaceId: workspaceId.value } })
    await load()
  } else {
    error.value = problemMessage(result.error, t('tests.plan.saveFailed'))
  }
  saving.value = false
}

watch(() => route.query.planId, () => {
  const selected = plans.value.find((item) => item.id === route.query.planId)
  if (selected) openEdit(selected)
})
onMounted(load)
</script>

<template>
  <TestWorkspaceSectionFrame v-if="workspace" :workspace="workspace" active-section="plans">
    <template #action>
      <UiButton @click="openCreate"><Plus :size="18" />{{ t('tests.plan.create') }}</UiButton>
    </template>
    <SharedStateBanner v-if="loading" type="loading" :title="t('tests.plan.loading')" />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="t('tests.plan.loadFailed')"
      :description="error"
      :reload-label="t('common.actions.reload')"
      @reload="load"
    />
    <SharedStateBanner
      v-else-if="!plans.length"
      :icon="ClipboardList"
      :title="t('tests.plan.emptyTitle')"
      :description="t('tests.plan.emptyDescription')"
    />
    <section v-else class="list-panel">
      <header><strong>{{ t('tests.plan.title') }}</strong><span>{{ t('tests.plan.count', { count: plans.length }) }}</span></header>
      <div class="table-wrap">
        <table>
          <thead><tr><th>{{ t('tests.plan.code') }}</th><th>{{ t('tests.plan.name') }}</th><th>{{ t('tests.plan.cases') }}</th><th>{{ t('tests.plan.statusLabel') }}</th><th>{{ t('tests.plan.updatedAt') }}</th><th>{{ t('common.actions.actions') }}</th></tr></thead>
          <tbody>
            <tr v-for="plan in paginatedPlans" :key="plan.id" tabindex="0" @click="openEdit(plan)" @keydown.enter="openEdit(plan)">
              <td><code>{{ plan.code }}</code></td>
              <td><strong>{{ plan.name }}</strong><small>{{ plan.description || t('tests.plan.noDescription') }}</small></td>
              <td>{{ plan.items.length }}</td>
              <td><span class="status-pill" :class="plan.status">{{ t(`tests.plan.status.${plan.status}`) }}</span></td>
              <td>{{ d(new Date(plan.updatedAt), 'medium') }}</td>
              <td @click.stop>
                <UiButton
                  v-if="plan.status === 'active'"
                  variant="secondary"
                  @click="createRunFromPlan(plan)"
                >
                  {{ t('tests.plan.createRun') }}
                </UiButton>
                <small v-else>{{ t('tests.plan.activateToRun') }}</small>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <UiPagination
        :page="page"
        :page-size="pageSize"
        :total-count="plans.length"
        :total-pages="totalPages"
        :navigation-label="t('common.pagination.navigation')"
        :summary-label="t('common.pagination.summary', { count: plans.length })"
        :page-size-label="t('common.pagination.pageSize')"
        :previous-label="t('common.pagination.previous')"
        :next-label="t('common.pagination.next')"
        :page-label="t('common.pagination.page', { page, total: totalPages })"
        @page-change="changePage"
        @page-size-change="changePageSize"
      />
    </section>
  </TestWorkspaceSectionFrame>
  <SharedStateBanner v-else type="loading" :title="t('tests.plan.loading')" />

  <UiActionDialog
    :open="dialogOpen"
    :title="editing ? t('tests.plan.edit') : t('tests.plan.create')"
    :description="t('tests.plan.scopeHint')"
    :close-label="t('common.actions.cancel')"
    @close="dialogOpen = false"
  >
    <form class="plan-form" @submit.prevent="save">
      <label>
        {{ t('tests.plan.name') }}
        <input v-model="form.name" maxlength="200" :placeholder="t('tests.plan.namePlaceholder')" />
        <small>{{ t('tests.plan.nameHint') }}</small>
      </label>
      <label>{{ t('tests.plan.descriptionLabel') }}<textarea v-model="form.description" maxlength="4000" /></label>
      <label v-if="editing">{{ t('tests.plan.statusLabel') }}
        <select v-model="form.status" :disabled="editing.status === 'archived'">
          <option value="draft">{{ t('tests.plan.status.draft') }}</option>
          <option value="active">{{ t('tests.plan.status.active') }}</option>
          <option value="archived">{{ t('tests.plan.status.archived') }}</option>
        </select>
      </label>
      <fieldset>
        <legend>{{ t('tests.plan.cases') }}</legend>
        <label v-for="testCase in activeCases" :key="testCase.id" class="case-choice">
          <input
            type="checkbox"
            :checked="form.caseIds.includes(testCase.id)"
            @change="toggleCase(testCase.id)"
          />
          <span>{{ testCase.title }}</span>
          <span v-if="form.caseIds.includes(testCase.id)" class="order-actions">
            <button type="button" @click.prevent="move(testCase.id, -1)">↑</button>
            <button type="button" @click.prevent="move(testCase.id, 1)">↓</button>
          </span>
        </label>
      </fieldset>
    </form>
    <template #actions>
      <UiButton variant="secondary" @click="dialogOpen = false">{{ t('common.actions.cancel') }}</UiButton>
      <UiButton :disabled="saving" @click="save">{{ t('common.actions.save') }}</UiButton>
    </template>
  </UiActionDialog>
</template>

<style scoped>
.list-panel{display:grid;overflow:hidden;background:white;border:1px solid var(--kk-border);border-radius:8px}.list-panel>header{display:flex;justify-content:space-between;padding:14px 18px;border-bottom:1px solid var(--kk-border)}.list-panel>header span{color:var(--kk-text-muted);font-size:.82rem}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{padding:12px 16px;text-align:left;border-bottom:1px solid var(--kk-border)}th{color:var(--kk-text-muted);background:var(--kk-surface-subtle);font-size:.76rem}tbody tr{cursor:pointer}tbody tr:hover{background:var(--kk-accent-soft)}td small{display:block;margin-top:4px;color:var(--kk-text-muted)}code{color:var(--kk-accent);font-weight:700}.status-pill{padding:4px 8px;border-radius:999px;background:var(--kk-surface-subtle);font-size:.75rem;font-weight:700}.status-pill.active{color:var(--kk-accent);background:var(--kk-accent-soft)}.plan-form{display:grid;gap:14px}.plan-form label{display:grid;gap:6px;font-size:.85rem;font-weight:650}.plan-form small{color:var(--kk-text-muted);font-weight:400}.plan-form input,.plan-form textarea,.plan-form select{padding:9px;border:1px solid var(--kk-border);border-radius:6px;background:white}.plan-form textarea{min-height:70px;resize:vertical}.plan-form fieldset{display:grid;max-height:280px;gap:7px;padding:10px;overflow:auto;border:1px solid var(--kk-border);border-radius:6px}.case-choice{grid-template-columns:auto minmax(0,1fr) auto!important;align-items:center}.order-actions{display:flex;gap:4px}.order-actions button{border:1px solid var(--kk-border);background:white;border-radius:4px;cursor:pointer}
</style>
  workspace.value = workspaceResult.data
