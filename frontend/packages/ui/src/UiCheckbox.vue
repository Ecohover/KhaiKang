<script setup lang="ts">
const model = defineModel<boolean>({ required: true })

defineProps<{
  id: string
  label: string
  disabled?: boolean
}>()
</script>

<template>
  <label class="ui-checkbox" :for="id">
    <input :id="id" v-model="model" type="checkbox" :disabled="disabled" />
    <span>{{ label }}</span>
  </label>
</template>

<style scoped>
.ui-checkbox {
  display: inline-flex;
  align-items: center;
  gap: 9px;
  color: var(--kk-text-muted);
  cursor: pointer;
  font-size: 0.875rem;
}

.ui-checkbox input {
  width: 17px;
  height: 17px;
  margin: 0;
  accent-color: var(--kk-accent);
}
</style>
