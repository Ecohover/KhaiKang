<script setup lang="ts">
withDefaults(defineProps<{
  interactive?: boolean
}>(), {
  interactive: false,
})
</script>

<template>
  <table class="ui-table" :class="{ 'ui-table--interactive': interactive }">
    <slot />
  </table>
</template>

<style scoped>
.ui-table{width:100%;border-collapse:collapse}
.ui-table :deep(th),.ui-table :deep(td){padding:12px 16px;text-align:left;border-bottom:1px solid var(--kk-border)}
.ui-table :deep(th){color:var(--kk-text-muted);background:var(--kk-surface-subtle);font-size:.76rem}
.ui-table--interactive :deep(tbody tr){cursor:pointer}
.ui-table--interactive :deep(tbody tr:hover){background:var(--kk-accent-soft)}
.ui-table :deep(td small){display:block;margin-top:4px;color:var(--kk-text-muted)}
.ui-table :deep(code){color:var(--kk-accent);font-weight:700}
</style>
