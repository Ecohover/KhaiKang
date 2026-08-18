<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { Bug, Link2, Plus } from '@lucide/vue'
import { UiButton, UiField, UiSelect } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type {
  TestRunBugLinkResponse,
  TestTraceIssueResponse,
  TestWorkspaceProjectResponse,
} from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const props = defineProps<{
  workspaceId: string
  runId: string
  testIssue: TestTraceIssueResponse | null
  canCreateBug: boolean
}>()

const { t } = useI18n()
const { showCreated } = useSaveNotice()
const projects = ref<TestWorkspaceProjectResponse[]>([])
const bugs = ref<TestRunBugLinkResponse[]>([])
const projectId = ref('')
const title = ref('')
const description = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')

onMounted(load)

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [projectResult, bugResult] = await Promise.all([
    apiClient.listTestWorkspaceProjects(props.workspaceId),
    apiClient.listTestRunBugs(props.workspaceId, props.runId),
  ])
  projects.value = projectResult.data ?? []
  bugs.value = bugResult.data ?? []
  projectId.value ||= projects.value[0]?.projectId ?? ''
  const loadError = projectResult.error ?? bugResult.error
  error.value = loadError
    ? problemMessage(loadError, t('tests.run.trace.loadFailed'))
    : ''
  loading.value = false
}

async function createBug(): Promise<void> {
  if (!projectId.value || !title.value.trim() || saving.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.createTestRunBug(
    props.workspaceId,
    props.runId,
    {
      projectId: projectId.value,
      title: title.value.trim(),
      priorityCode: null,
      description: description.value.trim() || null,
      assigneeAccountId: null,
    },
    await secureHeaders(),
  )
  if (result.data) {
    bugs.value = [...bugs.value, result.data]
      .sort((left, right) => left.issue.key.localeCompare(right.issue.key))
    title.value = ''
    description.value = ''
    showCreated(t('tests.run.trace.bugRecord'), result.data.issue.key)
  } else {
    error.value = problemMessage(result.error, t('tests.run.trace.createFailed'))
  }
  saving.value = false
}
</script>

<template>
  <section class="trace-panel">
    <header class="trace-heading">
      <Link2 :size="19" aria-hidden="true" />
      <div>
        <h3>{{ t('tests.run.trace.title') }}</h3>
        <p>{{ t('tests.run.trace.description') }}</p>
      </div>
    </header>

    <p v-if="error" class="trace-error" role="alert">{{ error }}</p>
    <p v-if="loading" class="trace-empty">{{ t('tests.run.trace.loading') }}</p>
    <template v-else>
      <div class="test-issue-block">
        <strong>{{ t('tests.run.trace.testIssue') }}</strong>
        <RouterLink
          v-if="testIssue"
          :to="{ name: 'project-issue-edit', params: { projectId: testIssue.projectId, issueId: testIssue.id } }"
        >
          <span>{{ testIssue.key }}</span>{{ testIssue.title }}
        </RouterLink>
        <span v-else class="trace-empty">{{ t('tests.run.trace.noTestIssue') }}</span>
      </div>

      <div class="bug-block">
        <div class="bug-heading"><Bug :size="18" aria-hidden="true" /><strong>{{ t('tests.run.trace.bugs') }}</strong></div>
        <div v-if="canCreateBug" class="bug-create">
          <label>
            <span>{{ t('tests.run.trace.project') }}</span>
            <UiSelect v-model="projectId" :disabled="saving">
              <option value="">{{ t('tests.run.trace.selectProject') }}</option>
              <option v-for="project in projects" :key="project.id" :value="project.projectId">
                {{ project.code }} · {{ project.name }}
              </option>
            </UiSelect>
          </label>
          <UiField
            id="run-bug-title"
            v-model="title"
            :label="t('tests.run.trace.bugTitle')"
            :placeholder="t('tests.run.trace.bugTitlePlaceholder')"
            maxlength="200"
            :disabled="saving"
          />
          <label class="bug-description">
            <span>{{ t('tests.run.trace.bugDescription') }}</span>
            <textarea v-model="description" rows="2" maxlength="20000" :disabled="saving" />
          </label>
          <UiButton type="button" :disabled="!projectId || !title.trim() || saving" :loading="saving" @click="createBug">
            <Plus :size="16" aria-hidden="true" />{{ t('tests.run.trace.createBug') }}
          </UiButton>
        </div>

        <p v-if="bugs.length === 0" class="trace-empty">{{ t('tests.run.trace.noBugs') }}</p>
        <ul v-else class="bug-list">
          <li v-for="bugLink in bugs" :key="bugLink.id">
            <RouterLink :to="{ name: 'project-issue-edit', params: { projectId: bugLink.issue.projectId, issueId: bugLink.issue.id } }">
              <strong>{{ bugLink.issue.key }}</strong><span>{{ bugLink.issue.title }}</span>
            </RouterLink>
            <span class="bug-status">{{ bugLink.issue.statusCode }}</span>
          </li>
        </ul>
      </div>
    </template>
  </section>
</template>

<style scoped>
.trace-panel{display:grid;gap:18px;padding:22px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}
.trace-heading,.bug-heading{display:flex;align-items:flex-start;gap:9px}.trace-heading h3,.trace-heading p{margin:0}.trace-heading p{margin-top:3px;color:var(--kk-text-muted);font-size:.84rem}.test-issue-block{display:grid;gap:7px;padding:12px;background:var(--kk-surface-subtle);border-radius:7px}.test-issue-block a,.bug-list a{display:flex;min-width:0;gap:8px;color:inherit;text-decoration:none}.test-issue-block a span{color:var(--kk-accent);font-weight:750}.bug-block{display:grid;gap:12px;padding-top:4px}.bug-create{display:grid;grid-template-columns:minmax(160px,.7fr) minmax(220px,1fr) minmax(260px,1.2fr) auto;align-items:end;gap:12px}.bug-create label{display:grid;gap:6px;font-size:.84rem;font-weight:650}.bug-create textarea{width:100%;box-sizing:border-box;padding:8px 10px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:6px;font:inherit;resize:vertical}.bug-create button{min-height:38px}.bug-list{display:grid;gap:8px;margin:0;padding:0;list-style:none}.bug-list li{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:10px 12px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-radius:7px}.bug-list a span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.bug-status{padding:3px 8px;color:var(--kk-text-muted);background:var(--kk-surface);border-radius:999px;font-size:.75rem}.trace-empty{margin:0;color:var(--kk-text-muted);font-size:.84rem}.trace-error{margin:0;color:var(--kk-danger)}
@media(max-width:950px){.bug-create{grid-template-columns:1fr 1fr}.bug-description{grid-column:1/-1}}@media(max-width:640px){.bug-create{grid-template-columns:1fr}.bug-description{grid-column:auto}.bug-list a{display:grid;gap:2px}}
</style>
