<script setup lang="ts">
import { Search, X } from '@lucide/vue'

withDefaults(
  defineProps<{
    modelValue: string
    placeholder: string
    ariaLabel?: string
    clearLabel?: string
    disabled?: boolean
  }>(),
  {
    ariaLabel: '',
    clearLabel: 'Clear search',
    disabled: false,
  },
)

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>

<template>
  <label class="shared-search-field">
    <span class="sr-only">{{ ariaLabel || placeholder }}</span>
    <Search :size="16" aria-hidden="true" />
    <input
      :value="modelValue"
      type="search"
      :placeholder="placeholder"
      :disabled="disabled"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <button
      v-if="modelValue"
      type="button"
      :aria-label="clearLabel"
      :title="clearLabel"
      :disabled="disabled"
      @click="emit('update:modelValue', '')"
    >
      <X :size="14" aria-hidden="true" />
    </button>
  </label>
</template>

<style scoped>
.shared-search-field {
  display: flex;
  width: 100%;
  max-width: 320px;
  min-height: 42px;
  flex: 1 1 240px;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  transition: border-color 140ms ease, box-shadow 140ms ease;
}

.shared-search-field:focus-within {
  border-color: var(--kk-focus);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--kk-focus) 16%, transparent);
}

.shared-search-field input {
  width: 100%;
  min-width: 0;
  padding: 0;
  color: var(--kk-text);
  background: transparent;
  border: 0;
  outline: 0;
  font: inherit;
  font-size: 0.85rem;
}

.shared-search-field input::-webkit-search-cancel-button {
  display: none;
}

.shared-search-field button {
  display: grid;
  flex: 0 0 auto;
  padding: 4px;
  place-items: center;
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
  border-radius: 4px;
  cursor: pointer;
}

.shared-search-field button:hover:not(:disabled) {
  color: var(--kk-text);
  background: var(--kk-surface);
}

.shared-search-field button:disabled,
.shared-search-field input:disabled {
  cursor: not-allowed;
  opacity: 0.62;
}

.sr-only {
  position: absolute;
  overflow: hidden;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  white-space: nowrap;
  border: 0;
  clip: rect(0, 0, 0, 0);
}

@media (max-width: 640px) {
  .shared-search-field {
    max-width: none;
    flex: 0 0 auto;
  }
}
</style>
