<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ClipboardList, Plus } from '@lucide/vue'
import { UiActionDialog, UiButton, UiPagination, UiTable, UiTableContainer } from '@khaikang/ui'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  TestCaseResponse,
  TestPlanResponse,
  TestPlanStatus,
  TestSuiteResponse,
  TestWorkspaceResponse,
} from '../api/contracts'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestPlanCaseTree from '../components/TestPlanCaseTree.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const { showUpdated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const plans = ref<TestPlanResponse[]>([])
const cases = ref<TestCaseResponse[]>([])
const suites = ref<TestSuiteResponse[]>([])
const workspace = ref<TestWorkspaceResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const page = ref(1)
const pageSize = ref(10)
const statusUpdatingId = ref('')
const dialogOpen = ref(false)
const editing = ref<TestPlanResponse>()
const form = ref({
  name: '',
  description: '',
  status: 'draft' as TestPlanStatus,
  caseIds: [] as string[],
})

const activeCases = computed(() => cases.value.filter((item) => item.status === 'active'))
const activeSuites = computed(() => suites.value.filter((item) => item.status === 'active'))
const totalPages = computed(() => Math.max(1, Math.ceil(plans.value.length / pageSize.value)))
const paginatedPlans = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return plans.value.slice(start, start + pageSize.value)
})

function openEdit(plan: TestPlanResponse): void {
  void router.push({ name: 'test-plan-edit', params: { workspaceId: workspaceId.value, planId: plan.id } })
}

function createRunFromPlan(plan: TestPlanResponse): void {
  void router.push({
    name: 'test-run-new',
    params: { workspaceId: workspaceId.value },
    query: { planId: plan.id },
  })
}

function toggleCase(caseId: string): void {
  form.value.caseIds = form.value.caseIds.includes(caseId)
    ? form.value.caseIds.filter((id) => id !== caseId)
    : [...form.value.caseIds, caseId]
}

function casesForSuite(suiteId: string): TestCaseResponse[] {
  const includedSuiteIds = new Set([suiteId])
  let foundChild = true

  while (foundChild) {
    foundChild = false
    for (const suite of activeSuites.value) {
      if (suite.parentId && includedSuiteIds.has(suite.parentId) && !includedSuiteIds.has(suite.id)) {
        includedSuiteIds.add(suite.id)
        foundChild = true
      }
    }
  }

  return activeCases.value.filter((testCase) => includedSuiteIds.has(testCase.suiteId))
}

function isSuiteSelected(suiteId: string): boolean {
  const suiteCases = casesForSuite(suiteId)
  return suiteCases.length > 0 && suiteCases.every((testCase) => form.value.caseIds.includes(testCase.id))
}

function toggleSuite(suiteId: string): void {
  const suiteCaseIds = casesForSuite(suiteId).map((testCase) => testCase.id)
  if (!suiteCaseIds.length) return

  if (isSuiteSelected(suiteId)) {
    form.value.caseIds = form.value.caseIds.filter((caseId) => !suiteCaseIds.includes(caseId))
    return
  }

  form.value.caseIds = [...form.value.caseIds, ...suiteCaseIds.filter(
    (caseId) => !form.value.caseIds.includes(caseId),
  )]
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
  const [workspaceResult, planResult, caseResult, suiteResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestPlans(workspaceId.value),
    apiClient.listTestCases(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  plans.value = planResult.data ?? []
  cases.value = caseResult.data ?? []
  suites.value = suiteResult.data ?? []
  error.value = problemMessage(
    workspaceResult.error ?? planResult.error ?? caseResult.error ?? suiteResult.error,
    '',
  )
  loading.value = false
}

function changePage(next: number): void {
  if (next >= 1 && next <= totalPages.value) page.value = next
}

function changePageSize(next: number): void {
  pageSize.value = next
  page.value = 1
}

async function updateStatus(plan: TestPlanResponse, event: Event): Promise<void> {
  const status = (event.target as HTMLSelectElement).value as TestPlanStatus
  if (status === plan.status || statusUpdatingId.value) return
  if (status === 'active' && !plan.items.length) {
    error.value = t('tests.plan.caseRequired')
    return
  }

  statusUpdatingId.value = plan.id
  error.value = ''
  const result = await apiClient.updateTestPlan(
    workspaceId.value,
    plan.id,
    {
      name: plan.name,
      description: plan.description,
      status,
      version: plan.version,
      caseIds: plan.items.map((item) => item.caseId),
    },
    await secureHeaders(),
  )
  if (result.data) {
    plans.value = plans.value.map((item) => item.id === result.data!.id ? result.data! : item)
    showUpdated(t('tests.plan.record'), result.data.name)
  } else {
    error.value = problemMessage(result.error, t('tests.plan.saveFailed'))
  }
  statusUpdatingId.value = ''
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
  if (!editing.value) return
  const result = await apiClient.updateTestPlan(
    workspaceId.value,
    editing.value.id,
    { ...body, status: form.value.status, version: editing.value.version },
    await secureHeaders(),
  )
  if (result.data) {
    showUpdated(t('tests.plan.record'), result.data.name)
    dialogOpen.value = false
    await router.replace({ name: 'test-plans', params: { workspaceId: workspaceId.value } })
    await load()
  } else {
    error.value = problemMessage(result.error, t('tests.plan.saveFailed'))
  }
  saving.value = false
}

onMounted(load)
</script>

<template>
  <TestWorkspaceSectionFrame v-if="workspace" :workspace="workspace" active-section="plans">
    <template #action>
      <UiButton @click="router.push({ name: 'test-plan-new', params: { workspaceId } })"><Plus :size="18" />{{ t('tests.plan.create') }}</UiButton>
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
    <UiTableContainer v-else>
      <template #header><strong>{{ t('tests.plan.title') }}</strong><span>{{ t('tests.plan.count', { count: plans.length }) }}</span></template>
      <UiTable interactive>
          <thead><tr><th>{{ t('tests.plan.code') }}</th><th>{{ t('tests.plan.name') }}</th><th>{{ t('tests.plan.cases') }}</th><th>{{ t('tests.plan.statusLabel') }}</th><th>{{ t('tests.plan.updatedAt') }}</th><th>{{ t('common.actions.actions') }}</th></tr></thead>
          <tbody>
            <tr v-for="plan in paginatedPlans" :key="plan.id" tabindex="0" @click="openEdit(plan)" @keydown.enter="openEdit(plan)">
              <td><code>{{ plan.code }}</code></td>
              <td><strong>{{ plan.name }}</strong><small>{{ plan.description || t('tests.plan.noDescription') }}</small></td>
              <td>{{ plan.items.length }}</td>
              <td @click.stop @keydown.stop>
                <select
                  class="status-select"
                  :class="plan.status"
                  :value="plan.status"
                  :disabled="statusUpdatingId === plan.id"
                  :aria-label="t('tests.plan.statusLabel')"
                  @change="updateStatus(plan, $event)"
                >
                  <option value="draft">{{ t('tests.plan.status.draft') }}</option>
                  <option value="active">{{ t('tests.plan.status.active') }}</option>
                  <option value="archived">{{ t('tests.plan.status.archived') }}</option>
                </select>
              </td>
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
      </UiTable>
      <template #footer><UiPagination
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
      /></template>
    </UiTableContainer>
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
        <TestPlanCaseTree
          :suites="activeSuites"
          :cases="activeCases"
          :selected-case-ids="form.caseIds"
          :workspace-prefix="workspace?.prefix ?? ''"
          @toggle-suite="toggleSuite"
          @toggle-case="toggleCase"
          @move-case="move"
        />
        <!-- previous plan tree implementation
        <section
          v-for="suite in activeSuites"
          :key="suite.id"
          class="suite-case-group"
          :style="{ paddingLeft: `${suite.depth * 16}px` }"
        >
          <div class="suite-choice" :class="{ disabled: !casesForSuite(suite.id).length }">
            <button
              type="button"
              class="expand-suite-button"
              :aria-expanded="isSuiteExpanded(suite.id)"
              :aria-label="suite.name"
              @click="toggleSuiteExpanded(suite.id)"
            >
              <ChevronDown v-if="isSuiteExpanded(suite.id)" :size="15" />
              <ChevronRight v-else :size="15" />
            </button>
            <input
              type="checkbox"
              :checked="isSuiteSelected(suite.id)"
              :disabled="!casesForSuite(suite.id).length"
              @change="toggleSuite(suite.id)"
            />
            <strong>{{ suite.name }}</strong>
            <small>{{ t('tests.testCase.count', { count: casesForSuite(suite.id).length }) }}</small>
          </div>
          <div v-if="isSuiteExpanded(suite.id)" class="suite-cases">
            <label v-for="testCase in directCasesForSuite(suite.id)" :key="testCase.id" class="case-choice">
              <input
                type="checkbox"
                :checked="form.caseIds.includes(testCase.id)"
                @change="toggleCase(testCase.id)"
              />
              <span>{{ testCase.title }}</span>
              <span v-if="form.caseIds.includes(testCase.id)" class="order-actions">
                <button type="button" @click.prevent="move(testCase.id, -1)">&uarr;</button>
                <button type="button" @click.prevent="move(testCase.id, 1)">&darr;</button>
              </span>
            </label>
          </div>
        </section>
        -->
        <!-- legacy flat suite markup retained below temporarily for source-safe migration
        <section v-for="suite in activeSuites" :key="suite.id" class="suite-case-group">
          <label class="suite-choice" :class="{ disabled: !casesForSuite(suite.id).length }">
            <input
              type="checkbox"
              :checked="isSuiteSelected(suite.id)"
              :disabled="!casesForSuite(suite.id).length"
              @change="toggleSuite(suite.id)"
            />
            <strong>{{ suite.name }}</strong>
            <small>{{ t('tests.testCase.count', { count: casesForSuite(suite.id).length }) }}</small>
          </label>
          <label v-for="testCase in casesForSuite(suite.id)" :key="testCase.id" class="case-choice">
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
        </section>
        legacy flat case markup
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
        -->
      </fieldset>
    </form>
    <template #actions>
      <UiButton variant="secondary" @click="dialogOpen = false">{{ t('common.actions.cancel') }}</UiButton>
      <UiButton :disabled="saving" @click="save">{{ t('common.actions.save') }}</UiButton>
    </template>
  </UiActionDialog>
</template>

<style scoped>
.status-select{min-height:30px;padding:3px 26px 3px 10px;border:0;border-radius:999px;background:var(--kk-surface-subtle);color:var(--kk-text);font:inherit;font-size:.78rem;font-weight:700;cursor:pointer}.status-select.active{color:var(--kk-accent);background:var(--kk-accent-soft)}.status-select.archived{color:var(--kk-text-muted);background:#edf0ee}.status-select:disabled{cursor:wait;opacity:.7}.plan-form{display:grid;gap:14px}.plan-form label{display:grid;gap:6px;font-size:.85rem;font-weight:650}.plan-form small{color:var(--kk-text-muted);font-weight:400}.plan-form input,.plan-form textarea,.plan-form select{padding:9px;border:1px solid var(--kk-border);border-radius:6px;background:white}.plan-form textarea{min-height:70px;resize:vertical}.plan-form fieldset{display:grid;max-height:280px;gap:10px;padding:10px;overflow:auto;border:1px solid var(--kk-border);border-radius:6px}.suite-case-group{display:grid;gap:6px}.suite-choice,.case-choice{display:grid;grid-template-columns:auto minmax(0,1fr) auto!important;align-items:center;gap:8px}.suite-choice{padding:7px 8px;background:var(--kk-accent-soft);border-radius:5px}.suite-choice small{font-size:.75rem}.suite-choice.disabled{opacity:.55}.expand-suite-button{display:grid;place-items:center;width:20px;height:20px;padding:0;border:0;border-radius:4px;background:transparent;color:var(--kk-text-muted);cursor:pointer}.expand-suite-button:hover{background:white;color:var(--kk-accent)}.suite-cases{display:grid;gap:6px}.case-choice{padding-left:28px}.order-actions{display:flex;gap:4px}.order-actions button{border:1px solid var(--kk-border);background:white;border-radius:4px;cursor:pointer}
</style>
