<script setup lang="ts">
withDefaults(defineProps<{
  mode?: 'floating' | 'fixed'
  message?: string
  messageTone?: 'neutral' | 'warning' | 'danger' | 'success'
}>(), {
  mode: 'floating',
  message: '',
  messageTone: 'neutral',
})
</script>

<template>
  <footer class="ui-form-action-bar" :class="`ui-form-action-bar--${mode}`">
    <div class="ui-form-action-bar__content">
      <div v-if="message || $slots.status" class="ui-form-action-bar__status">
        <p v-if="message" class="ui-form-action-bar__message" :class="`ui-form-action-bar__message--${messageTone}`">{{ message }}</p>
        <slot v-else name="status" />
      </div>
      <div class="ui-form-action-bar__actions"><slot /></div>
    </div>
  </footer>
</template>

<style scoped>
.ui-form-action-bar {
  z-index: 10;
  padding: 14px 20px;
  background: rgba(255, 255, 255, .95);
  backdrop-filter: blur(10px);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  box-shadow: 0 -4px 16px rgba(0, 0, 0, .08);
}
.ui-form-action-bar--floating { position: sticky; bottom: 0; margin-top: 12px; }
.ui-form-action-bar--fixed { position: relative; flex: 0 0 auto; margin-top: 0; border-radius: 0; border-right: 0; border-bottom: 0; border-left: 0; }
.ui-form-action-bar__content { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.ui-form-action-bar__actions { display: flex; gap: 8px; justify-content: flex-end; margin-left: auto; }
.ui-form-action-bar__message { margin: 0; font-size: .84rem; font-weight: 600; }
.ui-form-action-bar__message--neutral { color: var(--kk-text-muted); }
.ui-form-action-bar__message--warning { color: #c05621; }
.ui-form-action-bar__message--danger { color: var(--kk-danger); }
.ui-form-action-bar__message--success { color: var(--kk-accent); }
@media (max-width: 720px) { .ui-form-action-bar__content { align-items: stretch; flex-direction: column; } .ui-form-action-bar__actions { margin-left: 0; } }
</style>
