<script setup lang="ts">
import type { Component } from 'vue'

const props = withDefaults(
  defineProps<{
    title?: string
    description?: string
    icon?: Component
  }>(),
  {
    title: '',
    description: '',
  },
)
</script>

<template>
  <div class="shared-card-section">
    <header v-if="title || description || $slots.headerRight || icon" class="card-header">
      <div class="header-left">
        <component :is="icon" v-if="icon" :size="20" class="header-icon" />
        <div>
          <h3 v-if="title">{{ title }}</h3>
          <p v-if="description">{{ description }}</p>
        </div>
      </div>
      <div v-if="$slots.headerRight" class="header-right">
        <slot name="headerRight" />
      </div>
    </header>

    <div class="card-body">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.shared-card-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #ffffff;
  border: 1px solid var(--kk-border);
  border-radius: 10px;
  padding: 20px 24px;
  width: 100%;
  box-sizing: border-box;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  border-bottom: 1px solid var(--kk-border);
  padding-bottom: 14px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.header-icon {
  color: var(--kk-accent);
}

.card-header h3 {
  margin: 0;
  font-size: 1.08rem;
  font-weight: 700;
  color: var(--kk-text);
}

.card-header p {
  margin: 2px 0 0;
  font-size: 0.84rem;
  color: var(--kk-text-muted);
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.card-body {
  display: flex;
  flex-direction: column;
  width: 100%;
}
</style>
