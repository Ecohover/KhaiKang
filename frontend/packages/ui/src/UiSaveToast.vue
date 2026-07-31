<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

const props = withDefaults(
  defineProps<{
    mode: 'created' | 'updated'
    recordLabel: string
    recordKey: string
    inline?: boolean
    autoClose?: boolean
    autoCloseDelay?: number
    allowContinue?: boolean
    continueLabel?: string
    finishLabel?: string
  }>(),
  {
    inline: false,
    autoClose: true,
    autoCloseDelay: 5000,
    allowContinue: false,
    continueLabel: '繼續新增',
    finishLabel: '結束',
  },
)

const emit = defineEmits<{
  continue: []
  finish: []
  close: []
}>()

const visible = ref(true)
let closeTimer: ReturnType<typeof window.setTimeout> | undefined

onMounted(() => {
  if (props.autoClose && props.autoCloseDelay > 0) {
    closeTimer = window.setTimeout(close, props.autoCloseDelay)
  }
})

onBeforeUnmount(clearCloseTimer)

function clearCloseTimer(): void {
  if (closeTimer !== undefined) {
    window.clearTimeout(closeTimer)
    closeTimer = undefined
  }
}

function close(): void {
  clearCloseTimer()
  if (props.inline) {
    emit('close')
    return
  }

  visible.value = false
}

function notifyClosed(): void {
  emit('close')
}
</script>

<template>
  <Teleport to="body" :disabled="inline">
    <Transition
      name="ui-save-toast"
      :css="!inline"
      :appear="!inline"
      @after-leave="notifyClosed"
    >
      <aside
        v-if="visible"
        class="ui-save-toast"
        :class="{ 'ui-save-toast--inline': inline }"
        role="status"
        aria-live="polite"
        aria-atomic="true"
      >
        <div class="ui-save-toast__heading">
          <span class="ui-save-toast__icon" aria-hidden="true">✓</span>
          <div>
            <strong>{{ mode === 'created' ? '新增成功' : '修改成功' }}</strong>
            <p>{{ recordLabel }}：<code>{{ recordKey }}</code></p>
          </div>
          <button type="button" aria-label="關閉成功提示" @click="close">×</button>
        </div>

        <div v-if="$slots.default" class="ui-save-toast__content">
          <slot />
        </div>

        <div v-if="allowContinue" class="ui-save-toast__actions">
          <button type="button" class="ui-save-toast__secondary" @click="$emit('finish')">
            {{ finishLabel }}
          </button>
          <button type="button" class="ui-save-toast__primary" @click="$emit('continue')">
            {{ continueLabel }}
          </button>
        </div>
      </aside>
    </Transition>
  </Teleport>
</template>

<style scoped>
.ui-save-toast-enter-active,
.ui-save-toast-leave-active {
  transition:
    opacity 220ms ease,
    transform 260ms cubic-bezier(.2, .8, .2, 1);
}

.ui-save-toast-enter-from {
  opacity: 0;
  transform: translateY(-22px);
}

.ui-save-toast-leave-to {
  opacity: 0;
  transform: translateY(14px);
}

.ui-save-toast {
  position: fixed;
  right: max(20px, env(safe-area-inset-right));
  bottom: max(20px, env(safe-area-inset-bottom));
  z-index: 1000;
  display: grid;
  width: min(390px, calc(100vw - 32px));
  gap: 16px;
  padding: 18px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid color-mix(in srgb, var(--kk-accent) 38%, var(--kk-border));
  border-radius: var(--kk-radius);
  box-shadow: 0 18px 50px rgb(18 39 28 / 18%);
}

.ui-save-toast--inline {
  position: relative;
  right: auto;
  bottom: auto;
  width: 100%;
}

.ui-save-toast__heading {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  align-items: start;
  gap: 11px;
}

.ui-save-toast__icon {
  display: grid;
  width: 30px;
  height: 30px;
  place-items: center;
  color: white;
  background: var(--kk-accent);
  border-radius: 50%;
  font-weight: 800;
}

.ui-save-toast__heading strong {
  display: block;
  margin-top: 2px;
}

.ui-save-toast__heading p {
  margin: 5px 0 0;
  color: var(--kk-text-muted);
  font-size: 0.875rem;
}

.ui-save-toast__heading code {
  color: var(--kk-text);
  font-size: 0.875rem;
  font-weight: 700;
}

.ui-save-toast__heading > button {
  width: 30px;
  height: 30px;
  padding: 0;
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
  border-radius: var(--kk-radius);
  cursor: pointer;
  font-size: 1.3rem;
  line-height: 1;
}

.ui-save-toast__heading > button:hover {
  background: var(--kk-surface-subtle);
}

.ui-save-toast__content {
  padding: 12px;
  background: var(--kk-surface-subtle);
  border-radius: var(--kk-radius);
}

.ui-save-toast__actions {
  display: flex;
  justify-content: flex-end;
  gap: 9px;
}

.ui-save-toast__actions button {
  min-height: 38px;
  padding: 7px 13px;
  border-radius: var(--kk-radius);
  cursor: pointer;
  font-weight: 650;
}

.ui-save-toast__secondary {
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
}

.ui-save-toast__primary {
  color: white;
  background: var(--kk-accent);
  border: 1px solid var(--kk-accent);
}

@media (prefers-reduced-motion: reduce) {
  .ui-save-toast-enter-active,
  .ui-save-toast-leave-active {
    transition-duration: 1ms;
  }
}

@media (max-width: 520px) {
  .ui-save-toast {
    right: 16px;
    bottom: 16px;
    left: 16px;
    width: auto;
  }

  .ui-save-toast.ui-save-toast--inline {
    right: auto;
    bottom: auto;
    left: auto;
    width: 100%;
  }

  .ui-save-toast__actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }
}
</style>
