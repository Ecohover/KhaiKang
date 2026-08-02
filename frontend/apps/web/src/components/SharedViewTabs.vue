<script setup lang="ts">
import type { Component } from 'vue'

export interface ViewTabItem {
  key: string
  label: string
  icon?: Component
}

const props = defineProps<{
  tabs: ViewTabItem[]
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()
</script>

<template>
  <div class="shared-view-tabs" role="tablist">
    <button
      v-for="tab in tabs"
      :key="tab.key"
      type="button"
      role="tab"
      :aria-selected="modelValue === tab.key"
      class="tab-item"
      :class="{ 'is-active': modelValue === tab.key }"
      @click="emit('update:modelValue', tab.key)"
    >
      <component :is="tab.icon" v-if="tab.icon" :size="15" class="tab-icon" />
      <span>{{ tab.label }}</span>
    </button>
  </div>
</template>

<style scoped>
.shared-view-tabs {
  display: flex;
  align-items: center;
  gap: 6px;
  border-bottom: 2px solid var(--kk-border);
  width: 100%;
  box-sizing: border-box;
}

.tab-item {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 8px 16px;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--kk-text-muted);
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  margin-bottom: -2px;
  transition: all 0.15s ease;
  border-top-left-radius: 6px;
  border-top-right-radius: 6px;
}

.tab-item:hover {
  color: var(--kk-text);
  background: #f3f4f6;
}

.tab-item.is-active {
  color: var(--kk-accent);
  border-bottom-color: var(--kk-accent);
  background: #ffffff;
  font-weight: 700;
}

.tab-icon {
  flex-shrink: 0;
}
</style>
