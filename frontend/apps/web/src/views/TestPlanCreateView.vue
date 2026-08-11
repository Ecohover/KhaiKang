<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiCreateActions, UiFormActionBar } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { listWorkspaceIssues } from '../api/issueOptions'
import type { IssueResponse, TestCaseResponse, TestPlanResponse, TestPlanStatus, TestRunResponse, TestSuiteResponse, TestWorkspaceResponse } from '../api/contracts'
import TestPlanCaseTree from '../components/TestPlanCaseTree.vue'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const planId = computed(() => String(route.params.planId ?? ''))
const isEditing = computed(() => route.name === 'test-plan-edit')
const workspace = ref<TestWorkspaceResponse>()
const plan = ref<TestPlanResponse>()
const runs = ref<TestRunResponse[]>([])
const cases = ref<TestCaseResponse[]>([])
const suites = ref<TestSuiteResponse[]>([])
const name = ref('')
const description = ref('')
const status = ref<TestPlanStatus>('draft')
const caseIds = ref<string[]>([])
const testIssues = ref<IssueResponse[]>([])
const testIssueId = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')

const activeCases = computed(() => cases.value.filter((item) => item.status === 'active'))
const activeSuites = computed(() => suites.value.filter((item) => item.status === 'active'))
const validationHint = computed(() =>
  caseIds.value.length ? '' : t('tests.plan.caseRequired'),
)
const planRuns = computed(() => runs.value.filter((run) => run.planId === planId.value))
const actionMessage = computed(() => error.value || validationHint.value || (
  isEditing.value ? t('tests.plan.updateHint') : ''
))
const actionMessageTone = computed<'neutral' | 'warning' | 'danger'>(() => {
  if (error.value) return 'danger'
  if (validationHint.value) return 'warning'
  return 'neutral'
})

function toggleCase(caseId: string): void {
  caseIds.value = caseIds.value.includes(caseId)
    ? caseIds.value.filter((id) => id !== caseId)
    : [...caseIds.value, caseId]
}

function casesForSuite(suiteId: string): TestCaseResponse[] {
  const suiteIds = new Set([suiteId])
  let foundChild = true
  while (foundChild) {
    foundChild = false
    for (const suite of activeSuites.value) {
      if (suite.parentId && suiteIds.has(suite.parentId) && !suiteIds.has(suite.id)) {
        suiteIds.add(suite.id)
        foundChild = true
      }
    }
  }
  return activeCases.value.filter((testCase) => suiteIds.has(testCase.suiteId))
}

function toggleSuite(suiteId: string): void {
  const suiteCaseIds = casesForSuite(suiteId).map((testCase) => testCase.id)
  if (!suiteCaseIds.length) return
  const allSelected = suiteCaseIds.every((id) => caseIds.value.includes(id))
  caseIds.value = allSelected
    ? caseIds.value.filter((id) => !suiteCaseIds.includes(id))
    : [...caseIds.value, ...suiteCaseIds.filter((id) => !caseIds.value.includes(id))]
}

function moveCase(caseId: string, offset: number): void {
  const next = [...caseIds.value]
  const index = next.indexOf(caseId)
  const target = index + offset
  if (index < 0 || target < 0 || target >= next.length) return
  ;[next[index], next[target]] = [next[target]!, next[index]!]
  caseIds.value = next
}

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [workspaceResult, caseResult, suiteResult, planResult, runResult, issueOptionResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestCases(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
    isEditing.value
      ? apiClient.getTestPlan(workspaceId.value, planId.value)
      : Promise.resolve({ data: undefined, error: undefined }),
    isEditing.value ? apiClient.listTestRuns(workspaceId.value) : Promise.resolve({ data: [], error: undefined }),
    listWorkspaceIssues(workspaceId.value, { typeCode: 'task' }),
  ])
  workspace.value = workspaceResult.data
  cases.value = caseResult.data ?? []
  suites.value = suiteResult.data ?? []
  plan.value = planResult.data
  runs.value = runResult.data ?? []
  testIssues.value = issueOptionResult.issues
    .sort((left, right) => left.key.localeCompare(right.key))
  if (plan.value) {
    name.value = plan.value.name
    description.value = plan.value.description ?? ''
    status.value = plan.value.status
    caseIds.value = plan.value.items.map((item) => item.caseId)
    testIssueId.value = plan.value.testIssue?.id ?? ''
  }
  error.value = problemMessage(
    workspaceResult.error ?? caseResult.error ?? suiteResult.error ?? planResult.error ?? runResult.error ?? issueOptionResult.error,
    workspace.value && (!isEditing.value || plan.value) ? '' : t('tests.plan.loadFailed'),
  )
  loading.value = false
}

async function save(continueCreating = false): Promise<void> {
  if (!caseIds.value.length || saving.value) return
  saving.value = true
  error.value = ''
  const body = {
    name: name.value.trim(),
    description: description.value.trim() || null,
    caseIds: caseIds.value,
    testIssueId: testIssueId.value || null,
  }
  const result = isEditing.value && plan.value
    ? await apiClient.updateTestPlan(workspaceId.value, plan.value.id, {
      ...body,
      status: status.value,
      version: plan.value.version,
    }, await secureHeaders())
    : await apiClient.createTestPlan(workspaceId.value, body, await secureHeaders())

  if (result.data) {
    if (isEditing.value) {
      showUpdated(t('tests.plan.record'), result.data.name)
      await router.push({ name: 'test-plans', params: { workspaceId: workspaceId.value } })
    } else if (continueCreating) {
      showCreated(t('tests.plan.record'), result.data.name)
      name.value = ''
      description.value = ''
      caseIds.value = []
      testIssueId.value = ''
      await nextTick()
      document.getElementById('test-plan-name')?.focus()
    } else {
      showCreated(t('tests.plan.record'), result.data.name)
      await router.push({ name: 'test-plans', params: { workspaceId: workspaceId.value } })
    }
  } else {
    error.value = problemMessage(result.error, t('tests.plan.saveFailed'))
  }
  saving.value = false
}

onMounted(load)
</script>

<template>
  <section class="create-page">
    <button type="button" class="back-link" @click="router.push({ name: 'test-plans', params: { workspaceId } })">
      <ArrowLeft :size="16" />{{ t('tests.plan.backToList') }}
    </button>
    <header>
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ isEditing ? t('tests.plan.edit') : t('tests.plan.create') }}</h2>
      <span>{{ workspace?.prefix }} · {{ workspace?.name }}</span>
    </header>

    <p v-if="loading" class="state-panel">{{ t('tests.plan.loading') }}</p>
    <form v-else-if="workspace && (!isEditing || plan)" class="create-form" @submit.prevent="save()">
      <section class="form-section">
        <header><div><h3>{{ t('tests.plan.title') }}</h3><p>{{ t('tests.plan.description') }}</p></div></header>
        <label>
          <span>{{ t('tests.plan.name') }}</span>
          <input id="test-plan-name" v-model="name" maxlength="200" :placeholder="t('tests.plan.namePlaceholder')" :disabled="saving" />
          <small>{{ t('tests.plan.nameHint') }}</small>
        </label>
        <label>
          <span>{{ t('tests.plan.descriptionLabel') }}</span>
          <textarea v-model="description" rows="4" maxlength="4000" :disabled="saving" />
        </label>
        <label>
          <span>{{ t('tests.plan.testIssue') }}</span>
          <select v-model="testIssueId" :disabled="saving">
            <option value="">{{ t('tests.plan.noTestIssue') }}</option>
            <option v-for="testIssue in testIssues" :key="testIssue.id" :value="testIssue.id">
              {{ testIssue.key }} · {{ testIssue.title }}
            </option>
          </select>
          <small>{{ t('tests.plan.testIssueHint') }}</small>
        </label>
        <label v-if="isEditing">
          <span>{{ t('tests.plan.statusLabel') }}</span>
          <select v-model="status" :disabled="saving">
            <option value="draft">{{ t('tests.plan.status.draft') }}</option>
            <option value="active">{{ t('tests.plan.status.active') }}</option>
            <option value="archived">{{ t('tests.plan.status.archived') }}</option>
          </select>
        </label>
      </section>
      <section class="form-section">
        <header><div><h3>{{ t('tests.plan.cases') }}</h3><p>{{ t('tests.plan.scopeHint') }}</p></div></header>
        <TestPlanCaseTree
          :suites="activeSuites"
          :cases="activeCases"
          :selected-case-ids="caseIds"
          :workspace-prefix="workspace.prefix"
          @toggle-suite="toggleSuite"
          @toggle-case="toggleCase"
          @move-case="moveCase"
        />
      </section>
      <section v-if="isEditing" class="form-section plan-runs">
        <header><div><h3>{{ t('tests.plan.runsTitle') }}</h3><p>{{ t('tests.plan.runsDescription') }}</p></div></header>
        <p v-if="!planRuns.length" class="empty-runs">{{ t('tests.plan.runsEmpty') }}</p>
        <div v-else class="run-table-wrap">
          <table>
            <thead><tr><th>{{ t('tests.run.code') }}</th><th>{{ t('tests.run.name') }}</th><th>{{ t('tests.run.statusLabel') }}</th><th>{{ t('tests.run.updatedAt') }}</th></tr></thead>
            <tbody>
              <tr v-for="run in planRuns" :key="run.id" tabindex="0" @click="router.push({ name: 'test-run-detail', params: { workspaceId, runId: run.id } })" @keydown.enter="router.push({ name: 'test-run-detail', params: { workspaceId, runId: run.id } })">
                <td><code>{{ run.code }}</code></td><td>{{ run.name }}</td><td><span class="status-pill" :class="run.status">{{ t(`tests.run.status.${run.status}`) }}</span></td><td>{{ d(new Date(run.updatedAt), 'medium') }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
      <UiFormActionBar mode="floating" :message="actionMessage" :message-tone="actionMessageTone">
        <template v-if="isEditing">
          <UiButton variant="secondary" :disabled="saving" @click="router.push({ name: 'test-plans', params: { workspaceId } })">
            {{ t('common.actions.cancel') }}
          </UiButton>
          <UiButton type="submit" :loading="saving" :disabled="!caseIds.length">
            {{ t('tests.plan.update') }}
          </UiButton>
        </template>
        <UiCreateActions
          v-else
          :loading="saving"
          :disabled="!caseIds.length"
          :cancel-label="t('common.actions.cancel')"
          :create-label="t('tests.plan.create')"
          :continue-label="t('tests.testCase.createAndContinue')"
          @cancel="router.push({ name: 'test-plans', params: { workspaceId } })"
          @create="save(false)"
          @create-continue="save(true)"
        />
      </UiFormActionBar>
    </form>
    <div v-else class="state-panel state-panel--error" role="alert">{{ error }}</div>
  </section>
</template>

<style scoped>
.create-page{display:grid;max-width:960px;gap:22px;margin:0 auto;padding-bottom:40px}.back-link{display:flex;width:fit-content;align-items:center;gap:6px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}.create-page>header h2{margin:3px 0 7px;font-size:clamp(1.65rem,3vw,2.2rem)}.create-page>header span,.form-section p,.form-section small{color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.create-form{display:grid;gap:18px}.form-section{display:grid;gap:18px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.form-section>header{display:flex;justify-content:space-between;gap:16px}.form-section h3,.form-section p{margin:0}.form-section p{margin-top:4px;font-size:.84rem}.form-section label{display:grid;gap:7px;font-size:.875rem;font-weight:650}.form-section input,.form-section textarea,.form-section select{padding:11px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:var(--kk-radius);font:inherit}.form-section textarea{resize:vertical}.form-section small{font-weight:400}.error,.state-panel--error{color:var(--kk-danger)}.state-panel{margin:0;padding:42px 24px;text-align:center;background:var(--kk-surface);border:1px dashed var(--kk-border-strong);border-radius:var(--kk-radius)}.empty-runs{margin:0}.run-table-wrap{overflow-x:auto}.run-table-wrap table{width:100%;border-collapse:collapse}.run-table-wrap th,.run-table-wrap td{padding:10px 12px;border-bottom:1px solid var(--kk-border);text-align:left}.run-table-wrap tbody tr{cursor:pointer}.run-table-wrap tbody tr:hover,.run-table-wrap tbody tr:focus{background:var(--kk-surface-subtle);outline:0}.status-pill{display:inline-flex;padding:2px 8px;border-radius:999px;background:var(--kk-surface-subtle);font-size:.8rem;font-weight:650}.status-pill.in_progress{color:#975a16;background:#fef3c7}.status-pill.completed{color:#276749;background:#d9f99d}.status-pill.cancelled{color:#9b2c2c;background:#fed7d7}
</style>
