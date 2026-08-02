<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'
import { RouterLink } from 'vue-router'
import { ChevronRight, ArrowLeft } from '@lucide/vue'

export interface BreadcrumbItem {
  label: string
  to?: RouteLocationRaw
  active?: boolean
}

const props = withDefaults(
  defineProps<{
    items: BreadcrumbItem[]
    showBack?: boolean
    backTo?: RouteLocationRaw
    backLabel?: string
  }>(),
  {
    showBack: false,
    backLabel: '返回列表',
  },
)
</script>

<template>
  <nav class="shared-breadcrumb" aria-label="麵包屑導覽">
    <RouterLink v-if="showBack && backTo" :to="backTo" class="back-btn">
      <ArrowLeft :size="15" />
      <span>{{ backLabel }}</span>
    </RouterLink>

    <div v-if="showBack && backTo && items.length" class="divider-line" />

    <ol class="breadcrumb-list">
      <li v-for="(item, index) in items" :key="index" class="breadcrumb-item">
        <RouterLink v-if="item.to && !item.active" :to="item.to" class="breadcrumb-link">
          {{ item.label }}
        </RouterLink>
        <span v-else class="breadcrumb-current" :aria-current="item.active ? 'page' : undefined">
          {{ item.label }}
        </span>

        <ChevronRight v-if="index < items.length - 1" :size="14" class="separator-icon" />
      </li>
    </ol>
  </nav>
</template>

<style scoped>
.shared-breadcrumb {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 2px;
  font-size: 0.85rem;
}

.back-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  color: var(--kk-text-muted);
  text-decoration: none;
  font-weight: 500;
  padding: 3px 8px;
  border-radius: 4px;
  background: transparent;
  transition: all 0.15s ease;
}
.back-btn:hover {
  color: var(--kk-accent);
  background: #f3f4f6;
}

.divider-line {
  width: 1px;
  height: 14px;
  background: var(--kk-border);
}

.breadcrumb-list {
  display: flex;
  align-items: center;
  gap: 6px;
  list-style: none;
  margin: 0;
  padding: 0;
  flex-wrap: wrap;
}

.breadcrumb-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.breadcrumb-link {
  color: var(--kk-text-muted);
  text-decoration: none;
  font-weight: 500;
  transition: color 0.15s ease;
}
.breadcrumb-link:hover {
  color: var(--kk-accent);
  text-decoration: underline;
}

.breadcrumb-current {
  color: var(--kk-text);
  font-weight: 600;
}

.separator-icon {
  color: #9ca3af;
  flex-shrink: 0;
}
</style>
