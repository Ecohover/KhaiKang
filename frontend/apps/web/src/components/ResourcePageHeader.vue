<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

const props = withDefaults(
  defineProps<{
    meta?: string
    title: string
    subtitle?: string
    status?: 'active' | 'inactive' | string
    statusLabel?: string
  }>(),
  {
    meta: '',
    subtitle: '',
    status: '',
    statusLabel: '',
  },
)

const isStatusActive = computed(() => props.status === 'active')
const { t } = useI18n()
</script>

<template>
  <header class="resource-page-header">
    <div class="header-content">
      <p v-if="meta" class="header-meta">{{ meta }}</p>
      <h2 class="header-title">{{ title }}</h2>
      <span v-if="subtitle" class="header-subtitle">{{ subtitle }}</span>
    </div>
    <div class="header-right">
      <slot />
      <span v-if="status" class="header-status-badge" :class="[status, { active: isStatusActive, inactive: !isStatusActive }]">
        {{ statusLabel || t(isStatusActive ? 'common.status.active' : 'common.status.inactive') }}
      </span>
    </div>
  </header>
</template>

<style scoped>
.resource-page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
  margin: 0;
}

.header-content {
  display: flex;
  min-width: 0;
  flex-direction: column;
}

.header-meta {
  margin: 0 0 4px;
  font-size: 0.76rem;
  font-weight: 800;
  color: var(--kk-accent);
  letter-spacing: 0.05em;
  text-transform: uppercase;
  line-height: 1;
}

.header-title {
  margin: 0 0 6px;
  font-size: 1.75rem;
  font-weight: 800;
  color: var(--kk-text);
  line-height: 1.2;
}

.header-subtitle {
  font-size: 0.88rem;
  color: var(--kk-text-muted);
  line-height: 1.4;
}

.header-right {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  flex-wrap: wrap;
  gap: 12px;
  padding-top: 4px;
}

.header-status-badge {
  display: inline-block;
  padding: 5px 12px;
  border-radius: 99px;
  font-size: 0.78rem;
  font-weight: 700;
  white-space: nowrap;
}

.header-status-badge.active {
  background: #e5f5ec;
  color: #237047;
}

.header-status-badge.inactive {
  background: #f1f3f4;
  color: #5f6368;
}

@media (max-width: 760px) {
  .resource-page-header {
    align-items: stretch;
    flex-direction: column;
  }

  .header-right {
    width: 100%;
    justify-content: flex-start;
    padding-top: 0;
  }
}
</style>
