<script setup lang="ts">
import type { Component } from 'vue'
import { UiViewModeToggle } from '@khaikang/ui'
import ResourcePageHeader from './ResourcePageHeader.vue'
import SharedStateBanner from './SharedStateBanner.vue'

defineProps<{
  meta: string
  title: string
  description: string
  countLabel: string
  viewMode: 'list' | 'grid'
  storageKey: string
  groupLabel: string
  listLabel: string
  gridLabel: string
  loading: boolean
  loadingLabel: string
  error: string
  errorTitle: string
  reloadLabel: string
  hasItems: boolean
  emptyTitle: string
  emptyDescription: string
  emptyIcon: Component
}>()

const emit = defineEmits<{
  'update:viewMode': [value: 'list' | 'grid']
  reload: []
}>()
</script>

<template>
  <section class="shared-entity-list-page">
    <ResourcePageHeader :meta="meta" :title="title" :subtitle="description">
      <slot name="action" />
    </ResourcePageHeader>

    <div class="list-toolbar">
      <span>{{ countLabel }}</span>
      <UiViewModeToggle
        :model-value="viewMode"
        :storage-key="storageKey"
        :group-label="groupLabel"
        :list-label="listLabel"
        :grid-label="gridLabel"
        @update:model-value="emit('update:viewMode', $event)"
      />
    </div>

    <SharedStateBanner
      v-if="loading"
      type="loading"
      :title="loadingLabel"
    />
    <SharedStateBanner
      v-else-if="error"
      type="error"
      :title="errorTitle"
      :description="error"
      :reload-label="reloadLabel"
      @reload="emit('reload')"
    />
    <div
      v-else-if="hasItems"
      class="entity-collection"
      :class="`entity-collection--${viewMode}`"
    >
      <slot />
    </div>
    <SharedStateBanner
      v-else
      type="empty"
      :icon="emptyIcon"
      :title="emptyTitle"
      :description="emptyDescription"
    />
  </section>
</template>

<style scoped>
.shared-entity-list-page {
  display: grid;
  gap: 24px;
  width: 100%;
}

.list-toolbar {
  display: flex;
  min-height: 42px;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--kk-border);
}

.list-toolbar > span {
  color: var(--kk-text-muted);
  font-size: 0.8rem;
}

.entity-collection {
  display: grid;
  gap: 12px;
}

.entity-collection--grid {
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 330px), 1fr));
  gap: 16px;
}

@media (max-width: 520px) {
  .list-toolbar {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
