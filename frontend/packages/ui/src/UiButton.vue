<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'button' | 'submit'
    variant?: 'primary' | 'secondary' | 'ghost'
    loading?: boolean
    disabled?: boolean
  }>(),
  {
    type: 'button',
    variant: 'primary',
    loading: false,
    disabled: false,
  },
)
</script>

<template>
  <button
    class="ui-button"
    :class="`ui-button--${variant}`"
    :type="type"
    :disabled="disabled || loading"
    :aria-busy="loading"
  >
    <span class="ui-button__content" :class="{ 'ui-button__content--loading': loading }">
      <slot />
    </span>
    <span v-if="loading" class="ui-button__spinner" aria-hidden="true" />
  </button>
</template>

<style scoped>
.ui-button {
  position: relative;
  display: inline-flex;
  min-height: 42px;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 9px 16px;
  border: 1px solid transparent;
  border-radius: var(--kk-radius);
  cursor: pointer;
  font-weight: 650;
  letter-spacing: 0;
  transition: background 140ms ease, border-color 140ms ease, color 140ms ease;
}

.ui-button:disabled {
  cursor: not-allowed;
  opacity: 0.62;
}

.ui-button--primary {
  color: white;
  background: var(--kk-accent);
}

.ui-button--primary:hover:not(:disabled) {
  background: var(--kk-accent-hover);
}

.ui-button--secondary {
  color: var(--kk-text);
  background: var(--kk-surface);
  border-color: var(--kk-border-strong);
}

.ui-button--secondary:hover:not(:disabled),
.ui-button--ghost:hover:not(:disabled) {
  background: var(--kk-surface-subtle);
}

.ui-button--ghost {
  color: var(--kk-text-muted);
  background: transparent;
}

.ui-button__content--loading {
  visibility: hidden;
}

.ui-button__spinner {
  position: absolute;
  width: 18px;
  height: 18px;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 700ms linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
