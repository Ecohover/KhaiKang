<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestSuiteResponse } from '../api/contracts'

const props = defineProps<{
  workspaceId: string
  suite: TestSuiteResponse
  suites: TestSuiteResponse[]
}>()

const emit = defineEmits<{
  (e: 'saved', updatedSuite: TestSuiteResponse): void
  (e: 'cancel'): void
}>()

const { t } = useI18n()

const name = ref(props.suite.name)
const parentId = ref<string | null>(props.suite.parentId)
const description = ref(props.suite.description ?? '')
const status = ref<'active' | 'inactive'>(props.suite.status)
const sortOrder = ref(props.suite.sortOrder)
const saving = ref(false)
const error = ref('')

watch(
  () => props.suite,
  (newSuite) => {
    if (!newSuite) return
    name.value = newSuite.name
    parentId.value = newSuite.parentId
    description.value = newSuite.description ?? ''
    status.value = newSuite.status
    sortOrder.value = newSuite.sortOrder
  },
  { immediate: true },
)

// Prevent selecting self or descendant suites as parent
const isDescendant = (candidateId: string, targetAncestorId: string): boolean => {
  let current = props.suites.find((s) => s.id === candidateId)
  while (current && current.parentId) {
    if (current.parentId === targetAncestorId) return true
    current = props.suites.find((s) => s.id === current!.parentId)
  }
  return false
}

const eligibleParentSuites = computed(() =>
  props.suites
    .filter((s) => s.id !== props.suite.id && !isDescendant(s.id, props.suite.id))
    .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name)),
)

const validationHint = computed(() => {
  if (!name.value.trim()) return '請填寫測試套件名稱'
  return ''
})

const isValid = computed(() => validationHint.value === '')

async function save(): Promise<void> {
  if (!isValid.value || saving.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.updateTestSuite(
    props.workspaceId,
    props.suite.id,
    {
      parentId: parentId.value || null,
      name: name.value.trim(),
      description: description.value.trim() || null,
      sortOrder: sortOrder.value,
      status: status.value,
      version: props.suite.version,
    },
    await secureHeaders(),
  )

  if (result.data) {
    emit('saved', result.data)
  } else {
    error.value = problemMessage(result.error, t('tests.suite.updateFailed'))
  }
  saving.value = false
}
</script>

<template>
  <div class="test-suite-edit-form">
    <header class="edit-form-header">
      <div class="header-info">
        <button type="button" class="btn-back" @click="emit('cancel')">
          <ArrowLeft :size="16" /> {{ t('tests.testCase.backToList') }}
        </button>
        <p class="eyebrow">{{ t('tests.management') }}</p>
        <h2>{{ t('tests.suite.editTitle', '編輯測試套件') }} — {{ props.suite.name }}</h2>
      </div>
    </header>

    <div v-if="error" class="error-alert">{{ error }}</div>

    <form class="edit-form-body" @submit.prevent="save">
      <section class="form-section">
        <header class="section-header">
          <h3>{{ t('tests.testCase.basicInformation') }}</h3>
          <p>更新套件名稱、層級關係與運作狀態設定。</p>
        </header>

        <div class="form-grid">
          <label class="form-field">
            <span>{{ t('tests.suite.parent') }}</span>
            <select v-model="parentId">
              <option :value="null">{{ t('tests.suite.root') }}</option>
              <option v-for="s in eligibleParentSuites" :key="s.id" :value="s.id">
                {{ '— '.repeat(s.depth - 1) }}{{ s.name }}
              </option>
            </select>
          </label>

          <label class="form-field">
            <span>狀態</span>
            <select v-model="status">
              <option value="active">使用中 (Active)</option>
              <option value="inactive">停用中 (Inactive)</option>
            </select>
          </label>
        </div>

        <UiField
          id="suite-name"
          v-model="name"
          :label="t('tests.suite.name') + ' *'"
          :placeholder="t('tests.suite.namePlaceholder')"
          required
        />

        <label class="form-field">
          <span>{{ t('tests.suite.description') }}</span>
          <textarea v-model="description" rows="3"></textarea>
        </label>
      </section>

      <!-- STICKY ACTION BAR -->
      <footer class="sticky-action-bar">
        <div class="action-bar-content">
          <div class="validation-status">
            <span v-if="validationHint" class="hint-text font-medium text-amber-600">
              ⚠️ {{ validationHint }}
            </span>
            <span v-else class="hint-text text-emerald-600">
              ✓ 所有必要欄位均已完整填寫
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
.test-suite-edit-form {
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
