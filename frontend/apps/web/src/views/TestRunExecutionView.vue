<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { CheckCircle2, Play, XCircle } from '@lucide/vue'
import { UiActionDialog, UiButton } from '@khaikang/ui'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestResultStatus, TestRunItemResponse, TestRunResponse, TestRunStepResponse, TestWorkspaceResponse } from '../api/contracts'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { showUpdated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const runId = computed(() => String(route.params.runId))
const run = ref<TestRunResponse>()
const workspace = ref<TestWorkspaceResponse>()
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const finishStatus = ref<'completed' | 'cancelled'>('completed')
const summary = ref('')
const finishDialog = ref(false)
const terminal = computed(() => run.value?.status === 'completed' || run.value?.status === 'cancelled')
const resultOptions: TestResultStatus[] = ['not_run', 'passed', 'failed', 'blocked', 'skipped']

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, runResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.getTestRun(workspaceId.value, runId.value),
  ])
  workspace.value = workspaceResult.data
  run.value = runResult.data
  error.value = problemMessage(
    workspaceResult.error ?? runResult.error,
    t('tests.run.loadFailed'),
  )
  loading.value = false
}

async function saveStep(item: TestRunItemResponse, step: TestRunStepResponse): Promise<void> {
  if (!run.value || terminal.value) return
  saving.value = true
  const result = await apiClient.recordTestRunStep(
    workspaceId.value, run.value.id, item.id, step.id,
    { status: step.resultStatus, actualResult: step.actualResult, version: step.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    showUpdated(t('tests.run.step'), String(step.stepNo))
  } else error.value = problemMessage(result.error, t('tests.run.saveFailed'))
  saving.value = false
}

async function saveItem(item: TestRunItemResponse): Promise<void> {
  if (!run.value || terminal.value) return
  saving.value = true
  const result = await apiClient.recordTestRunItem(
    workspaceId.value, run.value.id, item.id,
    { status: item.resultStatus, actualResult: item.actualResult, version: item.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    showUpdated(t('tests.testCase.record'), item.caseTitle)
  } else error.value = problemMessage(result.error, t('tests.run.saveFailed'))
  saving.value = false
}

function openFinish(status: 'completed' | 'cancelled'): void {
  finishStatus.value = status
  summary.value = run.value?.summary ?? ''
  finishDialog.value = true
}

async function finish(): Promise<void> {
  if (!run.value) return
  saving.value = true
  const result = await apiClient.updateTestRunStatus(
    workspaceId.value,
    run.value.id,
    { status: finishStatus.value, summary: summary.value.trim() || null, version: run.value.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    finishDialog.value = false
    showUpdated(t('tests.run.record'), result.data.name)
  } else error.value = problemMessage(result.error, t('tests.run.finishFailed'))
  saving.value = false
}

onMounted(load)
</script>

<template>
  <SharedStateBanner v-if="loading" type="loading" :title="t('tests.run.loading')" />
  <SharedStateBanner
    v-else-if="!run"
    type="error"
    :title="t('tests.run.loadFailed')"
    :description="error"
    :reload-label="t('common.actions.reload')"
    @reload="load"
  />
  <TestWorkspaceSectionFrame
    v-else-if="workspace && run"
    :workspace="workspace"
    active-section="runs"
  >
    <section class="run-page">
    <ResourcePageHeader
      :meta="`${run.code} · ${t('tests.run.progress', {
        passed: run.progress.passed,
        total: run.progress.total,
        notRun: run.progress.notRun,
      })}`"
      :title="run.name"
      :subtitle="run.summary || t('tests.run.snapshotHint')"
      :status="run.status"
    >
      <UiButton v-if="!terminal" variant="secondary" @click="openFinish('cancelled')">
        <XCircle :size="17" />{{ t('tests.run.cancel') }}
      </UiButton>
      <UiButton v-if="!terminal" :disabled="run.progress.notRun > 0" @click="openFinish('completed')">
        <CheckCircle2 :size="17" />{{ t('tests.run.complete') }}
      </UiButton>
    </ResourcePageHeader>
    <p v-if="error" class="error">{{ error }}</p>
    <div class="progress-bar">
      <span :style="{ width: `${run.progress.total ? ((run.progress.total - run.progress.notRun) / run.progress.total) * 100 : 0}%` }" />
    </div>
    <article v-for="item in run.items" :key="item.id" class="run-case">
      <header>
        <div><small>#{{ item.sortOrder }}</small><h3>{{ item.caseTitle }}</h3></div>
        <select v-model="item.resultStatus" :disabled="terminal">
          <option v-for="status in resultOptions" :key="status" :value="status">{{ t(`tests.run.result.${status}`) }}</option>
        </select>
      </header>
      <p v-if="item.preconditions"><strong>{{ t('tests.testCase.preconditions') }}：</strong>{{ item.preconditions }}</p>
      <section v-for="step in item.steps" :key="step.id" class="run-step">
        <div class="step-copy">
          <strong>{{ t('tests.run.step') }} {{ step.stepNo }} · {{ step.action }}</strong>
          <span>{{ t('tests.testCase.expectedResult') }}：{{ step.expectedResult }}</span>
        </div>
        <select v-model="step.resultStatus" :disabled="terminal">
          <option v-for="status in resultOptions" :key="status" :value="status">{{ t(`tests.run.result.${status}`) }}</option>
        </select>
        <textarea v-model="step.actualResult" :disabled="terminal" :placeholder="t('tests.run.actualResult')" />
        <UiButton v-if="!terminal" variant="secondary" :disabled="saving" @click="saveStep(item, step)">
          {{ t('common.actions.save') }}
        </UiButton>
      </section>
      <div class="item-result">
        <textarea v-model="item.actualResult" :disabled="terminal" :placeholder="t('tests.run.caseActualResult')" />
        <UiButton v-if="!terminal" :disabled="saving" @click="saveItem(item)">{{ t('tests.run.saveCase') }}</UiButton>
      </div>
    </article>
    </section>
  </TestWorkspaceSectionFrame>

  <UiActionDialog
    :open="finishDialog"
    :title="finishStatus === 'completed' ? t('tests.run.complete') : t('tests.run.cancel')"
    :description="t('tests.run.terminalHint')"
    :close-label="t('common.actions.cancel')"
    @close="finishDialog = false"
  >
    <label class="summary">{{ t('tests.run.summary') }}<textarea v-model="summary" maxlength="4000" /></label>
    <template #actions>
      <UiButton variant="secondary" @click="finishDialog = false">{{ t('common.actions.cancel') }}</UiButton>
      <UiButton :disabled="saving" @click="finish">{{ t('common.actions.confirm') }}</UiButton>
    </template>
  </UiActionDialog>
</template>

<style scoped>
.run-page{display:grid;gap:20px}.error{padding:10px;color:#b42318;background:#fff1f0;border-radius:7px}.progress-bar{height:8px;overflow:hidden;background:var(--kk-surface-subtle);border-radius:999px}.progress-bar span{display:block;height:100%;background:var(--kk-accent)}.run-case{display:grid;gap:14px;padding:20px;background:white;border:1px solid var(--kk-border);border-radius:8px}.run-case>header{display:flex;justify-content:space-between;gap:16px}.run-case h3{margin:2px 0}.run-case small,.run-case p{color:var(--kk-text-muted)}select,textarea{padding:8px;border:1px solid var(--kk-border);border-radius:6px;background:white}.run-step{display:grid;grid-template-columns:minmax(0,1fr) 130px minmax(180px,.7fr) auto;align-items:center;gap:10px;padding:12px;background:var(--kk-surface-subtle);border-radius:7px}.step-copy{display:grid;gap:5px}.step-copy span{color:var(--kk-text-muted);font-size:.84rem}.item-result{display:grid;grid-template-columns:1fr auto;gap:10px}.summary{display:grid;gap:6px}.summary textarea{min-height:100px}@media(max-width:900px){.run-step{grid-template-columns:1fr}.item-result{grid-template-columns:1fr}}
</style>
