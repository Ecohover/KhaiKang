<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  open: boolean
  title: string
  description?: string
  closeLabel?: string
  closeOnBackdrop?: boolean
}>(), {
  description: '',
  closeLabel: 'Close',
  closeOnBackdrop: true,
})

const emit = defineEmits<{ close: [] }>()
const dialog = ref<HTMLElement>()

watch(() => props.open, async (open) => {
  if (open) {
    await nextTick()
    dialog.value?.focus()
  }
})

function backdropClose(): void {
  if (props.closeOnBackdrop) emit('close')
}
</script>

<template>
  <Teleport to="body">
    <Transition name="ui-action-dialog">
      <div v-if="open" class="ui-action-dialog__backdrop" @click.self="backdropClose">
        <section
          ref="dialog"
          class="ui-action-dialog"
          role="dialog"
          aria-modal="true"
          aria-labelledby="ui-action-dialog-title"
          tabindex="-1"
          @keydown.esc="$emit('close')"
        >
          <header class="ui-action-dialog__heading">
            <slot name="icon"><span class="ui-action-dialog__icon" aria-hidden="true">✓</span></slot>
            <div>
              <h3 id="ui-action-dialog-title">{{ title }}</h3>
              <p v-if="description">{{ description }}</p>
            </div>
            <button type="button" :aria-label="closeLabel" @click="$emit('close')">×</button>
          </header>
          <div v-if="$slots.default" class="ui-action-dialog__content"><slot /></div>
          <footer class="ui-action-dialog__actions">
            <slot name="actions">
              <button type="button" class="ui-action-dialog__primary" @click="$emit('close')">{{ closeLabel }}</button>
            </slot>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.ui-action-dialog__backdrop{position:fixed;z-index:1200;display:grid;padding:24px;background:rgb(15 24 19/48%);inset:0;place-items:center}.ui-action-dialog{display:grid;width:min(460px,100%);gap:20px;padding:24px;color:var(--kk-text);background:var(--kk-surface);border:1px solid color-mix(in srgb,var(--kk-accent) 38%,var(--kk-border));border-radius:var(--kk-radius);box-shadow:0 24px 70px rgb(12 28 19/30%)}.ui-action-dialog:focus{outline:3px solid color-mix(in srgb,var(--kk-accent) 28%,transparent);outline-offset:3px}.ui-action-dialog__heading{display:grid;grid-template-columns:auto minmax(0,1fr) auto;align-items:start;gap:12px}.ui-action-dialog__heading h3,.ui-action-dialog__heading p{margin:0}.ui-action-dialog__heading p{margin-top:5px;color:var(--kk-text-muted);font-size:.875rem}.ui-action-dialog__icon{display:grid;width:32px;height:32px;color:white;background:var(--kk-accent);border-radius:50%;font-weight:800;place-items:center}.ui-action-dialog__heading>button{width:32px;height:32px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;border-radius:var(--kk-radius);cursor:pointer;font-size:1.35rem}.ui-action-dialog__content{padding:14px;background:var(--kk-surface-subtle);border-radius:var(--kk-radius)}.ui-action-dialog__actions{display:flex;justify-content:flex-end;gap:10px}.ui-action-dialog__primary{min-height:38px;padding:7px 13px;color:white;background:var(--kk-accent);border:1px solid var(--kk-accent);border-radius:var(--kk-radius);cursor:pointer;font-weight:650}.ui-action-dialog-enter-active,.ui-action-dialog-leave-active{transition:opacity 160ms ease}.ui-action-dialog-enter-from,.ui-action-dialog-leave-to{opacity:0}
</style>
