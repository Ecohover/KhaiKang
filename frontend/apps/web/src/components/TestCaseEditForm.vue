<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ArrowDown, ArrowLeft, ArrowUp, GripVertical, Plus, Trash2 } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { UiButton, UiCreateActions, UiField, UiFormActionBar } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestCaseResponse, TestSuiteResponse } from '../api/contracts'
import type { TestWorkspaceResponse } from '../api/contracts'

interface EditableStep {
  key: number
  action: string
  expectedResult: string
}

const props = defineProps<{
  workspaceId: string
  workspace: TestWorkspaceResponse | undefined
  embedded?: boolean
  testCase: TestCaseResponse
  suites: TestSuiteResponse[]
}>()

const emit = defineEmits<{
  (e: 'saved', updatedCase: TestCaseResponse): void
  (e: 'cancel'): void
  (e: 'dirty-change', value: boolean): void
}>()

const { t } = useI18n()

const suiteId = ref(props.testCase.suiteId)
const title = ref(props.testCase.title)
const description = ref(props.testCase.description ?? '')
const preconditions = ref(props.testCase.preconditions ?? '')
const overallExpectedResult = ref(props.testCase.overallExpectedResult ?? '')
const status = ref<'active' | 'inactive'>(props.testCase.status)
const sortOrder = ref(props.testCase.sortOrder)
const tagIds = ref<string[]>([])
const steps = ref<EditableStep[]>([])
const saving = ref(false)
const error = ref('')
const nextStepKey = ref(1)
const initialState = ref('')

const formState = computed(() => JSON.stringify({
  suiteId: suiteId.value, title: title.value, description: description.value,
  preconditions: preconditions.value, overallExpectedResult: overallExpectedResult.value,
  status: status.value, sortOrder: sortOrder.value,
  tagIds: tagIds.value,
  steps: steps.value.map(({ action, expectedResult }) => ({ action, expectedResult })),
}))
const isDirty = computed(() => initialState.value !== '' && formState.value !== initialState.value)

watch(isDirty, (value) => emit('dirty-change', value))

watch(
  () => props.testCase,
  (newCase) => {
    if (!newCase) return
    suiteId.value = newCase.suiteId
    title.value = newCase.title
    description.value = newCase.description ?? ''
    preconditions.value = newCase.preconditions ?? ''
    overallExpectedResult.value = newCase.overallExpectedResult ?? ''
    status.value = newCase.status
    sortOrder.value = newCase.sortOrder
    tagIds.value = newCase.tags.map((tag) => tag.id)
    steps.value = newCase.steps.map((step, idx) => ({
      key: idx + 1,
      action: step.action,
      expectedResult: step.expectedResult,
    }))
    nextStepKey.value = newCase.steps.length + 1
    initialState.value = ''
    queueMicrotask(() => { initialState.value = formState.value })
  },
  { immediate: true },
)

const activeSuites = computed(() =>
  props.suites
    .filter((suite) => suite.status === 'active' || suite.id === suiteId.value)
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

async function save(): Promise<void> {
  if (!isValid.value || saving.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.updateTestCase(
    props.workspaceId,
    props.testCase.id,
    {
      suiteId: suiteId.value,
      title: title.value.trim(),
      description: description.value.trim() || null,
      preconditions: preconditions.value.trim() || null,
      overallExpectedResult: overallExpectedResult.value.trim() || null,
      sortOrder: sortOrder.value,
      status: status.value,
      tagIds: tagIds.value,
      version: props.testCase.version,
      steps: steps.value.map((step) => ({
        action: step.action.trim(),
        expectedResult: step.expectedResult.trim(),
      })),
    },
    await secureHeaders(),
  )

  if (result.data) {
    initialState.value = formState.value
    emit('saved', result.data)
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.updateFailed'))
  }
  saving.value = false
}
</script>

<template>
  <div class="edit-page" :class="{ 'edit-page--embedded': props.embedded }">
    <button v-if="!props.embedded" type="button" class="back-link" @click="emit('cancel')">
      <ArrowLeft :size="16" />{{ t('tests.testCase.backToList') }}
    </button>
    <header v-if="!props.embedded" class="page-header">
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ t('tests.testCase.edit') }}</h2>
      <span>{{ props.workspace?.prefix }} · {{ props.workspace?.name }}</span>
    </header>

    <div v-if="error" class="error-alert">{{ error }}</div>

    <form class="edit-form" @submit.prevent="save">
      <section class="form-section">
        <header>
          <div>
            <h3>{{ t('tests.testCase.basicInformation') }}</h3>
            <p>{{ t('tests.testCase.basicInformationHint') }}</p>
          </div>
        </header>

        <label>
          <span>{{ t('tests.testCase.suite') }}</span>
          <select v-model="suiteId" required :disabled="saving">
            <option value="" disabled>{{ t('tests.testCase.selectSuite') }}</option>
            <option v-for="suite in activeSuites" :key="suite.id" :value="suite.id">
              {{ '—'.repeat(suite.depth - 1) }} {{ suite.name }}
            </option>
          </select>
        </label>

        <UiField
          id="case-title"
          v-model="title"
          :label="t('tests.testCase.title') + ' *'"
          :placeholder="t('tests.testCase.titlePlaceholder')"
          required
          :disabled="saving"
        />

        <label>
          <span>{{ t('tests.testCase.description') }}</span>
          <textarea v-model="description" rows="3" maxlength="4000" :disabled="saving" />
        </label>

        <label>
          <span>{{ t('tests.testCase.preconditions') }}</span>
          <textarea v-model="preconditions" rows="3" maxlength="4000" :disabled="saving" />
        </label>

        <label>
          <span>{{ t('tests.testCase.overallExpectedResult') }}</span>
          <textarea v-model="overallExpectedResult" rows="3" maxlength="4000" :disabled="saving" />
        </label>

        <label>
          <span>{{ t('common.fields.status') }}</span>
          <select v-model="status" :disabled="saving">
            <option value="active">{{ t('common.status.active') }}</option>
            <option value="inactive">{{ t('common.status.inactive') }}</option>
          </select>
        </label>
      </section>

      <section class="form-section">
        <header>
          <div>
            <h3>{{ t('tests.testCase.steps') }}</h3>
            <p>{{ t('tests.testCase.stepsHint') }}</p>
          </div>
          <UiButton type="button" variant="secondary" :disabled="saving" @click="addStep">
            <Plus :size="16" />{{ t('tests.testCase.addStep') }}
          </UiButton>
        </header>

        <div class="step-editor-list">
          <article v-for="(stepItem, index) in steps" :key="stepItem.key" class="step-editor-card">
            <header>
              <span><GripVertical :size="17" /> {{ t('tests.testCase.stepNumber', { number: index + 1 }) }}</span>
              <button
                type="button"
                class="btn-icon"
                :aria-label="t('tests.testCase.moveStepUp')"
                :disabled="index === 0"
                @click="moveStep(index, -1)"
              ><ArrowUp :size="16" /></button>
              <button
                type="button"
                class="btn-icon"
                :aria-label="t('tests.testCase.moveStepDown')"
                :disabled="index === steps.length - 1"
                @click="moveStep(index, 1)"
              ><ArrowDown :size="16" /></button>
              <button
                v-if="steps.length > 1"
                type="button"
                class="btn-icon-danger"
                :aria-label="t('tests.testCase.removeStep')"
                @click="removeStep(index)"
              >
                <Trash2 :size="16" /> {{ t('tests.testCase.removeStep') }}
              </button>
            </header>

            <div class="step-fields-grid">
              <label>
                <span>{{ t('tests.testCase.action') }} *</span>
                <textarea v-model="stepItem.action" rows="3" maxlength="4000" required :disabled="saving" />
              </label>

              <label>
                <span>{{ t('tests.testCase.expectedResult') }} *</span>
                <textarea v-model="stepItem.expectedResult" rows="3" maxlength="4000" required :disabled="saving" />
              </label>
            </div>
          </article>
        </div>
      </section>

    </form>
    <UiFormActionBar :mode="props.embedded ? 'fixed' : 'floating'">
      <template #status>
        <p v-if="!isValid" class="validation-hint">{{ validationHint }}</p>
      </template>
      <UiCreateActions
        :loading="saving"
        :disabled="!isValid"
        :allow-continue="false"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('tests.testCase.save')"
        @cancel="emit('cancel')"
        @create="save"
      />
    </UiFormActionBar>
  </div>
</template>

<style scoped>
.edit-page { display: grid; max-width: 960px; gap: 22px; margin: 0 auto; padding-bottom: 40px; }
.edit-page--embedded { display: flex; flex-direction: column; max-width: none; height: 100%; min-height: 0; margin: 0; padding: 20px; }
.edit-page--embedded .edit-form { flex: 1 1 auto; min-height: 0; overflow-y: auto; padding: 0 2px 18px; }
.back-link { display: flex; width: fit-content; align-items: center; gap: 6px; padding: 0; color: var(--kk-text-muted); background: transparent; border: 0; cursor: pointer; }
.page-header h2 { margin: 3px 0 7px; font-size: clamp(1.65rem, 3vw, 2.2rem); }
.page-header > span, .form-section p { color: var(--kk-text-muted); }

.eyebrow {
  font-size: 0.75rem;
  font-weight: 750;
  letter-spacing: 0.08em;
  color: var(--kk-accent);
  margin: 0;
  text-transform: uppercase;
}

.error-alert {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  margin-bottom: 1.5rem;
  font-size: 0.875rem;
}

.edit-form { display: grid; gap: 18px; }

.form-section {
  display: grid; gap: 18px; padding: 24px; background: var(--kk-surface); border: 1px solid var(--kk-border); border-radius: var(--kk-radius); box-shadow: var(--kk-shadow);
}

.form-section > header, .step-editor-card > header { display: flex; align-items: start; justify-content: space-between; gap: 16px; }
.form-section h3, .form-section p { margin: 0; }
.form-section p { margin-top: 4px; font-size: 0.84rem; }
.form-section label { display: grid; gap: 7px; font-size: 0.875rem; font-weight: 650; }
.form-section select, .form-section textarea { padding: 11px; color: var(--kk-text); background: var(--kk-surface); border: 1px solid var(--kk-border-strong); border-radius: var(--kk-radius); font: inherit; }
.step-editor-list { display: grid; gap: 14px; }

.step-editor-card {
  display: grid; gap: 14px; padding: 16px; background: var(--kk-surface-subtle); border: 1px solid var(--kk-border); border-radius: var(--kk-radius);
}

.step-editor-card > header span { display: flex; align-items: center; gap: 7px; font-weight: 750; }
.step-fields-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }

.btn-icon-danger {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  border: none;
  background: none;
  color: var(--kk-danger);
  font-size: 0.875rem;
  cursor: pointer;

  &:hover {
    color: #991b1b;
  }
}

.btn-icon {
  border: none;
  background: none;
  color: var(--kk-text-muted);
  cursor: pointer;
  padding: 0.25rem;

  &:disabled { color: #d1d5db; cursor: not-allowed; }
}

.validation-hint { margin: 0; color: #c05621; font-size: 0.84rem; font-weight: 600; }

@media (max-width: 720px) { .step-fields-grid { grid-template-columns: 1fr; } .form-section > header { align-items: stretch; flex-direction: column; } }
</style>
