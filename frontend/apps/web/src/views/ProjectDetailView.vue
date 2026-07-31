<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { RouterLink, useRoute } from 'vue-router'
import { UiButton } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { apiClient, problemMessage } from '../api/client'
import type { ProjectResponse } from '../api/contracts'

const route = useRoute()
const { t } = useI18n()
const project = ref<ProjectResponse>()
const loading = ref(true)
const error = ref('')

onMounted(loadProject)

async function loadProject(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    const result = await apiClient.getProject(String(route.params.projectId))
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.detail.loadError'))
      return
    }
    project.value = result.data
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="project-home">
    <RouterLink :to="{ name: 'projects' }" class="back-link">
      <ArrowLeft :size="17" aria-hidden="true" />
      {{ t('projects.detail.back') }}
    </RouterLink>

    <p v-if="loading" class="page-state">{{ t('projects.detail.loading') }}</p>
    <div v-else-if="error" class="page-state page-state--error" role="alert">
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="loadProject">{{ t('projects.detail.reload') }}</UiButton>
    </div>

    <template v-else-if="project">
      <header class="project-heading">
        <div>
          <p>{{ project.code }}</p>
          <h2>{{ project.name }}</h2>
          <span :class="`status-badge status-badge--${project.status}`">
            {{ t(`projects.detail.status.${project.status}`) }}
          </span>
        </div>
      </header>

      <div class="home-canvas" :aria-label="t('projects.detail.contentLabel')">
        <p>{{ t('projects.detail.placeholderTitle') }}</p>
        <span>{{ t('projects.detail.placeholderDescription') }}</span>
      </div>
    </template>
  </section>
</template>

<style scoped>
.project-home {
  display: grid;
  gap: 26px;
}

.back-link {
  display: inline-flex;
  width: fit-content;
  align-items: center;
  gap: 7px;
  color: var(--kk-text-muted);
  font-size: 0.875rem;
  font-weight: 650;
  text-decoration: none;
}

.project-heading p,
.project-heading h2,
.home-canvas p {
  margin: 0;
}

.project-heading p {
  margin-bottom: 4px;
  color: var(--kk-accent);
  font-size: 0.78rem;
  font-weight: 750;
  letter-spacing: 0.08em;
}

.project-heading h2 {
  margin-bottom: 11px;
  font-size: clamp(1.65rem, 3vw, 2.2rem);
}

.status-badge {
  display: inline-flex;
  padding: 4px 8px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
}

.status-badge--inactive {
  color: var(--kk-text-muted);
  background: #eaedeb;
}

.home-canvas {
  display: grid;
  min-height: 360px;
  place-content: center;
  gap: 8px;
  color: var(--kk-text-muted);
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.home-canvas p {
  color: var(--kk-text);
  font-weight: 700;
}

.page-state {
  padding: 42px 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.page-state--error {
  color: var(--kk-danger);
}
</style>
