<script setup lang="ts">
withDefaults(
  defineProps<{
    modelValue: string | number
    disabled?: boolean
  }>(),
  {
    disabled: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>

<template>
  <select
    class="ui-select"
    :value="modelValue"
    :disabled="disabled"
    @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
  >
    <slot />
  </select>
</template>

<style scoped>
.ui-select {
  min-height: 32px;
  padding: 5px 28px 5px 10px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
  font-size: .85rem;
}

.ui-select:disabled {
  cursor: not-allowed;
  opacity: .62;
}
</style>
