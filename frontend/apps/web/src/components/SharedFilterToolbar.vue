<script setup lang="ts">
withDefaults(defineProps<{ align?: 'start' | 'end' }>(), { align: 'end' })
</script>

<template>
  <div class="shared-filter-toolbar" :class="`shared-filter-toolbar--${align}`">
    <slot />
  </div>
</template>

<style scoped>
.shared-filter-toolbar {
  display: flex;
  min-height: 42px;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
}
.shared-filter-toolbar--end { justify-content: flex-end; }
.shared-filter-toolbar--start { justify-content: flex-start; }
.shared-filter-toolbar :deep(.ui-select) {
  min-height: 32px;
}
@media (max-width: 640px) {
  .shared-filter-toolbar { align-items: stretch; }
  .shared-filter-toolbar :deep(.shared-search-field) { max-width: none; }
}
</style>
