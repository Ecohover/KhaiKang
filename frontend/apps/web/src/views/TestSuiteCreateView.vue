<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestSuiteResponse, TestWorkspaceResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const workspaceId = computed(() => String(route.params.workspaceId))
const workspace = ref<TestWorkspaceResponse>()
const suites = ref<TestSuiteResponse[]>([])
const parentId = ref<string | null>(typeof route.query.parentId === 'string' ? route.query.parentId : null)
const name = ref('')
const description = ref('')
const loading = ref(true)
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()

const orderedSuites = computed(() => {
  const result: TestSuiteResponse[] = []
  const appendChildren = (currentParentId: string | null): void => {
    suites.value
      .filter((suite) => suite.parentId === currentParentId)
      .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))
      .forEach((suite) => {
        result.push(suite)
        appendChildren(suite.id)
      })
  }
  appendChildren(null)
  return result
})

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, suiteResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  suites.value = suiteResult.data ?? []
  error.value = problemMessage(
    workspaceResult.error ?? suiteResult.error,
    workspace.value ? '' : t('tests.workspace.loadFailed'),
  )
  loading.value = false
}

async function create(continueCreating: boolean): Promise<void> {
  if (!name.value.trim() || creating.value) return
  creating.value = true
  error.value = ''
  const result = await apiClient.createTestSuite(workspaceId.value, {
    parentId: parentId.value,
    name: name.value.trim(),
    description: description.value.trim() || null,
    sortOrder: suites.value.length + 1,
  }, await secureHeaders())

  if (result.data) {
    showCreated(t('tests.suite.record'), result.data.name)
    if (continueCreating) {
      suites.value.push(result.data)
      name.value = ''
      description.value = ''
      await nextTick()
      document.getElementById('test-suite-name')?.focus()
    } else {
      await router.push({ name: 'test-suites', params: { workspaceId: workspaceId.value } })
    }
  } else {
    error.value = problemMessage(result.error, t('tests.suite.createFailed'))
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
      <ArrowLeft :size="16" />{{ t('tests.suite.backToList') }}
    </button>

    <header>
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ t('tests.suite.create') }}</h2>
      <span>{{ workspace?.prefix }} · {{ workspace?.name }}</span>
    </header>

    <p v-if="loading" class="state-panel">{{ t('tests.workspace.loading') }}</p>
    <form v-else-if="workspace" class="create-form" @submit.prevent="create(false)">
      <label>
        <span>{{ t('tests.suite.parent') }}</span>
        <select v-model="parentId" :disabled="creating">
          <option :value="null">{{ t('tests.suite.root') }}</option>
          <option
            v-for="suite in orderedSuites.filter((item) => item.depth < 5)"
            :key="suite.id"
            :value="suite.id"
          >
            {{ '—'.repeat(suite.depth - 1) }} {{ suite.name }}
          </option>
        </select>
      </label>
      <UiField
        id="test-suite-name"
        v-model="name"
        :label="t('tests.suite.name')"
        :placeholder="t('tests.suite.namePlaceholder')"
        :disabled="creating"
      />
      <label>
        <span>{{ t('tests.suite.description') }}</span>
        <textarea v-model="description" rows="7" maxlength="4000" :disabled="creating" />
      </label>
      <p v-if="error" class="error" role="alert">{{ error }}</p>
      <UiCreateActions
        :loading="creating"
        :disabled="!name.trim()"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('tests.suite.create')"
        :continue-label="t('tests.suite.createAndContinue')"
        @cancel="router.push({ name: 'test-suites', params: { workspaceId } })"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </form>
    <div v-else class="state-panel state-panel--error" role="alert">{{ error }}</div>
  </section>
</template>

<style scoped>
.create-page{display:grid;max-width:900px;gap:22px;margin:0 auto}.back-link{display:flex;width:fit-content;align-items:center;gap:6px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}.create-page header h2{margin:3px 0 7px;font-size:clamp(1.65rem,3vw,2.2rem)}.create-page header span{color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.create-form{display:grid;gap:22px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.create-form label{display:grid;gap:7px;font-size:.875rem;font-weight:650}.create-form select,.create-form textarea{padding:11px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:var(--kk-radius);font:inherit}.error,.state-panel--error{color:var(--kk-danger)}.state-panel{margin:0;padding:42px 24px;text-align:center;background:var(--kk-surface);border:1px dashed var(--kk-border-strong);border-radius:var(--kk-radius)}
</style>
