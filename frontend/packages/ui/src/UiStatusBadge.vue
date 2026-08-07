<script setup lang="ts">
withDefaults(defineProps<{ variant?: 'neutral' | 'success' | 'warning' | 'danger' | 'inactive' }>(), { variant: 'neutral' })
</script>

<template><span class="ui-status-badge" :class="`ui-status-badge--${variant}`"><slot /></span></template>

<style scoped>
.ui-status-badge { display: inline-flex; min-height: 28px; align-items: center; padding: 3px 9px; border-radius: 999px; background: var(--kk-surface-subtle); color: var(--kk-text-muted); font-size: .75rem; font-weight: 700; }
.ui-status-badge--success { color: var(--kk-accent); background: var(--kk-accent-soft); }
.ui-status-badge--warning { color: #9a6700; background: #fff8df; }
.ui-status-badge--danger { color: var(--kk-danger); background: var(--kk-danger-soft); }
.ui-status-badge--inactive { color: var(--kk-text-muted); background: #edf0ee; }
</style>
