<script setup lang="ts">
const model = defineModel<string>({ required: true })

withDefaults(defineProps<{ disabled?: boolean }>(), { disabled: false })
</script>

<template>
  <input v-model="model" class="ui-input" :disabled="disabled" />
</template>

<style scoped>
.ui-input {
  width: 100%;
  min-height: 32px;
  padding: 5px 10px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
}

.ui-input:disabled { cursor: not-allowed; opacity: .62; }
</style>
