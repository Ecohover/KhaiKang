<script setup lang="ts">
const model = defineModel<string>({ required: true })

withDefaults(defineProps<{ disabled?: boolean }>(), { disabled: false })
</script>

<template>
  <textarea v-model="model" class="ui-textarea" :disabled="disabled" />
</template>

<style scoped>
.ui-textarea {
  width: 100%;
  min-height: 80px;
  padding: 9px 10px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
  resize: vertical;
}

.ui-textarea:disabled { cursor: not-allowed; opacity: .62; }
</style>
