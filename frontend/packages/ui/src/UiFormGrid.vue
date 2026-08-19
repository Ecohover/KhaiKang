<script setup lang="ts">
withDefaults(defineProps<{
  columns?: 1 | 2 | 3
}>(), {
  columns: 2,
})
</script>

<template>
  <div class="ui-form-grid" :class="`ui-form-grid--${columns}`">
    <slot />
  </div>
</template>

<style scoped>
.ui-form-grid { display: grid; gap: 14px; }
.ui-form-grid--1 { grid-template-columns: minmax(0, 1fr); }
.ui-form-grid--2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.ui-form-grid--3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.ui-form-grid :slotted(.ui-form-grid__full) { grid-column: 1 / -1; }
@media (max-width: 720px) { .ui-form-grid--2, .ui-form-grid--3 { grid-template-columns: minmax(0, 1fr); } .ui-form-grid :slotted(.ui-form-grid__full) { grid-column: auto; } }
</style>
