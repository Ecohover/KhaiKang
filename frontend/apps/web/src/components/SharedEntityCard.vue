<script setup lang="ts">
import type { Component } from 'vue'
import type { RouteLocationRaw } from 'vue-router'
import { ArrowRight } from '@lucide/vue'
import { RouterLink } from 'vue-router'

withDefaults(
  defineProps<{
    to: RouteLocationRaw
    icon: Component
    eyebrow: string
    title: string
    description: string
    status: string
    statusLabel: string
    viewMode?: 'list' | 'grid'
  }>(),
  {
    viewMode: 'list',
  },
)
</script>

<template>
  <RouterLink
    :to="to"
    class="shared-entity-card"
    :class="`shared-entity-card--${viewMode}`"
  >
    <div class="entity-icon">
      <component :is="icon" :size="22" aria-hidden="true" />
    </div>
    <div class="entity-main">
      <div class="entity-title">
        <span>{{ eyebrow }}</span>
        <h3>{{ title }}</h3>
      </div>
      <p>{{ description }}</p>
    </div>
    <span class="status-badge" :class="`status-badge--${status}`">
      {{ statusLabel }}
    </span>
    <div class="entity-meta">
      <slot name="meta" />
      <ArrowRight :size="17" aria-hidden="true" />
    </div>
  </RouterLink>
</template>

<style scoped>
.shared-entity-card {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 16px;
  padding: 17px 20px;
  color: inherit;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  text-decoration: none;
  transition: border-color 140ms ease, box-shadow 140ms ease, transform 140ms ease;
}

.shared-entity-card:hover {
  border-color: var(--kk-border-strong);
  box-shadow: var(--kk-shadow);
  transform: translateY(-1px);
}

.entity-icon {
  display: grid;
  width: 42px;
  height: 42px;
  place-items: center;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: var(--kk-radius);
}

.entity-title {
  display: flex;
  align-items: baseline;
  gap: 10px;
}

.entity-title > span {
  color: var(--kk-text-muted);
  font-size: 0.72rem;
  font-weight: 750;
  letter-spacing: 0.06em;
}

.entity-title h3 {
  margin: 0;
}

.entity-main > p {
  margin: 5px 0 0;
  color: var(--kk-text-muted);
  font-size: 0.85rem;
}

.entity-meta {
  display: flex;
  align-items: center;
  gap: 14px;
  color: var(--kk-text-muted);
  font-size: 0.75rem;
}

.status-badge {
  padding: 4px 7px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
}

.status-badge--inactive {
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
}

.shared-entity-card--grid {
  min-height: 190px;
  grid-template-columns: auto 1fr;
  align-items: start;
}

.shared-entity-card--grid .status-badge {
  justify-self: end;
}

.shared-entity-card--grid .entity-meta {
  grid-column: 1 / -1;
  align-self: end;
  padding-top: 14px;
  border-top: 1px solid var(--kk-border);
}

.shared-entity-card--grid .entity-main > p {
  min-height: 42px;
}

@media (max-width: 760px) {
  .shared-entity-card {
    grid-template-columns: auto minmax(0, 1fr) auto;
  }

  .entity-meta {
    grid-column: 2 / -1;
  }
}

@media (max-width: 520px) {
  .shared-entity-card {
    grid-template-columns: auto 1fr;
  }

  .status-badge {
    justify-self: end;
  }

  .entity-meta {
    grid-column: 1 / -1;
    flex-wrap: wrap;
  }
}
</style>
