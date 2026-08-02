<script setup lang="ts">
const model = defineModel<string>({ required: true })

withDefaults(
  defineProps<{
    id: string
    label: string
    type?: 'text' | 'password'
    autocomplete?: string
    error?: string
    disabled?: boolean
  }>(),
  {
    type: 'text',
    autocomplete: 'off',
    disabled: false,
  },
)
</script>

<template>
  <label class="ui-field" :for="id">
    <span class="ui-field__label">{{ label }}</span>
    <input
      :id="id"
      v-model="model"
      class="ui-field__input"
      :class="{ 'ui-field__input--error': error }"
      :type="type"
      :autocomplete="autocomplete"
      :disabled="disabled"
      :aria-invalid="Boolean(error)"
      :aria-describedby="error ? `${id}-error` : undefined"
    />
    <span v-if="error" :id="`${id}-error`" class="ui-field__error">{{ error }}</span>
  </label>
</template>

<style scoped>
.ui-field {
  display: grid;
  gap: 7px;
}

.ui-field__label {
  color: var(--kk-text);
  font-size: 0.875rem;
  font-weight: 650;
}

.ui-field__input {
  width: 100%;
  min-height: 44px;
  padding: 9px 11px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.ui-field__input:hover:not(:disabled) {
  border-color: #87928a;
}

.ui-field__input--error {
  border-color: var(--kk-danger);
}

.ui-field__error {
  color: var(--kk-danger);
  font-size: 0.8125rem;
}
</style>
