<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { Download, FileText, X } from '@lucide/vue'

const props = withDefaults(defineProps<{
  open: boolean
  href: string
  fileName: string
  labels?: Record<string, string>
}>(), {
  labels: () => ({}),
})

const emit = defineEmits<{ close: [] }>()
const closeButton = ref<HTMLButtonElement>()
const downloadLink = ref<HTMLAnchorElement>()
let previousFocus: HTMLElement | null = null

function handleKeydown(event: KeyboardEvent): void {
  if (!props.open) return
  if (event.key === 'Escape') {
    emit('close')
    return
  }
  if (event.key !== 'Tab') return
  const focusable = [closeButton.value, downloadLink.value]
    .filter((element): element is HTMLButtonElement | HTMLAnchorElement => element !== undefined)
  if (!focusable.length) return
  const currentIndex = focusable.indexOf(document.activeElement as HTMLButtonElement | HTMLAnchorElement)
  const nextIndex = event.shiftKey
    ? (currentIndex <= 0 ? focusable.length - 1 : currentIndex - 1)
    : (currentIndex === focusable.length - 1 ? 0 : currentIndex + 1)
  event.preventDefault()
  focusable[nextIndex]?.focus()
}

watch(() => props.open, async (open) => {
  if (open) {
    previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
    await nextTick()
    closeButton.value?.focus()
    return
  }
  previousFocus?.focus()
  previousFocus = null
})

onMounted(() => window.addEventListener('keydown', handleKeydown))
onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  previousFocus?.focus()
})
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="ui-attachment-dialog" role="dialog" aria-modal="true" :aria-label="labels.attachmentDialog ?? 'Attachment'" @mousedown.self="$emit('close')">
      <section class="ui-attachment-dialog__panel">
        <header>
          <div><FileText :size="20" aria-hidden="true" /><strong>{{ labels.attachmentDialog ?? 'Attachment' }}</strong></div>
          <button ref="closeButton" type="button" :aria-label="labels.close ?? 'Close'" @click="$emit('close')"><X :size="19" /></button>
        </header>
        <p>{{ fileName }}</p>
        <a ref="downloadLink" :href="href" download>
          <Download :size="17" aria-hidden="true" />
          {{ labels.downloadAttachment ?? 'Download file' }}
        </a>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.ui-attachment-dialog{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:20px;background:rgb(18 24 21 / .55);backdrop-filter:blur(2px)}.ui-attachment-dialog__panel{display:grid;width:min(440px,100%);gap:18px;padding:18px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:10px;box-shadow:0 18px 60px rgb(0 0 0 / .28)}.ui-attachment-dialog header{display:flex;align-items:center;justify-content:space-between;gap:12px}.ui-attachment-dialog header>div{display:flex;align-items:center;gap:8px}.ui-attachment-dialog header button{display:grid;place-items:center;width:32px;height:32px;padding:0;border:0;border-radius:5px;background:transparent;color:var(--kk-text-muted);cursor:pointer}.ui-attachment-dialog header button:hover{background:var(--kk-surface-subtle)}.ui-attachment-dialog p{overflow-wrap:anywhere;margin:0;padding:12px;background:var(--kk-surface-subtle);border-radius:6px;color:var(--kk-text)}.ui-attachment-dialog a{display:inline-flex;width:fit-content;align-items:center;gap:7px;padding:8px 12px;color:#fff;background:var(--kk-accent);border-radius:6px;font-size:.85rem;font-weight:700;text-decoration:none}
</style>
