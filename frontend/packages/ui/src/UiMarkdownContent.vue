<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import DOMPurify from 'dompurify'
import { marked } from 'marked'
import UiImageViewer from './UiImageViewer.vue'
import UiAttachmentDialog from './UiAttachmentDialog.vue'

const props = withDefaults(defineProps<{ content: string | null | undefined, labels?: Record<string, string> }>(), {
  labels: () => ({}),
})

const html = computed(() => DOMPurify.sanitize(marked.parse(props.content ?? '') as string))
const container = ref<HTMLElement>()
const viewerOpen = ref(false)
const viewerSource = ref('')
const viewerAlt = ref('')
const attachmentDialogOpen = ref(false)
const attachmentSource = ref('')
const attachmentName = ref('')

function sourceWithoutSize(source: string): string {
  return source.replace(/#size=\d{1,3}$/, '')
}

function applyImageWidths(): void {
  container.value?.querySelectorAll<HTMLImageElement>('img').forEach((image) => {
    const size = Number((image.getAttribute('src') ?? '').match(/#size=(\d{1,3})$/)?.[1])
    image.style.width = size >= 10 && size <= 100 ? `${size}%` : ''
  })
  container.value?.querySelectorAll<HTMLAnchorElement>('a[href]').forEach((anchor) => {
    const href = anchor.getAttribute('href') ?? ''
    if (!href.includes('/attachments/') || !href.includes('/content')) return
    anchor.dataset.attachmentHref = anchor.href
    anchor.removeAttribute('href')
    anchor.setAttribute('role', 'button')
    anchor.setAttribute('tabindex', '0')
  })
}

function openContent(event: MouseEvent): void {
  const element = event.target as HTMLElement
  const image = element.closest('img') as HTMLImageElement | null
  if (image) {
    event.preventDefault()
    viewerSource.value = sourceWithoutSize(image.getAttribute('src') ?? '')
    viewerAlt.value = image.getAttribute('alt') ?? ''
    viewerOpen.value = true
    return
  }

  const attachment = element.closest('a[data-attachment-href]') as HTMLAnchorElement | null
  if (!attachment) return
  event.preventDefault()
  event.stopImmediatePropagation()
  attachmentSource.value = attachment.dataset.attachmentHref ?? ''
  attachmentName.value = attachment.textContent?.trim() || props.labels.attachment || 'Attachment'
  attachmentDialogOpen.value = true
}

function openContentWithKeyboard(event: KeyboardEvent): void {
  if (event.key !== 'Enter' && event.key !== ' ') return
  const attachment = (event.target as HTMLElement).closest('a[data-attachment-href]') as HTMLAnchorElement | null
  if (!attachment) return
  event.preventDefault()
  event.stopImmediatePropagation()
  attachmentSource.value = attachment.dataset.attachmentHref ?? ''
  attachmentName.value = attachment.textContent?.trim() || props.labels.attachment || 'Attachment'
  attachmentDialogOpen.value = true
}

watch(html, () => void nextTick(applyImageWidths), { immediate: true })
</script>

<template>
  <div ref="container" class="ui-markdown-content" @click.capture="openContent" @keydown.capture="openContentWithKeyboard" v-html="html" />
  <UiImageViewer :open="viewerOpen" :src="viewerSource" :alt="viewerAlt" :labels="labels" @close="viewerOpen = false" />
  <UiAttachmentDialog :open="attachmentDialogOpen" :href="attachmentSource" :file-name="attachmentName" :labels="labels" @close="attachmentDialogOpen = false" />
</template>

<style scoped>
.ui-markdown-content{font-synthesis:weight style}.ui-markdown-content :deep(:first-child){margin-top:0}.ui-markdown-content :deep(:last-child){margin-bottom:0}.ui-markdown-content :deep(strong){font-weight:800}.ui-markdown-content :deep(em){font-style:italic}.ui-markdown-content :deep(img){display:block;max-width:100%;height:auto;margin:.75em 0;border:1px solid var(--kk-border);border-radius:6px;cursor:zoom-in}.ui-markdown-content :deep(pre){overflow-x:auto;padding:12px 14px;color:#25332b;background:#f2f5f3;border:1px solid var(--kk-border);border-left:3px solid var(--kk-accent);border-radius:6px;line-height:1.55;white-space:pre-wrap}.ui-markdown-content :deep(pre code){padding:0;color:inherit;background:transparent;border:0;font-family:ui-monospace,SFMono-Regular,Consolas,"Liberation Mono",monospace}.ui-markdown-content :deep(code:not(pre code)){padding:1px 4px;color:#9b3f12;background:#fff3e8;border-radius:4px;font-family:ui-monospace,SFMono-Regular,Consolas,"Liberation Mono",monospace}.ui-markdown-content :deep(a){color:var(--kk-accent)}.ui-markdown-content :deep(a[data-attachment-href]){display:flex;width:fit-content;max-width:100%;align-items:center;gap:7px;margin:.55em 0;padding:7px 10px;border:1px solid var(--kk-border);border-radius:6px;background:var(--kk-surface-subtle);font-weight:650;line-height:1.35;text-decoration:none;overflow-wrap:anywhere;cursor:pointer}.ui-markdown-content :deep(a[data-attachment-href]::before){content:"";flex:0 0 16px;width:16px;height:16px;background:currentColor;mask:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'/%3E%3C/svg%3E") center/contain no-repeat}.ui-markdown-content :deep(a[data-attachment-href]:hover),.ui-markdown-content :deep(a[data-attachment-href]:focus-visible){border-color:var(--kk-accent);background:var(--kk-accent-soft);outline:0}
</style>
