<script setup lang="ts">
import type { Component } from 'vue'

defineProps<{ title: string; description?: string; icon?: Component }>()
</script>

<template>
  <div class="ui-empty-state">
    <component :is="icon" v-if="icon" :size="28" aria-hidden="true" />
    <strong>{{ title }}</strong>
    <span v-if="description">{{ description }}</span>
    <slot />
  </div>
</template>

<style scoped>
.ui-empty-state { display: grid; min-height: 220px; place-content: center; justify-items: center; gap: 8px; color: var(--kk-text-muted); text-align: center; }
.ui-empty-state strong { color: var(--kk-text); }
.ui-empty-state span { font-size: .82rem; }
</style>
