<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ArrowLeft, GripVertical, Plus, Trash2 } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestCaseResponse, TestSuiteResponse } from '../api/contracts'

interface EditableStep {
  key: number
  action: string
  expectedResult: string
}

const props = defineProps<{
  workspaceId: string
  testCase: TestCaseResponse
  suites: TestSuiteResponse[]
}>()

const emit = defineEmits<{
  (e: 'saved', updatedCase: TestCaseResponse): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const suiteId = ref(props.testCase.suiteId)
const title = ref(props.testCase.title)
const description = ref(props.testCase.description ?? '')
const preconditions = ref(props.testCase.preconditions ?? '')
const overallExpectedResult = ref(props.testCase.overallExpectedResult ?? '')
const status = ref<'active' | 'inactive'>(props.testCase.status)
const sortOrder = ref(props.testCase.sortOrder)
const steps = ref<EditableStep[]>([])
const saving = ref(false)
const error = ref('')
const nextStepKey = ref(1)

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
    steps.value = newCase.steps.map((step, idx) => ({
      key: idx + 1,
      action: step.action,
      expectedResult: step.expectedResult,
    }))
    nextStepKey.value = newCase.steps.length + 1
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
      version: props.testCase.version,
      steps: steps.value.map((step) => ({
        action: step.action.trim(),
        expectedResult: step.expectedResult.trim(),
      })),
    },
    await secureHeaders(),
  )

  if (result.data) {
    emit('saved', result.data)
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.updateFailed'))
  }
  saving.value = false
}
</script>

<template>
  <div class="test-case-edit-form">
    <header class="edit-form-header">
      <div class="header-info">
        <button type="button" class="btn-back" @click="emit('cancel')">
          <ArrowLeft :size="16" /> {{ t('tests.testCase.backToList') }}
        </button>
        <p class="eyebrow">{{ t('tests.management') }}</p>
        <h2>{{ t('tests.testCase.edit') }} — {{ props.testCase.title }}</h2>
      </div>
    </header>

    <div v-if="error" class="error-alert">{{ error }}</div>

    <form class="edit-form-body" @submit.prevent="save">
      <section class="form-section">
        <header class="section-header">
          <h3>{{ t('tests.testCase.basicInformation') }}</h3>
          <p>{{ t('tests.testCase.basicInformationHint') }}</p>
        </header>

        <div class="form-grid">
          <label class="form-field">
            <span>{{ t('tests.testCase.suite') }} *</span>
            <select v-model="suiteId" required>
              <option value="" disabled>{{ t('tests.testCase.selectSuite') }}</option>
              <option v-for="s in activeSuites" :key="s.id" :value="s.id">
                {{ '— '.repeat(s.depth - 1) }}{{ s.name }}
              </option>
            </select>
          </label>

          <label class="form-field">
            <span>{{ t('common.fields.status') }}</span>
            <select v-model="status">
              <option value="active">{{ t('common.status.active') }}</option>
              <option value="inactive">{{ t('common.status.inactive') }}</option>
            </select>
          </label>
        </div>

        <UiField
          id="case-title"
          v-model="title"
          :label="t('tests.testCase.title') + ' *'"
          :placeholder="t('tests.testCase.titlePlaceholder')"
          required
        />

        <label class="form-field">
          <span>{{ t('tests.testCase.description') }}</span>
          <textarea v-model="description" rows="3"></textarea>
        </label>

        <label class="form-field">
          <span>{{ t('tests.testCase.preconditions') }}</span>
          <textarea v-model="preconditions" rows="2"></textarea>
        </label>

        <label class="form-field">
          <span>{{ t('tests.testCase.overallExpectedResult') }}</span>
          <textarea v-model="overallExpectedResult" rows="2"></textarea>
        </label>
      </section>

      <section class="form-section">
        <header class="section-header">
          <div>
            <h3>{{ t('tests.testCase.steps') }}</h3>
            <p>{{ t('tests.testCase.stepsHint') }}</p>
          </div>
          <button type="button" class="btn-subtle" @click="addStep">
            <Plus :size="16" /> {{ t('tests.testCase.addStep') }}
          </button>
        </header>

        <div class="step-editor-list">
          <article v-for="(stepItem, index) in steps" :key="stepItem.key" class="step-editor-card">
            <div class="step-card-header">
              <span><GripVertical :size="17" /> {{ t('tests.testCase.stepNumber', { number: index + 1 }) }}</span>
              <button
                v-if="steps.length > 1"
                type="button"
                class="btn-icon-danger"
                :aria-label="t('tests.testCase.removeStep')"
                @click="removeStep(index)"
              >
                <Trash2 :size="16" /> {{ t('tests.testCase.removeStep') }}
              </button>
            </div>

            <div class="step-fields-grid">
              <label class="form-field">
                <span>{{ t('tests.testCase.action') }} *</span>
                <textarea v-model="stepItem.action" rows="2" required></textarea>
              </label>

              <label class="form-field">
                <span>{{ t('tests.testCase.expectedResult') }} *</span>
                <textarea v-model="stepItem.expectedResult" rows="2" required></textarea>
              </label>
            </div>
          </article>
        </div>
      </section>

      <!-- STICKY ACTION BAR -->
      <footer class="sticky-action-bar">
        <div class="action-bar-content">
          <div class="validation-status">
            <span v-if="validationHint" class="hint-text font-medium text-amber-600">
              ⚠️ {{ validationHint }}
            </span>
            <span v-else class="hint-text text-emerald-600">
              ✓ {{ t('tests.testCase.completedHint') }}
            </span>
          </div>
          <div class="action-buttons">
            <UiButton
              type="button"
              variant="secondary"
              :disabled="saving"
              @click="emit('cancel')"
            >
              {{ t('common.actions.cancel') }}
            </UiButton>
            <UiButton
              type="submit"
              :disabled="saving || !isValid"
            >
              {{ saving ? t('tests.workspace.loading') : t('tests.testCase.save') }}
            </UiButton>
          </div>
        </div>
      </footer>
    </form>
  </div>
</template>

<style scoped>
.test-case-edit-form {
  padding: 1.5rem;
  max-width: 960px;
  margin: 0 auto;
}

.edit-form-header {
  margin-bottom: 1.5rem;
}

.btn-back {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.25rem 0;
  border: none;
  background: none;
  font-size: 0.875rem;
  color: #4b5563;
  cursor: pointer;
  margin-bottom: 0.5rem;

  &:hover {
    color: #111827;
  }
}

.eyebrow {
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  color: #6b7280;
  margin: 0;
}

h2 {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0.25rem 0 0;
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

.edit-form-body {
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

.form-section {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;

  h3 {
    font-size: 1.125rem;
    font-weight: 600;
    color: #111827;
    margin: 0;
  }

  p {
    font-size: 0.875rem;
    color: #6b7280;
    margin: 0.25rem 0 0;
  }
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  font-size: 0.875rem;

  span {
    font-weight: 500;
    color: #374151;
  }

  select,
  textarea {
    width: 100%;
    padding: 0.5rem 0.75rem;
    border: 1px solid #d1d5db;
    border-radius: 6px;
    font-size: 0.875rem;
    background-color: #fff;
    box-sizing: border-box;

    &:focus {
      outline: none;
      border-color: #059669;
      box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.15);
    }
  }
}

.step-editor-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.step-editor-card {
  border: 1px solid #e5e7eb;
  background-color: #f9fafb;
  border-radius: 8px;
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.step-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.875rem;
  font-weight: 600;
  color: #374151;

  span {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
  }
}

.step-fields-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

.btn-subtle {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  background: #ffffff;
  font-size: 0.875rem;
  color: #374151;
  cursor: pointer;

  &:hover {
    background: #f3f4f6;
  }
}

.btn-icon-danger {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  border: none;
  background: none;
  color: #dc2626;
  font-size: 0.875rem;
  cursor: pointer;

  &:hover {
    color: #991b1b;
  }
}

.sticky-action-bar {
  position: sticky;
  bottom: 0;
  z-index: 100;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(8px);
  border-top: 1px solid #e5e7eb;
  padding: 0.875rem 1.5rem;
  margin: 0 -1.5rem -1.5rem -1.5rem;
  box-shadow: 0 -4px 12px rgba(0, 0, 0, 0.05);
}

.action-bar-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.validation-status {
  font-size: 0.875rem;

  .hint-text {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
  }
}

.action-buttons {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
</style>
