<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ArrowRight, ClipboardCheck, Plus } from '@lucide/vue'
import { RouterLink, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiViewModeToggle } from '@khaikang/ui'
import { apiClient, problemMessage } from '../api/client'
import type { TestWorkspaceResponse } from '../api/contracts'

const workspaces = ref<TestWorkspaceResponse[]>([])
const router = useRouter()
const { t, d } = useI18n()
const loading = ref(true)
const error = ref('')
const viewMode = ref<'list' | 'grid'>('list')

async function load(): Promise<void> {
  loading.value = true
  const result = await apiClient.listTestWorkspaces()
  error.value = result.error ? problemMessage(result.error, t('tests.workspace.loadFailed')) : ''
  workspaces.value = result.data ?? []
  loading.value = false
}

function formatDate(value: string): string {
  return d(new Date(value), 'medium')
}

onMounted(load)
</script>

<template>
  <section class="page">
    <header class="page-heading">
      <div>
        <p class="eyebrow">{{ t('tests.management') }}</p>
        <h2>{{ t('tests.workspace.title') }}</h2>
        <p>{{ t('tests.workspace.description') }}</p>
      </div>
      <UiButton @click="router.push({ name: 'test-workspace-new' })">
        <Plus :size="18" />{{ t('tests.workspace.create') }}
      </UiButton>
    </header>

    <div class="list-toolbar">
      <span>{{ t('tests.workspace.count', { count: workspaces.length }, workspaces.length) }}</span>
      <UiViewModeToggle
        v-model="viewMode"
        storage-key="khaikang.test-workspaces.view-mode"
        :group-label="t('common.viewMode.label')"
        :list-label="t('common.viewMode.list')"
        :grid-label="t('common.viewMode.grid')"
      />
    </div>

    <p v-if="loading" class="state-panel">{{ t('tests.workspace.loading') }}</p>
    <div v-else-if="error" class="state-panel state-panel--error" role="alert">
      <p>{{ error }}</p>
      <UiButton variant="secondary" @click="load">{{ t('common.actions.reload') }}</UiButton>
    </div>
    <div v-else-if="workspaces.length" class="entity-collection" :class="`entity-collection--${viewMode}`">
      <RouterLink
        v-for="workspace in workspaces"
        :key="workspace.id"
        :to="{ name: 'test-suites', params: { workspaceId: workspace.id } }"
        class="entity-card"
      >
        <div class="entity-card__icon"><ClipboardCheck :size="22" /></div>
        <div class="entity-card__main">
          <div class="entity-card__title"><span>{{ workspace.prefix }}</span><h3>{{ workspace.name }}</h3></div>
          <p>{{ workspace.description || t('tests.workspace.noDescription') }}</p>
        </div>
        <span class="status-badge" :class="`status-badge--${workspace.status}`">
          {{ t(`tests.workspace.${workspace.status}`) }}
        </span>
        <div class="entity-card__meta">
          <span>{{ workspace.currentUserRole }}</span>
          <span>{{ t('tests.workspace.updatedAt', { date: formatDate(workspace.updatedAt) }) }}</span>
          <ArrowRight :size="17" />
        </div>
      </RouterLink>
    </div>
    <div v-else class="empty-state">
      <ClipboardCheck :size="34" />
      <h3>{{ t('tests.workspace.emptyTitle') }}</h3>
      <p>{{ t('tests.workspace.emptyDescription') }}</p>
    </div>
  </section>
</template>

<style scoped>
.page{display:grid;gap:24px}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:24px}.page-heading h2,.entity-card h3,.empty-state h3{margin:0}.page-heading h2{margin-top:3px;font-size:clamp(1.65rem,3vw,2.2rem)}.page-heading>div>p:last-child,.empty-state p{margin:7px 0 0;color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.list-toolbar{display:flex;align-items:center;justify-content:space-between;gap:16px;padding-bottom:12px;border-bottom:1px solid var(--kk-border)}.list-toolbar>span{color:var(--kk-text-muted);font-size:.8rem}
.entity-collection{display:grid;gap:12px}.entity-collection--grid{grid-template-columns:repeat(auto-fill,minmax(min(100%,330px),1fr));gap:16px}.entity-card{display:grid;grid-template-columns:auto minmax(0,1fr) auto auto;align-items:center;gap:16px;padding:17px 20px;color:inherit;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);text-decoration:none;transition:140ms ease}.entity-card:hover{border-color:var(--kk-border-strong);box-shadow:var(--kk-shadow);transform:translateY(-1px)}.entity-card__icon{display:grid;width:42px;height:42px;place-items:center;color:var(--kk-accent);background:var(--kk-accent-soft);border-radius:var(--kk-radius)}.entity-card__main>p{margin:5px 0 0;color:var(--kk-text-muted);font-size:.85rem}.entity-card__meta{display:flex;align-items:center;gap:14px;color:var(--kk-text-muted);font-size:.75rem}.status-badge{padding:4px 7px;color:var(--kk-accent);background:var(--kk-accent-soft);border-radius:999px;font-size:.72rem;font-weight:700}.status-badge--inactive{color:var(--kk-text-muted);background:var(--kk-surface-subtle)}
.entity-collection--grid .entity-card{min-height:190px;grid-template-columns:auto 1fr;align-items:start}.entity-collection--grid .status-badge{justify-self:end}.entity-collection--grid .entity-card__meta{grid-column:1/-1;align-self:end;padding-top:14px;border-top:1px solid var(--kk-border)}.entity-collection--grid .entity-card__main>p{min-height:42px}.state-panel,.empty-state{margin:0;padding:42px 24px;text-align:center;background:var(--kk-surface);border:1px dashed var(--kk-border-strong);border-radius:var(--kk-radius)}.state-panel--error{color:var(--kk-danger)}.empty-state{display:grid;place-items:center;gap:10px}
@media(max-width:760px){.entity-card{grid-template-columns:auto minmax(0,1fr) auto}.entity-card__meta{grid-column:2/-1}.page-heading{align-items:stretch;flex-direction:column}}@media(max-width:520px){.entity-card{grid-template-columns:auto 1fr}.status-badge{justify-self:end}.entity-card__meta{grid-column:1/-1;flex-wrap:wrap}}
.entity-card__title{display:flex;align-items:baseline;gap:10px}.entity-card__title>span{color:var(--kk-text-muted);font-size:.72rem;font-weight:750;letter-spacing:.06em}
</style>
