<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import TestCaseEditForm from '../components/TestCaseEditForm.vue'
import { apiClient, problemMessage } from '../api/client'
import type { TestCaseResponse, TestSuiteResponse, TestWorkspaceResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const workspaceId = computed(() => String(route.params.workspaceId))
const caseId = computed(() => String(route.params.caseId))

const workspace = ref<TestWorkspaceResponse>()
const suites = ref<TestSuiteResponse[]>([])
const originalCase = ref<TestCaseResponse>()
const loading = ref(true)
const error = ref('')
const isDirty = ref(false)
const { showUpdated } = useSaveNotice()

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [workspaceResult, suiteResult, caseResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
    apiClient.getTestCase(workspaceId.value, caseId.value),
  ])

  workspace.value = workspaceResult.data
  suites.value = suiteResult.data ?? []
  originalCase.value = caseResult.data

  error.value = problemMessage(
    workspaceResult.error ?? suiteResult.error ?? caseResult.error,
    caseResult.data ? '' : t('tests.testCase.loadFailed'),
  )
  loading.value = false
}

function handleSaved(updatedCase: TestCaseResponse): void {
  showUpdated(t('tests.testCase.record'), updatedCase.title)
  router.push({
    name: 'test-suites',
    params: { workspaceId: workspaceId.value },
    query: { caseId: updatedCase.id },
  })
}

function handleCancel(): void {
  router.push({
    name: 'test-suites',
    params: { workspaceId: workspaceId.value },
    query: { caseId: caseId.value },
  })
}

onMounted(load)

onBeforeRouteLeave(() => {
  if (!isDirty.value) return true
  return window.confirm(t('tests.testCase.unsavedChanges'))
})
</script>

<template>
  <section class="edit-page">
    <p v-if="loading" class="state-panel">{{ t('tests.workspace.loading') }}</p>
    <div v-else-if="error" class="error-alert">{{ error }}</div>
    <TestCaseEditForm
      v-else-if="originalCase"
      :workspace-id="workspaceId"
      :workspace="workspace"
      :test-case="originalCase"
      :suites="suites"
      @dirty-change="isDirty = $event"
      @saved="handleSaved"
      @cancel="handleCancel"
    />
  </section>
</template>

<style scoped>
.edit-page {
  padding: 1.5rem;
}

.state-panel {
  padding: 3rem;
  text-align: center;
  color: #6b7280;
  background: #ffffff;
  border-radius: 12px;
  border: 1px solid #e5e7eb;
}

.error-alert {
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
  padding: 0.75rem 1rem;
  border-radius: 8px;
  margin-bottom: 1.5rem;
}
</style>
