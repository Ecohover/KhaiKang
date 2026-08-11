<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Link2, Plus, Trash2 } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { listWorkspaceIssues } from '../api/issueOptions'
import type { IssueResponse, TestCaseRequirementLinkResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'
import IssueKeyPicker from './IssueKeyPicker.vue'

const props = defineProps<{
  workspaceId: string
  testCaseId: string
  canManage: boolean
}>()

const { t } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()
const links = ref<TestCaseRequirementLinkResponse[]>([])
const issues = ref<IssueResponse[]>([])
const selectedIssueId = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')

const candidates = computed(() => {
  const linkedIds = new Set(links.value.map(item => item.issue.id))
  return issues.value.filter(item => !linkedIds.has(item.id))
})

onMounted(load)

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [linkResult, issueResult] = await Promise.all([
    apiClient.listTestCaseRequirementIssues(props.workspaceId, props.testCaseId),
    listWorkspaceIssues(props.workspaceId),
  ])
  links.value = linkResult.data ?? []
  issues.value = issueResult.issues
  const loadError = linkResult.error ?? issueResult.error
  error.value = loadError
    ? problemMessage(loadError, t('tests.testCase.requirements.loadFailed'))
    : ''
  loading.value = false
}

async function add(): Promise<void> {
  if (!selectedIssueId.value || saving.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.linkTestCaseRequirementIssue(
    props.workspaceId,
    props.testCaseId,
    { requirementIssueId: selectedIssueId.value },
    await secureHeaders(),
  )
  if (result.data) {
    links.value = [...links.value, result.data]
      .sort((left, right) => left.issue.key.localeCompare(right.issue.key))
    selectedIssueId.value = ''
    showCreated(t('tests.testCase.requirements.record'), result.data.issue.key)
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.requirements.addFailed'))
  }
  saving.value = false
}

async function remove(link: TestCaseRequirementLinkResponse): Promise<void> {
  if (saving.value || !window.confirm(t('tests.testCase.requirements.removeConfirm'))) return
  saving.value = true
  error.value = ''
  const result = await apiClient.unlinkTestCaseRequirementIssue(
    props.workspaceId,
    props.testCaseId,
    link.id,
    link.version,
    await secureHeaders(),
  )
  if (result.error) {
    error.value = problemMessage(result.error, t('tests.testCase.requirements.removeFailed'))
  } else {
    links.value = links.value.filter(item => item.id !== link.id)
    showUpdated(t('tests.testCase.requirements.record'), link.issue.key)
  }
  saving.value = false
}
</script>

<template>
  <section class="requirements-section">
    <header>
      <div class="heading">
        <Link2 :size="19" aria-hidden="true" />
        <div>
          <h3>{{ t('tests.testCase.requirements.title') }}</h3>
          <p>{{ t('tests.testCase.requirements.description') }}</p>
        </div>
      </div>
    </header>
    <p v-if="error" class="section-error" role="alert">{{ error }}</p>
    <p v-if="loading" class="empty">{{ t('tests.testCase.requirements.loading') }}</p>
    <template v-else>
      <div v-if="canManage" class="requirement-create">
        <IssueKeyPicker
          id="test-case-requirement-issue"
          v-model="selectedIssueId"
          :issues="candidates"
          :label="t('tests.testCase.requirements.issue')"
          :placeholder="t('tests.testCase.requirements.issueKeyPlaceholder')"
          :search-label="t('tests.testCase.requirements.searchIssue')"
          :not-found-message="t('tests.testCase.requirements.issueNotFound')"
          :disabled="saving"
        />
        <UiButton type="button" :disabled="!selectedIssueId || saving" :loading="saving" @click="add">
          <Plus :size="16" aria-hidden="true" />{{ t('tests.testCase.requirements.add') }}
        </UiButton>
      </div>
      <p v-if="links.length === 0" class="empty">{{ t('tests.testCase.requirements.empty') }}</p>
      <ul v-else class="requirement-list">
        <li v-for="link in links" :key="link.id">
          <RouterLink :to="{ name: 'project-issue-edit', params: { projectId: link.issue.projectId, issueId: link.issue.id } }">
            <strong>{{ link.issue.key }}</strong><span>{{ link.issue.title }}</span>
          </RouterLink>
          <UiButton
            v-if="canManage"
            type="button"
            variant="secondary"
            :disabled="saving"
            :aria-label="t('tests.testCase.requirements.remove')"
            @click="remove(link)"
          ><Trash2 :size="15" aria-hidden="true" /></UiButton>
        </li>
      </ul>
    </template>
  </section>
</template>

<style scoped>
.requirements-section{display:grid;gap:18px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}
.requirements-section header,.heading{display:flex;align-items:flex-start;gap:10px}.requirements-section h3,.requirements-section p{margin:0}.requirements-section header p,.empty{color:var(--kk-text-muted);font-size:.84rem}.requirement-create{display:grid;grid-template-columns:minmax(0,1fr) auto;align-items:end;gap:12px}.requirement-create>button{min-height:32px}.requirement-list{display:grid;gap:8px;margin:0;padding:0;list-style:none}.requirement-list li{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 12px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-radius:7px}.requirement-list a{display:flex;min-width:0;gap:8px;color:inherit;text-decoration:none}.requirement-list a span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.section-error{color:var(--kk-danger)}
@media(max-width:640px){.requirement-create{grid-template-columns:1fr}.requirement-list a{display:grid;gap:2px}}
</style>
