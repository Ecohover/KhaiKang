<script setup lang="ts">
withDefaults(defineProps<{ variant?: 'info' | 'warning' | 'error' }>(), { variant: 'info' })
</script>

<template>
  <div class="ui-alert" :class="`ui-alert--${variant}`" role="alert"><slot /></div>
</template>

<style scoped>
.ui-alert { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 14px; border: 1px solid; border-radius: var(--kk-radius); font-size: .82rem; }
.ui-alert--info { color: var(--kk-text); background: var(--kk-surface-subtle); border-color: var(--kk-border); }
.ui-alert--warning { color: #715514; background: #fff8df; border-color: #e9d48a; }
.ui-alert--error { color: var(--kk-danger); background: var(--kk-danger-soft); border-color: #f1c2bd; }
</style>
