<script setup lang="ts">
import type { Component } from 'vue'
import { AlertCircle, FolderOpen, RefreshCw, Loader2 } from '@lucide/vue'

const props = withDefaults(
  defineProps<{
    type?: 'loading' | 'empty' | 'error'
    title?: string
    description?: string
    icon?: Component
    showReload?: boolean
    reloadLabel?: string
  }>(),
  {
    type: 'empty',
    title: '',
    description: '',
    showReload: false,
    reloadLabel: '重新載入',
  },
)

const emit = defineEmits<{ (e: 'reload'): void }>()
</script>

<template>
  <div class="shared-state-banner" :class="type">
    <div class="state-icon-wrap">
      <component :is="icon" v-if="icon" :size="28" />
      <Loader2 v-else-if="type === 'loading'" :size="28" class="spin-icon" />
      <AlertCircle v-else-if="type === 'error'" :size="28" />
      <FolderOpen v-else :size="28" />
    </div>

    <div class="state-text-group">
      <h4 v-if="title">{{ title }}</h4>
      <p v-if="description">{{ description }}</p>
    </div>

    <button
      v-if="type === 'error' || showReload"
      type="button"
      class="btn-subtle"
      @click="emit('reload')"
    >
      <RefreshCw :size="14" /> {{ reloadLabel }}
    </button>
  </div>
</template>

<style scoped>
.shared-state-banner {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 36px 20px;
  background: #ffffff;
  border: 1px solid var(--kk-border);
  border-radius: 10px;
  text-align: center;
  gap: 12px;
  width: 100%;
  box-sizing: border-box;
}

.shared-state-banner.loading {
  background: #f9fafb;
  color: var(--kk-text-muted);
}

.shared-state-banner.error {
  background: #fef2f2;
  border-color: #fecaca;
  color: #dc2626;
}

.state-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #f3f4f6;
  color: #6b7280;
}

.shared-state-banner.error .state-icon-wrap {
  background: #fee2e2;
  color: #dc2626;
}

.shared-state-banner.loading .state-icon-wrap {
  background: #e0f2fe;
  color: #0284c7;
}

.spin-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.state-text-group h4 {
  margin: 0 0 4px;
  font-size: 1rem;
  font-weight: 700;
  color: var(--kk-text);
}

.shared-state-banner.error .state-text-group h4 {
  color: #dc2626;
}

.state-text-group p {
  margin: 0;
  font-size: 0.85rem;
  color: var(--kk-text-muted);
}

.btn-subtle {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  height: 32px;
  padding: 0 12px;
  font-size: 0.85rem;
  font-weight: 500;
  border: 1px solid var(--kk-border);
  background: #ffffff;
  border-radius: 6px;
  cursor: pointer;
  margin-top: 4px;
}
.btn-subtle:hover {
  background: #f3f4f6;
}
</style>
