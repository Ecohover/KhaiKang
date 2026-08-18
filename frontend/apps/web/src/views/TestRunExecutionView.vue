<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { CheckCircle2, Paperclip, Play, Trash2, Upload, XCircle } from '@lucide/vue'
import { UiActionDialog, UiAttachmentLink, UiButton, UiFormActionBar, UiStatusBadge } from '@khaikang/ui'
import AppMarkdown from '../components/AppMarkdown.vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestResultStatus, TestRunItemAttachmentResponse, TestRunItemResponse, TestRunResponse, TestRunStepResponse, TestWorkspaceResponse } from '../api/contracts'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import TestWorkspaceSectionFrame from '../components/TestWorkspaceSectionFrame.vue'
import TestRunTracePanel from '../components/TestRunTracePanel.vue'
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
const activeItemId = ref('')
const itemAttachments = ref<Record<string, TestRunItemAttachmentResponse[]>>({})
const uploadingItemId = ref('')
const isExecuting = computed(() => run.value?.status === 'in_progress')
const canModifyEvidence = computed(() =>
  isExecuting.value &&
  workspace.value?.status === 'active' &&
  ['owner', 'manager', 'tester'].includes(workspace.value.currentUserRole),
)
const canCreateBugs = computed(() =>
  workspace.value?.status === 'active' &&
  ['owner', 'manager'].includes(workspace.value.currentUserRole),
)
const attachmentDialogLabels = computed(() => ({
  attachmentDialog: t('common.markdown.attachmentDialog'),
  downloadAttachment: t('common.markdown.downloadAttachment'),
  close: t('common.actions.close'),
}))
const resultOptions: TestResultStatus[] = ['not_run', 'passed', 'failed', 'blocked', 'skipped']
type ResultTone = TestResultStatus

const canComplete = computed(() => Boolean(run.value) && run.value!.items.every((item) =>
  item.steps.length
    ? item.steps.every((step) => step.resultStatus !== 'not_run')
    : item.resultStatus !== 'not_run',
))

function itemDisplayStatus(item: TestRunItemResponse): ResultTone {
  if (!item.steps.length) return item.resultStatus
  const statuses = item.steps.map((step) => step.resultStatus)
  if (statuses.some((status) => status === 'failed' || status === 'blocked')) return 'failed'
  if (statuses.every((status) => status === 'passed')) return 'passed'
  if (statuses.every((status) => status === 'skipped')) return 'skipped'
  return 'not_run'
}

function currentItem(itemId: string): TestRunItemResponse | undefined {
  return run.value?.items.find((item) => item.id === itemId)
}

async function loadItemAttachments(currentRun: TestRunResponse): Promise<string> {
  const results = await Promise.all(currentRun.items.map(async (item) => ({
    itemId: item.id,
    result: await apiClient.listTestRunItemAttachments(workspaceId.value, currentRun.id, item.id),
  })))
  const nextAttachments: Record<string, TestRunItemAttachmentResponse[]> = {}
  let firstError = ''
  for (const { itemId, result } of results) {
    if (result.data) {
      nextAttachments[itemId] = result.data
    } else if (!firstError) {
      firstError = problemMessage(result.error, t('tests.run.attachments.loadFailed'))
    }
  }
  itemAttachments.value = nextAttachments
  return firstError
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, runResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.getTestRun(workspaceId.value, runId.value),
  ])
  workspace.value = workspaceResult.data
  run.value = runResult.data
  if (run.value && !activeItemId.value) {
    activeItemId.value = run.value.items.find((item) => item.resultStatus === 'not_run')?.id ?? run.value.items[0]?.id ?? ''
  }
  const loadError = workspaceResult.error ?? runResult.error
  const attachmentError = run.value ? await loadItemAttachments(run.value) : ''
  error.value = loadError ? problemMessage(loadError, t('tests.run.loadFailed')) : attachmentError
  loading.value = false
}

async function uploadRunItemAttachment(
  item: TestRunItemResponse,
  file: File,
): Promise<TestRunItemAttachmentResponse> {
  if (!run.value) throw new Error(t('tests.run.attachments.uploadFailed'))
  const result = await apiClient.uploadTestRunItemAttachment(
    workspaceId.value,
    run.value.id,
    item.id,
    file,
    await secureHeaders(),
  )
  if (!result.data) {
    const message = problemMessage(result.error, t('tests.run.attachments.uploadFailed'))
    error.value = message
    throw new Error(message)
  }
  itemAttachments.value = {
    ...itemAttachments.value,
    [item.id]: [
      result.data,
      ...(itemAttachments.value[item.id] ?? []).filter(attachment => attachment.id !== result.data!.id),
    ],
  }
  return result.data
}

async function uploadRunItemImage(
  item: TestRunItemResponse,
  file: File,
): Promise<{ src: string, alt?: string }> {
  if (!file.type.startsWith('image/')) throw new Error(t('tests.run.attachments.imageRequired'))
  const attachment = await uploadRunItemAttachment(item, file)
  return {
    src: apiClient.testRunItemAttachmentContentUrl(
      workspaceId.value,
      runId.value,
      item.id,
      attachment.id,
      true,
    ),
    alt: attachment.originalFileName,
  }
}

async function uploadRunItemFile(
  item: TestRunItemResponse,
  file: File,
): Promise<{ src: string, name: string }> {
  const attachment = await uploadRunItemAttachment(item, file)
  return {
    src: apiClient.testRunItemAttachmentContentUrl(
      workspaceId.value,
      runId.value,
      item.id,
      attachment.id,
    ),
    name: attachment.originalFileName,
  }
}

async function selectRunItemAttachment(event: Event, item: TestRunItemResponse): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || uploadingItemId.value) return
  uploadingItemId.value = item.id
  error.value = ''
  try {
    await uploadRunItemAttachment(item, file)
  } catch (uploadError) {
    error.value = uploadError instanceof Error
      ? uploadError.message
      : t('tests.run.attachments.uploadFailed')
  } finally {
    uploadingItemId.value = ''
    input.value = ''
  }
}

async function deleteRunItemAttachment(
  item: TestRunItemResponse,
  attachment: TestRunItemAttachmentResponse,
): Promise<void> {
  if (!run.value || !canModifyEvidence.value || !window.confirm(t('tests.run.attachments.deleteConfirm'))) return
  error.value = ''
  const result = await apiClient.deleteTestRunItemAttachment(
    workspaceId.value,
    run.value.id,
    item.id,
    attachment.id,
    await secureHeaders(),
  )
  if (result.error) {
    error.value = problemMessage(result.error, t('tests.run.attachments.deleteFailed'))
    return
  }
  itemAttachments.value = {
    ...itemAttachments.value,
    [item.id]: (itemAttachments.value[item.id] ?? []).filter(itemAttachment => itemAttachment.id !== attachment.id),
  }
}

function attachmentSize(fileSize: number): string {
  return t('tests.run.attachments.size', {
    size: Math.max(0.1, fileSize / 1024).toFixed(1),
  })
}

async function saveStep(
  item: TestRunItemResponse,
  step: TestRunStepResponse,
  synchronizeCase = true,
): Promise<void> {
  if (!run.value || !isExecuting.value) return
  saving.value = true
  const result = await apiClient.recordTestRunStep(
    workspaceId.value, run.value.id, item.id, step.id,
    { status: step.resultStatus, actualResult: step.actualResult, version: step.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    if (synchronizeCase) await synchronizeCaseStatus(item.id)
    showUpdated(t('tests.run.step'), String(step.stepNo))
  } else error.value = problemMessage(result.error, t('tests.run.saveFailed'))
  saving.value = false
}

async function saveItem(item: TestRunItemResponse, showNotice = true): Promise<void> {
  if (!run.value || !isExecuting.value) return
  saving.value = true
  const result = await apiClient.recordTestRunItem(
    workspaceId.value, run.value.id, item.id,
    { status: item.resultStatus, actualResult: item.actualResult, version: item.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    if (showNotice) showUpdated(t('tests.testCase.record'), item.caseTitle)
  } else error.value = problemMessage(result.error, t('tests.run.saveFailed'))
  saving.value = false
}

function openFinish(status: 'completed' | 'cancelled'): void {
  finishStatus.value = status
  summary.value = run.value?.summary ?? ''
  finishDialog.value = true
}

async function startRun(): Promise<void> {
  if (!run.value || !['not_started', 'cancelled'].includes(run.value.status)) return
  saving.value = true
  const result = await apiClient.updateTestRunStatus(
    workspaceId.value,
    run.value.id,
    { status: 'in_progress', summary: null, version: run.value.version },
    await secureHeaders(),
  )
  if (result.data) {
    run.value = result.data
    activeItemId.value = result.data.items.find((item) => item.resultStatus === 'not_run')?.id ?? result.data.items[0]?.id ?? ''
    showUpdated(t('tests.run.status.in_progress'), result.data.name)
  } else error.value = problemMessage(result.error, t('tests.run.saveFailed'))
  saving.value = false
}

async function synchronizeCaseStatus(itemId: string): Promise<void> {
  const item = currentItem(itemId)
  if (!item) return
  const derivedStatus = itemDisplayStatus(item)
  if (item.resultStatus === derivedStatus) return
  item.resultStatus = derivedStatus
  await saveItem(item, false)
}

async function applyCaseStatus(item: TestRunItemResponse): Promise<void> {
  if (!isExecuting.value) return
  const selectedStatus = item.resultStatus
  if (!item.steps.length) {
    await saveItem(item)
    return
  }

  for (const step of item.steps) {
    if (step.resultStatus === selectedStatus) continue
    step.resultStatus = selectedStatus
    await saveStep(item, step, false)
  }

  const refreshedItem = currentItem(item.id)
  if (!refreshedItem) return
  refreshedItem.resultStatus = selectedStatus
  await saveItem(refreshedItem)
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
    />
    <p v-if="error" class="error">{{ error }}</p>
    <div class="progress-bar">
      <span :style="{ width: `${run.progress.total ? ((run.progress.total - run.progress.notRun) / run.progress.total) * 100 : 0}%` }" />
    </div>
    <TestRunTracePanel
      :workspace-id="workspaceId"
      :run-id="run.id"
      :test-issue="run.testIssue"
      :can-create-bug="canCreateBugs"
    />
    <article
      v-for="item in run.items"
      :key="item.id"
      class="run-case"
      :class="[`result-${itemDisplayStatus(item)}`, { 'is-active': activeItemId === item.id }]"
      @click="activeItemId = item.id"
    >
      <header class="case-header">
        <div><small>#{{ item.sortOrder }}</small><h3>{{ item.caseTitle }}</h3></div>
        <div class="result-control">
          <span class="result-badge" :class="itemDisplayStatus(item)">{{ t(`tests.run.result.${itemDisplayStatus(item)}`) }}</span>
          <select
            v-model="item.resultStatus"
            :class="itemDisplayStatus(item)"
            :disabled="!isExecuting || saving"
            @change="applyCaseStatus(item)"
          >
            <option v-for="status in resultOptions" :key="status" :value="status">{{ t(`tests.run.result.${status}`) }}</option>
          </select>
        </div>
      </header>
      <div v-if="item.preconditions" class="case-preconditions">
        <span>{{ t('tests.testCase.preconditions') }}</span>
        <AppMarkdown
          :model-value="item.preconditions"
          mode="display"
          size="compact"
        />
      </div>
      <section v-for="step in item.steps" :key="step.id" class="run-step" :class="`result-${step.resultStatus}`">
        <header class="step-heading">
          <strong>{{ t('tests.run.step') }} {{ step.stepNo }}</strong>
          <select
            v-model="step.resultStatus"
            :class="step.resultStatus"
            :disabled="!isExecuting || saving"
            @change="saveStep(item, step)"
          >
            <option v-for="status in resultOptions" :key="status" :value="status">{{ t(`tests.run.result.${status}`) }}</option>
          </select>
        </header>
        <div class="step-action">
          <span>{{ t('tests.testCase.action') }}</span>
          <AppMarkdown
            :model-value="step.action"
            mode="display"
            size="compact"
          />
        </div>
        <div class="step-results">
          <div class="step-expected-result">
            <span>{{ t('tests.testCase.expectedResult') }}</span>
            <AppMarkdown
              :model-value="step.expectedResult"
              mode="display"
              size="compact"
            />
          </div>
          <div class="step-actual-result">
            <span>{{ t('tests.run.actualResult') }}</span>
            <AppMarkdown
              :model-value="step.actualResult ?? ''"
              size="compact"
              :disabled="!isExecuting || saving"
              :placeholder="t('tests.run.actualResult')"
              :upload-image="canModifyEvidence ? (file) => uploadRunItemImage(item, file) : undefined"
              :upload-attachment="canModifyEvidence ? (file) => uploadRunItemFile(item, file) : undefined"
              @update:model-value="step.actualResult = $event"
              @blur="saveStep(item, step)"
            />
          </div>
        </div>
      </section>
      <div class="item-result">
        <span>{{ t('tests.run.caseActualResult') }}</span>
        <AppMarkdown
          :model-value="item.actualResult ?? ''"
          :disabled="!isExecuting || saving"
          :placeholder="t('tests.run.caseActualResult')"
          :upload-image="canModifyEvidence ? (file) => uploadRunItemImage(item, file) : undefined"
          :upload-attachment="canModifyEvidence ? (file) => uploadRunItemFile(item, file) : undefined"
          @update:model-value="item.actualResult = $event"
          @blur="saveItem(item)"
        />
      </div>
      <section class="run-evidence" @click.stop>
        <header>
          <div class="evidence-heading">
            <Paperclip :size="18" aria-hidden="true" />
            <div>
              <strong>{{ t('tests.run.attachments.title') }}</strong>
              <span>{{ t('tests.run.attachments.description') }}</span>
            </div>
          </div>
          <label v-if="canModifyEvidence" class="evidence-upload">
            <Upload :size="16" aria-hidden="true" />
            <span>{{ t(uploadingItemId === item.id ? 'tests.run.attachments.uploading' : 'tests.run.attachments.upload') }}</span>
            <input type="file" :disabled="Boolean(uploadingItemId)" @change="selectRunItemAttachment($event, item)" />
          </label>
        </header>
        <p v-if="(itemAttachments[item.id] ?? []).length === 0" class="evidence-empty">
          {{ t('tests.run.attachments.empty') }}
        </p>
        <ul v-else class="evidence-list">
          <li v-for="attachment in itemAttachments[item.id]" :key="attachment.id">
            <div>
              <UiAttachmentLink
                :href="apiClient.testRunItemAttachmentContentUrl(workspaceId, run.id, item.id, attachment.id)"
                :file-name="attachment.originalFileName"
                :labels="attachmentDialogLabels"
              />
              <small>{{ attachmentSize(attachment.fileSize) }} · {{ attachment.uploadedByUsername }}</small>
            </div>
            <UiButton
              v-if="canModifyEvidence"
              type="button"
              variant="secondary"
              :aria-label="t('tests.run.attachments.delete')"
              @click="deleteRunItemAttachment(item, attachment)"
            >
              <Trash2 :size="15" aria-hidden="true" />
            </UiButton>
          </li>
        </ul>
      </section>
    </article>
    <UiFormActionBar mode="floating">
      <template #status>
        <UiStatusBadge
          :variant="run.status === 'completed' ? 'success' : run.status === 'in_progress' ? 'warning' : run.status === 'cancelled' ? 'inactive' : 'neutral'"
        >
          {{ t(`tests.run.status.${run.status}`) }}
        </UiStatusBadge>
      </template>
      <UiButton v-if="run.status === 'not_started' || run.status === 'cancelled'" :disabled="saving" @click="startRun">
        <Play :size="17" />{{ t(run.status === 'cancelled' ? 'tests.run.restart' : 'tests.run.start') }}
      </UiButton>
      <UiButton v-if="isExecuting" variant="secondary" :disabled="saving" @click="openFinish('cancelled')">
        <XCircle :size="17" />{{ t('tests.run.cancel') }}
      </UiButton>
      <UiButton v-if="isExecuting" :disabled="saving || !canComplete" @click="openFinish('completed')">
        <CheckCircle2 :size="17" />{{ t('tests.run.complete') }}
      </UiButton>
    </UiFormActionBar>
    </section>
  </TestWorkspaceSectionFrame>

  <UiActionDialog
    :open="finishDialog"
    :title="finishStatus === 'completed' ? t('tests.run.complete') : t('tests.run.cancel')"
    :description="t('tests.run.terminalHint')"
    :close-label="t('common.actions.cancel')"
    @close="finishDialog = false"
  >
    <div class="summary">{{ t('tests.run.summary') }}<AppMarkdown v-model="summary" /></div>
    <template #actions>
      <UiButton variant="secondary" @click="finishDialog = false">{{ t('common.actions.cancel') }}</UiButton>
      <UiButton :disabled="saving" @click="finish">{{ t('common.actions.confirm') }}</UiButton>
    </template>
  </UiActionDialog>
</template>

<style scoped>
.run-page{display:grid;gap:16px}.error{padding:10px;color:#b42318;background:#fff1f0;border-radius:7px}.progress-bar{height:8px;overflow:hidden;background:var(--kk-surface-subtle);border-radius:999px}.progress-bar span{display:block;height:100%;background:var(--kk-accent)}.run-case{display:grid;gap:14px;padding:18px;background:white;border:1px solid var(--kk-border);border-radius:8px;cursor:pointer;transition:border-color 140ms ease,box-shadow 140ms ease,background 140ms ease}.run-case.is-active{background:#fbfefc;border-color:color-mix(in srgb,var(--kk-accent) 55%,var(--kk-border));box-shadow:0 0 0 3px color-mix(in srgb,var(--kk-accent) 12%,transparent)}.case-header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.run-case h3{margin:2px 0}.run-case small,.run-case p{color:var(--kk-text-muted)}.result-control{display:flex;align-items:center;gap:8px}.result-badge{display:inline-flex;min-height:28px;align-items:center;padding:3px 9px;border-radius:999px;font-size:.78rem;font-weight:700}.result-badge.not_run,select.not_run{color:#596560;background:#f1f3f2}.result-badge.passed,select.passed{color:#18794e;background:#e7f5ec}.result-badge.failed,.result-badge.blocked,select.failed,select.blocked{color:#b42318;background:#fff0ee}.result-badge.partial,select.partial{color:#9a6700;background:#fff8df}.result-badge.skipped,select.skipped{color:#626b73;background:#f2f4f7}select,textarea{padding:7px 9px;border:1px solid var(--kk-border);border-radius:6px;background:white}select{min-height:32px;font-weight:650}.run-step{display:grid;gap:10px;padding:12px 14px;background:var(--kk-surface-subtle);border-radius:7px}.step-heading{display:flex;align-items:center;justify-content:space-between;gap:16px}.step-heading>strong{line-height:1.45}.step-heading>select{width:132px;height:32px;flex:0 0 132px}.step-action,.step-expected-result,.step-actual-result{display:grid;min-width:0;gap:5px}.step-action>span,.step-expected-result>span,.step-actual-result>span{color:var(--kk-text-muted);font-size:.84rem}.step-results{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));align-items:start;gap:12px}.run-step textarea{min-height:46px;resize:vertical}.item-result{display:grid;gap:6px;color:var(--kk-text-muted);font-size:.84rem;font-weight:650}.item-result textarea{min-height:76px;resize:vertical;color:var(--kk-text);font-weight:400}.summary{display:grid;gap:6px}.summary textarea{min-height:100px}@media(max-width:900px){.case-header{align-items:stretch;flex-direction:column}.result-control{justify-content:space-between}.step-results{grid-template-columns:1fr}}@media(max-width:640px){.step-heading{align-items:stretch;flex-direction:column}.step-heading>select{width:100%;flex-basis:auto}}
.run-case.result-passed{background:#f4fbf6;border-color:#b9dfc5}.run-case.result-failed,.run-case.result-blocked{background:#fff7f6;border-color:#f1c2bd}.run-case.result-partial{background:#fffdf4;border-color:#f1d892}.run-case.result-not_run,.run-case.result-skipped{background:white}.run-step.result-passed{background:#eff9f2}.run-step.result-failed,.run-step.result-blocked{background:#fff0ee}.run-step.result-partial{background:#fff8df}.run-step.result-not_run,.run-step.result-skipped{background:var(--kk-surface-subtle)}
.run-evidence{display:grid;gap:10px;padding-top:14px;border-top:1px solid var(--kk-border)}.run-evidence>header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.evidence-heading{display:flex;align-items:flex-start;gap:9px}.evidence-heading>div{display:grid;gap:2px}.evidence-heading span,.evidence-empty{color:var(--kk-text-muted);font-size:.84rem;font-weight:400}.evidence-upload{display:inline-flex;min-height:32px;align-items:center;gap:6px;padding:5px 10px;border:1px solid var(--kk-border);border-radius:6px;background:white;cursor:pointer;font-size:.84rem;font-weight:650}.evidence-upload input{display:none}.evidence-list{display:grid;gap:7px;margin:0;padding:0;list-style:none}.evidence-list li{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:9px 11px;background:var(--kk-surface-subtle);border-radius:6px}.evidence-list li>div{display:grid;gap:2px;min-width:0}.evidence-list small{color:var(--kk-text-muted)}
.case-preconditions{display:grid;min-width:0;gap:5px}.case-preconditions>span{color:var(--kk-text-muted);font-size:.84rem}
.case-preconditions>span,.step-action>span,.step-expected-result>span,.step-actual-result>span{color:var(--kk-text);font-size:.9rem;font-weight:700;line-height:1.45}
@media(max-width:640px){.run-evidence>header{align-items:stretch;flex-direction:column}.evidence-upload{justify-content:center}}
</style>
