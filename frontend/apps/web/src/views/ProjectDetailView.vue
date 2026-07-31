<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import SharedCardSection from '../components/SharedCardSection.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import { apiClient, problemMessage } from '../api/client'
import type { IssueResponse, ProjectResponse } from '../api/contracts'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()

const projectId = computed(() => String(route.params.projectId))
const project = ref<ProjectResponse>()
const memberCount = ref(0)
const totalIssues = ref(0)
const completedIssues = ref(0)
const recentIssues = ref<IssueResponse[]>([])

const loading = ref(true)
const error = ref('')

const completionRate = computed(() => {
  if (totalIssues.value === 0) return 0
  return Math.round((completedIssues.value / totalIssues.value) * 100)
})

onMounted(loadProject)

async function loadProject(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const [projectRes, memberRes, issueRes] = await Promise.all([
      apiClient.getProject(projectId.value),
      apiClient.listProjectMembers(projectId.value),
      apiClient.listIssues(projectId.value, 1, 50),
    ])

    if (!projectRes.data) {
      error.value = problemMessage(projectRes.error, t('projects.detail.loadError'))
      return
    }

    project.value = projectRes.data
    if (memberRes.data) {
      memberCount.value = memberRes.data.length
    }
    if (issueRes.data) {
      totalIssues.value = issueRes.data.totalCount
      const items = issueRes.data.items || []
      completedIssues.value = items.filter(
        (item) => item.statusCode === 'DONE' || item.statusCode === 'CLOSED' || item.statusCode === 'RESOLVED',
      ).length
      recentIssues.value = items.slice(0, 5)
    }
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    loading.value = false
  }
}

function formatDate(value: string): string {
  return d(new Date(value), 'dateTime')
}
</script>

<template>
  <section class="project-home">
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'projects' }"
      :back-label="t('projects.detail.back')"
      :items="[
        { label: t('projects.list.title'), to: { name: 'projects' } },
        { label: project?.name || t('projects.record'), active: true },
      ]"
    />

    <SharedStateBanner
      v-if="loading"
      type="loading"
      :title="t('projects.detail.loading')"
    />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="t('projects.detail.loadError')"
      :description="error"
      show-reload
      :reload-label="t('projects.detail.reload')"
      @reload="loadProject"
    />

    <template v-else-if="project">
      <ResourcePageHeader
        :meta="`${project.code} · PROJECT`"
        :title="project.name"
        :subtitle="project.description || '專案總覽儀表板與狀態管理'"
        :status="project.status"
      />

      <div class="home-canvas" :aria-label="t('projects.detail.contentLabel')">
        <!-- METRICS SUMMARY CARD -->
        <SharedCardSection
          :title="t('projects.detail.summaryTitle')"
          :description="t('projects.detail.summaryDescription')"
        >
          <div class="canvas-card__metrics">
            <div class="metric-item">
              <strong>{{ totalIssues }}</strong>
              <small>{{ t('projects.detail.metrics.issues') }}</small>
            </div>
            <div class="metric-item">
              <strong>{{ memberCount }}</strong>
              <small>{{ t('projects.detail.metrics.members') }}</small>
            </div>
            <div class="metric-item">
              <strong>{{ completionRate }}%</strong>
              <small>{{ t('projects.detail.metrics.completion') }}</small>
            </div>
          </div>
        </SharedCardSection>

        <!-- RECENT ISSUES CARD -->
        <SharedCardSection
          :title="t('projects.detail.recentActivity')"
          :description="t('projects.detail.recentActivityDescription')"
        >
          <ul v-if="recentIssues.length" class="activity-list">
            <li
              v-for="issue in recentIssues"
              :key="issue.id"
              class="activity-item"
              @click="router.push({ name: 'project-issue-edit', params: { projectId, issueId: issue.id } })"
            >
              <div class="activity-info">
                <span class="issue-key">{{ issue.key }}</span>
                <span class="issue-title">{{ issue.title }}</span>
              </div>
              <small class="activity-time">{{ formatDate(issue.updatedAt) }}</small>
            </li>
          </ul>
          <p v-else class="empty-activities">目前尚無任務動態與紀錄。</p>
        </SharedCardSection>
      </div>
    </template>
  </section>
</template>

<style scoped>
.project-home {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}

.home-canvas {
  display: grid;
  grid-template-columns: 1.5fr 1fr;
  gap: 16px;
}

.canvas-card__metrics {
  display: flex;
  gap: 32px;
  margin-top: 8px;
}

.metric-item {
  display: flex;
  flex-direction: column;
}

.metric-item strong {
  font-size: 1.6rem;
  font-weight: 800;
  color: var(--kk-accent);
}

.metric-item small {
  font-size: 0.82rem;
  color: var(--kk-text-muted);
  margin-top: 2px;
}

.activity-list {
  list-style: none;
  padding: 0;
  margin: 4px 0 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.activity-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.85rem;
  border-bottom: 1px solid var(--kk-border);
  padding: 8px 4px;
  cursor: pointer;
  border-radius: 4px;
  transition: background 0.12s ease;
}

.activity-item:hover {
  background: #f9fafb;
}

.activity-item:last-child {
  border-bottom: none;
}

.activity-info {
  display: flex;
  align-items: center;
  gap: 8px;
  overflow: hidden;
}

.issue-key {
  font-weight: 700;
  font-family: monospace;
  color: var(--kk-accent);
  font-size: 0.82rem;
}

.issue-title {
  color: var(--kk-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.activity-time {
  font-size: 0.78rem;
  color: var(--kk-text-muted);
  white-space: nowrap;
}

.empty-activities {
  margin-top: 8px;
  font-size: 0.85rem;
  color: var(--kk-text-muted);
}

@media (max-width: 900px) {
  .home-canvas {
    grid-template-columns: 1fr;
  }
}
</style>
