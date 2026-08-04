<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { ArrowDown, ArrowLeft, ArrowUp, GripVertical, Plus, Trash2 } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiCreateActions, UiField, UiFormActionBar } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestSuiteResponse, TestWorkspaceResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

interface EditableStep {
  key: number
  action: string
  expectedResult: string
}

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const workspaceId = computed(() => String(route.params.workspaceId))
const workspace = ref<TestWorkspaceResponse>()
const suites = ref<TestSuiteResponse[]>([])
const suiteId = ref(typeof route.query.suiteId === 'string' ? route.query.suiteId : '')
const title = ref('')
const description = ref('')
const preconditions = ref('')
const overallExpectedResult = ref('')
const steps = ref<EditableStep[]>([newStep()])
const loading = ref(true)
const creating = ref(false)
const error = ref('')
const nextStepKey = ref(2)
const { showCreated } = useSaveNotice()

const activeSuites = computed(() =>
  suites.value
    .filter((suite) => suite.status === 'active')
    .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name)),
)

const validationHint = computed(() => {
  if (!suiteId.value) return t('tests.testCase.suiteRequired')
  if (!title.value.trim()) return t('tests.testCase.titleRequired')
  if (!steps.value.length) return t('tests.testCase.stepsRequired')
  if (steps.value.some((step) => !step.action.trim() || !step.expectedResult.trim())) {
    return t('tests.testCase.stepFieldsRequired')
  }
  return ''
})

const isValid = computed(() => validationHint.value === '')

function newStep(): EditableStep {
  return { key: 1, action: '', expectedResult: '' }
}

function addStep(): void {
  steps.value.push({ key: nextStepKey.value++, action: '', expectedResult: '' })
}

function removeStep(index: number): void {
  if (steps.value.length > 1) steps.value.splice(index, 1)
}

function moveStep(index: number, direction: -1 | 1): void {
  const target = index + direction
  if (target < 0 || target >= steps.value.length) return
  const step = steps.value[index]
  if (!step) return
  steps.value.splice(index, 1)
  steps.value.splice(target, 0, step)
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, suiteResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  suites.value = suiteResult.data ?? []
  if (!activeSuites.value.some((suite) => suite.id === suiteId.value)) {
    suiteId.value = activeSuites.value[0]?.id ?? ''
  }
  error.value = problemMessage(
    workspaceResult.error ?? suiteResult.error,
    workspace.value ? '' : t('tests.workspace.loadFailed'),
  )
  loading.value = false
}

function resetForm(): void {
  title.value = ''
  description.value = ''
  preconditions.value = ''
  overallExpectedResult.value = ''
  nextStepKey.value = 2
  steps.value = [newStep()]
}

async function create(continueCreating: boolean): Promise<void> {
  if (!isValid.value || creating.value) return
  creating.value = true
  error.value = ''
  const result = await apiClient.createTestCase(workspaceId.value, {
    suiteId: suiteId.value,
    title: title.value.trim(),
    description: description.value.trim() || null,
    preconditions: preconditions.value.trim() || null,
    overallExpectedResult: overallExpectedResult.value.trim() || null,
    sortOrder: 0,
    steps: steps.value.map((step) => ({
      action: step.action.trim(),
      expectedResult: step.expectedResult.trim(),
    })),
  }, await secureHeaders())

  if (result.data) {
    showCreated(t('tests.testCase.record'), result.data.title)
    if (continueCreating) {
      resetForm()
      await nextTick()
      document.getElementById('test-case-title')?.focus()
    } else {
      await router.push({ name: 'test-suites', params: { workspaceId: workspaceId.value } })
    }
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.createFailed'))
  }
  creating.value = false
}

onMounted(load)
</script>

<template>
  <section class="create-page">
    <button
      class="back-link"
      type="button"
      @click="router.push({ name: 'test-suites', params: { workspaceId } })"
    >
      <ArrowLeft :size="16" />{{ t('tests.testCase.backToList') }}
    </button>

    <header>
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ t('tests.testCase.create') }}</h2>
      <span>{{ workspace?.prefix }} · {{ workspace?.name }}</span>
    </header>

    <p v-if="loading" class="state-panel">{{ t('tests.workspace.loading') }}</p>
    <form v-else-if="workspace" class="create-form" @submit.prevent="create(false)">
      <section class="form-section">
        <header>
          <div>
            <h3>{{ t('tests.testCase.basicInformation') }}</h3>
            <p>{{ t('tests.testCase.basicInformationHint') }}</p>
          </div>
        </header>
        <label>
          <span>{{ t('tests.testCase.suite') }}</span>
          <select v-model="suiteId" required :disabled="creating">
            <option value="" disabled>{{ t('tests.testCase.selectSuite') }}</option>
            <option v-for="suite in activeSuites" :key="suite.id" :value="suite.id">
              {{ '—'.repeat(suite.depth - 1) }} {{ suite.name }}
            </option>
          </select>
        </label>
        <UiField
          id="test-case-title"
          v-model="title"
          :label="t('tests.testCase.title')"
          :placeholder="t('tests.testCase.titlePlaceholder')"
          :disabled="creating"
        />
        <label>
          <span>{{ t('tests.testCase.description') }}</span>
          <textarea v-model="description" rows="3" maxlength="4000" :disabled="creating" />
        </label>
        <label>
          <span>{{ t('tests.testCase.preconditions') }}</span>
          <textarea v-model="preconditions" rows="3" maxlength="4000" :disabled="creating" />
        </label>
        <label>
          <span>{{ t('tests.testCase.overallExpectedResult') }}</span>
          <textarea
            v-model="overallExpectedResult"
            rows="3"
            maxlength="4000"
            :disabled="creating"
          />
        </label>
      </section>

      <section class="form-section">
        <header>
          <div>
            <h3>{{ t('tests.testCase.steps') }}</h3>
            <p>{{ t('tests.testCase.stepsHint') }}</p>
          </div>
          <UiButton type="button" variant="secondary" :disabled="creating" @click="addStep">
            <Plus :size="16" />{{ t('tests.testCase.addStep') }}
          </UiButton>
        </header>
        <article v-for="(step, index) in steps" :key="step.key" class="step-card">
          <header>
            <span><GripVertical :size="17" />{{ t('tests.testCase.stepNumber', { number: index + 1 }) }}</span>
            <div class="step-actions">
              <button type="button" :disabled="creating || index === 0" :aria-label="t('tests.testCase.moveStepUp')" @click="moveStep(index, -1)"><ArrowUp :size="16" /></button>
              <button type="button" :disabled="creating || index === steps.length - 1" :aria-label="t('tests.testCase.moveStepDown')" @click="moveStep(index, 1)"><ArrowDown :size="16" /></button>
              <button type="button" :disabled="creating || steps.length === 1" :aria-label="t('tests.testCase.removeStep')" @click="removeStep(index)"><Trash2 :size="16" />{{ t('tests.testCase.removeStep') }}</button>
            </div>
          </header>
          <label>
            <span>{{ t('tests.testCase.action') }}</span>
            <textarea v-model="step.action" rows="3" maxlength="4000" required :disabled="creating" />
          </label>
          <label>
            <span>{{ t('tests.testCase.expectedResult') }}</span>
            <textarea
              v-model="step.expectedResult"
              rows="3"
              maxlength="4000"
              required
              :disabled="creating"
            />
          </label>
        </article>
      </section>

      <UiFormActionBar mode="floating">
        <template #status>
          <p v-if="error" class="error" role="alert">{{ error }}</p>
          <p v-else-if="!isValid" class="validation-hint">{{ validationHint }}</p>
        </template>
          <UiCreateActions
            :loading="creating"
            :disabled="!isValid"
            :cancel-label="t('common.actions.cancel')"
            :create-label="t('tests.testCase.create')"
            :continue-label="t('tests.testCase.createAndContinue')"
            @cancel="router.push({ name: 'test-suites', params: { workspaceId } })"
            @create="create(false)"
            @create-continue="create(true)"
          />
      </UiFormActionBar>
    </form>
    <div v-else class="state-panel state-panel--error" role="alert">{{ error }}</div>
  </section>
</template>

<style scoped>
.create-page {
  display: grid;
  max-width: 960px;
  gap: 22px;
  margin: 0 auto;
  padding-bottom: 40px;
}

.back-link {
  display: flex;
  width: fit-content;
  align-items: center;
  gap: 6px;
  padding: 0;
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
  cursor: pointer;
}

.create-page > header h2 {
  margin: 3px 0 7px;
  font-size: clamp(1.65rem, 3vw, 2.2rem);
}

.create-page > header span,
.form-section p {
  color: var(--kk-text-muted);
}

.eyebrow {
  margin: 0;
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 750;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.create-form {
  display: grid;
  gap: 18px;
}

.form-section {
  display: grid;
  gap: 18px;
  padding: 24px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  box-shadow: var(--kk-shadow);
}

.form-section > header,
.step-card > header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 16px;
}

.form-section h3,
.form-section p {
  margin: 0;
}
.form-section p {
  margin-top: 4px;
  font-size: 0.84rem;
}

.form-section label {
  display: grid;
  gap: 7px;
  font-size: 0.875rem;
  font-weight: 650;
}

.form-section select,
.form-section textarea {
  padding: 11px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
}

.step-card {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  padding: 16px;
  background: var(--kk-surface-subtle);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}
.step-card > header {
  grid-column: 1 / -1;
}
.step-card > header span {
  display: flex;
  align-items: center;
  gap: 7px;
  font-weight: 750;
}
.step-card > header button {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 5px 8px;
  color: var(--kk-danger);
  background: transparent;
  border: 0;
  cursor: pointer;
}
.step-card > header button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.step-actions{display:flex;align-items:center;gap:4px}.step-actions button:not(:last-child){display:grid;place-items:center;padding:5px;border:1px solid var(--kk-border);border-radius:4px;background:var(--kk-surface);color:var(--kk-text-muted)}

/* STICKY BOTTOM ACTION BAR */
.validation-hint {
  margin: 0;
  font-size: 0.84rem;
  color: #c05621;
  font-weight: 600;
}

.actions-group {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
}

.error,
.state-panel--error {
  margin: 0;
  color: var(--kk-danger);
  font-weight: 600;
  font-size: 0.85rem;
}

.state-panel {
  margin: 0;
  padding: 42px 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

@media (max-width: 720px) {
  .step-card {
    grid-template-columns: 1fr;
  }
  .form-section > header {
    align-items: stretch;
    flex-direction: column;
  }
  .actions-group {
    justify-content: flex-end;
  }
}
</style>
