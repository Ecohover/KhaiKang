<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, shallowRef, watch } from 'vue'
import { Bold, Code2, ImagePlus, Italic, Link as LinkIcon, List, ListOrdered, Paperclip, ZoomIn } from '@lucide/vue'
import { EditorContent, useEditor } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import LinkExtension from '@tiptap/extension-link'
import ImageExtension from '@tiptap/extension-image'
import { Markdown } from '@tiptap/markdown'
import UiImageViewer from './UiImageViewer.vue'
import UiAttachmentDialog from './UiAttachmentDialog.vue'

const props = withDefaults(defineProps<{
  modelValue: string
  disabled?: boolean
  showToolbar?: boolean
  placeholder?: string
  labels?: Record<string, string>
  uploadImage?: ((file: File) => Promise<{ src: string, alt?: string }>) | undefined
  uploadAttachment?: ((file: File) => Promise<{ src: string, name?: string }>) | undefined
}>(), {
  disabled: false,
  showToolbar: true,
  placeholder: '',
  labels: () => ({}),
})

const labels = computed(() => ({
  toolbar: 'Text formatting',
  bold: 'Bold',
  italic: 'Italic',
  blockStyle: 'Paragraph style',
  blockStyleHint: 'Apply to selected lines',
  paragraph: 'Body',
  heading1: 'Large heading',
  heading2: 'Medium heading',
  heading3: 'Small heading',
  bulletList: 'Bulleted list',
  orderedList: 'Numbered list',
  codeBlock: 'Code block',
  codeBlockHint: 'Convert selected paragraphs to a code block',
  codeLanguage: 'Code language',
  plainText: 'Plain text',
  link: 'Insert link',
  linkPrompt: 'Link URL',
  image: 'Insert image',
  imageUploadFailed: 'Image upload failed.',
  attachment: 'Insert attachment',
  attachmentUploadFailed: 'Attachment upload failed.',
  attachmentDialog: 'Attachment',
  downloadAttachment: 'Download file',
  resizeImage: 'Resize image',
  imagePreview: 'Image preview',
  close: 'Close',
  zoomIn: 'Zoom in',
  zoomOut: 'Zoom out',
  fitImage: 'Fit to window',
  resetImage: 'Reset view',
  ...props.labels,
}))

const emit = defineEmits<{
  'update:modelValue': [value: string]
  blur: []
}>()

const editor = useEditor({
  extensions: [
    StarterKit,
    LinkExtension.configure({ openOnClick: false }),
    ImageExtension.configure({ allowBase64: false }),
    Markdown,
  ],
  content: props.modelValue,
  contentType: 'markdown',
  editable: !props.disabled,
  editorProps: {
    attributes: {
      class: 'ui-markdown-editor__content',
      'data-placeholder': props.placeholder,
    },
    handleDOMEvents: {
      blur: () => {
        emit('blur')
        return false
      },
    },
    handlePaste: (_view, event) => {
      const file = Array.from(event.clipboardData?.files ?? [])[0]
      if (!file || props.disabled || (!props.uploadImage && !props.uploadAttachment)) return false
      event.preventDefault()
      rememberSelection()
      void insertFile(file)
      return true
    },
    handleDrop: (_view, event, _slice, moved) => {
      const file = Array.from(event.dataTransfer?.files ?? [])[0]
      if (moved || !file || props.disabled || (!props.uploadImage && !props.uploadAttachment)) return false
      event.preventDefault()
      rememberSelection()
      void insertFile(file)
      return true
    },
  },
  onCreate: () => scheduleImageWidths(),
  onUpdate: ({ editor: value }) => {
    emit('update:modelValue', value.getMarkdown())
    scheduleImageWidths()
  },
})

watch(() => props.modelValue, (value) => {
  if (!editor.value || editor.value.getMarkdown() === value) return
  editor.value.commands.setContent(value, { contentType: 'markdown' })
  scheduleImageWidths()
})

watch(() => props.disabled, (disabled) => editor.value?.setEditable(!disabled))

let selectedRange: { from: number, to: number } | undefined
const fileInput = ref<HTMLInputElement>()
const attachmentInput = ref<HTMLInputElement>()
const uploadingImage = ref(false)
const imageError = ref('')
const selectedImage = shallowRef<HTMLImageElement | null>(null)
const resizeHandleStyle = ref<Record<string, string>>({})
const previewButtonStyle = ref<Record<string, string>>({})
const viewerOpen = ref(false)
const viewerSource = ref('')
const viewerAlt = ref('')
const attachmentDialogOpen = ref(false)
const attachmentSource = ref('')
const attachmentName = ref('')
let resizeState: { image: HTMLImageElement, startX: number, startWidth: number, containerWidth: number } | undefined

function rememberSelection(): void {
  if (!editor.value) return
  const { from, to } = editor.value.state.selection
  selectedRange = { from, to }
}

function selectedChain() {
  const chain = editor.value?.chain().focus()
  return chain && selectedRange ? chain.setTextSelection(selectedRange) : chain
}

function toggleBold(): void {
  selectedChain()?.toggleBold().run()
}

function toggleItalic(): void {
  selectedChain()?.toggleItalic().run()
}

function toggleBulletList(): void {
  selectedChain()?.toggleBulletList().run()
}

function toggleOrderedList(): void {
  selectedChain()?.toggleOrderedList().run()
}

function toggleCodeBlock(): void {
  selectedChain()?.toggleCodeBlock().run()
}

function currentCodeLanguage(): string {
  return editor.value?.getAttributes('codeBlock').language ?? ''
}

function setCodeLanguage(event: Event): void {
  const language = (event.target as HTMLSelectElement).value || null
  selectedChain()?.updateAttributes('codeBlock', { language }).run()
}

function setLink(): void {
  const href = window.prompt(labels.value.linkPrompt)?.trim()
  if (!href) return
  selectedChain()?.extendMarkRange('link').setLink({ href }).run()
}

async function insertImage(file: File): Promise<void> {
  const uploader = props.uploadImage ?? props.uploadAttachment
  if (!uploader || uploadingImage.value) return
  uploadingImage.value = true
  imageError.value = ''
  try {
    const image = await uploader(file)
    const alt = 'alt' in image
      ? image.alt
      : 'name' in image
        ? image.name
        : undefined
    selectedChain()?.setImage({ src: image.src, alt: alt ?? file.name }).run()
  } catch {
    imageError.value = labels.value.imageUploadFailed
  } finally {
    uploadingImage.value = false
    if (fileInput.value) fileInput.value.value = ''
  }
}

async function insertAttachment(file: File): Promise<void> {
  if (!props.uploadAttachment || uploadingImage.value) return
  uploadingImage.value = true
  imageError.value = ''
  try {
    const attachment = await props.uploadAttachment(file)
    selectedChain()?.insertContent({
      type: 'text',
      text: attachment.name ?? file.name,
      marks: [{ type: 'link', attrs: { href: attachment.src } }],
    }).run()
  } catch {
    imageError.value = labels.value.attachmentUploadFailed
  } finally {
    uploadingImage.value = false
    if (attachmentInput.value) attachmentInput.value.value = ''
  }
}

async function insertFile(file: File): Promise<void> {
  if (file.type.startsWith('image/')) {
    await insertImage(file)
    return
  }
  await insertAttachment(file)
}

function selectImage(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (file) void insertImage(file)
}

function selectAttachment(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (file) void insertFile(file)
}

function imageSize(source: string): number | undefined {
  const value = Number(source.match(/#size=(\d{1,3})$/)?.[1])
  return value >= 10 && value <= 100 ? value : undefined
}

function sourceWithoutSize(source: string): string {
  return source.replace(/#size=\d{1,3}$/, '')
}

function scheduleImageWidths(): void {
  void nextTick(() => {
    const root = editor.value?.view.dom
    root?.querySelectorAll<HTMLImageElement>('img').forEach((image) => {
      const size = imageSize(image.getAttribute('src') ?? '')
      image.style.width = size ? `${size}%` : ''
    })
    root?.querySelectorAll<HTMLAnchorElement>('a[href]').forEach((anchor) => {
      const href = anchor.getAttribute('href') ?? ''
      if (!isAttachmentLink(href)) return
      anchor.dataset.attachmentHref = anchor.href
      anchor.removeAttribute('href')
      anchor.setAttribute('role', 'button')
      anchor.setAttribute('tabindex', '0')
    })
  })
}

function positionResizeHandle(image: HTMLImageElement): void {
  const canvas = image.closest('.ui-markdown-editor__canvas')?.getBoundingClientRect()
  const bounds = image.getBoundingClientRect()
  if (!canvas) return
  resizeHandleStyle.value = {
    left: `${bounds.right - canvas.left - 7}px`,
    top: `${bounds.bottom - canvas.top - 7}px`,
  }
  previewButtonStyle.value = {
    left: `${bounds.right - canvas.left - 34}px`,
    top: `${bounds.top - canvas.top + 8}px`,
  }
}

function isAttachmentLink(href: string): boolean {
  return href.includes('/attachments/') && href.includes('/content')
}

function handleContentClick(event: MouseEvent): void {
  if (resizeState) return
  const element = event.target as HTMLElement
  const attachment = element.closest('a[data-attachment-href]') as HTMLAnchorElement | null
  if (attachment) {
    event.preventDefault()
    event.stopImmediatePropagation()
    selectedImage.value = null
    attachmentSource.value = attachment.dataset.attachmentHref ?? ''
    attachmentName.value = attachment.textContent?.trim() || labels.value.attachment
    attachmentDialogOpen.value = true
    return
  }

  const target = element.closest('img') as HTMLImageElement | null
  if (!target) {
    selectedImage.value = null
    return
  }
  event.preventDefault()
  selectedImage.value = target
  positionResizeHandle(target)
}

function handleContentKeydown(event: KeyboardEvent): void {
  if (event.key !== 'Enter' && event.key !== ' ') return
  const attachment = (event.target as HTMLElement).closest('a[data-attachment-href]') as HTMLAnchorElement | null
  if (!attachment) return
  event.preventDefault()
  event.stopImmediatePropagation()
  selectedImage.value = null
  attachmentSource.value = attachment.dataset.attachmentHref ?? ''
  attachmentName.value = attachment.textContent?.trim() || labels.value.attachment
  attachmentDialogOpen.value = true
}

function openSelectedImage(): void {
  const image = selectedImage.value
  if (!image) return
  viewerSource.value = sourceWithoutSize(image.getAttribute('src') ?? '')
  viewerAlt.value = image.getAttribute('alt') ?? ''
  viewerOpen.value = true
}

function startResize(event: PointerEvent): void {
  const image = selectedImage.value
  const content = editor.value?.view.dom
  if (!image || !content || props.disabled) return
  event.preventDefault()
  event.stopPropagation()
  resizeState = {
    image,
    startX: event.clientX,
    startWidth: image.getBoundingClientRect().width,
    containerWidth: content.clientWidth,
  }
  window.addEventListener('pointermove', resizeImage)
  window.addEventListener('pointerup', finishResize, { once: true })
}

function resizeImage(event: PointerEvent): void {
  if (!resizeState) return
  const width = resizeState.startWidth + event.clientX - resizeState.startX
  const percentage = Math.min(100, Math.max(10, Math.round(width / resizeState.containerWidth * 100)))
  resizeState.image.style.width = `${percentage}%`
  positionResizeHandle(resizeState.image)
}

function finishResize(): void {
  window.removeEventListener('pointermove', resizeImage)
  if (!resizeState || !editor.value) return
  const { image, containerWidth } = resizeState
  const percentage = Math.min(100, Math.max(10, Math.round(image.getBoundingClientRect().width / containerWidth * 100)))
  const source = image.getAttribute('src') ?? ''
  try {
    const position = editor.value.view.posAtDOM(image, 0)
    editor.value.chain().setNodeSelection(position).updateAttributes('image', {
      src: `${sourceWithoutSize(source)}#size=${percentage}`,
    }).run()
  } finally {
    resizeState = undefined
  }
}

function currentBlockStyle(): string {
  if (editor.value?.isActive('heading', { level: 1 })) return 'heading-1'
  if (editor.value?.isActive('heading', { level: 2 })) return 'heading-2'
  if (editor.value?.isActive('heading', { level: 3 })) return 'heading-3'
  return 'paragraph'
}

function setBlockStyle(event: Event): void {
  const value = (event.target as HTMLSelectElement).value
  const chain = selectedChain()
  if (!chain) return

  if (value === 'paragraph') {
    chain.setParagraph().run()
    return
  }

  const level = Number(value.replace('heading-', '')) as 1 | 2 | 3
  chain.setHeading({ level }).run()
}

onBeforeUnmount(() => {
  window.removeEventListener('pointermove', resizeImage)
  editor.value?.destroy()
})
</script>

<template>
  <div class="ui-markdown-editor" :class="{ 'ui-markdown-editor--disabled': disabled }">
    <div v-if="showToolbar" class="ui-markdown-editor__toolbar" role="toolbar" :aria-label="labels.toolbar">
      <button type="button" :aria-label="labels.bold" :title="labels.bold" :disabled="disabled" :class="{ active: editor?.isActive('bold') }" @mousedown.prevent="rememberSelection" @click="toggleBold"><Bold :size="16" aria-hidden="true" /></button>
      <button type="button" :aria-label="labels.italic" :title="labels.italic" :disabled="disabled" :class="{ active: editor?.isActive('italic') }" @mousedown.prevent="rememberSelection" @click="toggleItalic"><Italic :size="16" aria-hidden="true" /></button>
      <label class="ui-markdown-editor__size"><span class="sr-only">{{ labels.blockStyle }}</span><select :aria-label="labels.blockStyle" :title="labels.blockStyleHint" :value="currentBlockStyle()" :disabled="disabled" @mousedown="rememberSelection" @change="setBlockStyle"><option value="paragraph">¶</option><option value="heading-1">H1</option><option value="heading-2">H2</option><option value="heading-3">H3</option></select></label>
      <button type="button" :aria-label="labels.bulletList" :title="labels.bulletList" :disabled="disabled" :class="{ active: editor?.isActive('bulletList') }" @mousedown.prevent="rememberSelection" @click="toggleBulletList"><List :size="16" aria-hidden="true" /></button>
      <button type="button" :aria-label="labels.orderedList" :title="labels.orderedList" :disabled="disabled" :class="{ active: editor?.isActive('orderedList') }" @mousedown.prevent="rememberSelection" @click="toggleOrderedList"><ListOrdered :size="16" aria-hidden="true" /></button>
      <button type="button" :aria-label="labels.codeBlock" :title="labels.codeBlockHint" :disabled="disabled" :class="{ active: editor?.isActive('codeBlock') }" @mousedown.prevent="rememberSelection" @click="toggleCodeBlock"><Code2 :size="16" aria-hidden="true" /></button>
      <label v-if="editor?.isActive('codeBlock')" class="ui-markdown-editor__language"><span class="sr-only">{{ labels.codeLanguage }}</span><select :aria-label="labels.codeLanguage" :title="labels.codeLanguage" :value="currentCodeLanguage()" :disabled="disabled" @mousedown="rememberSelection" @change="setCodeLanguage"><option value="">{{ labels.plainText }}</option><option value="javascript">JavaScript</option><option value="typescript">TypeScript</option><option value="csharp">C#</option><option value="java">Java</option><option value="python">Python</option><option value="json">JSON</option><option value="sql">SQL</option><option value="bash">Bash</option><option value="html">HTML</option><option value="css">CSS</option><option value="yaml">YAML</option></select></label>
      <button type="button" :aria-label="labels.link" :title="labels.link" :disabled="disabled" @mousedown.prevent="rememberSelection" @click="setLink"><LinkIcon :size="16" aria-hidden="true" /></button>
      <button v-if="uploadImage || uploadAttachment" type="button" :aria-label="labels.image" :title="labels.image" :disabled="disabled || uploadingImage" @mousedown.prevent="rememberSelection" @click="fileInput?.click()"><ImagePlus :size="16" aria-hidden="true" /></button>
      <button v-if="uploadAttachment" type="button" :aria-label="labels.attachment" :title="labels.attachment" :disabled="disabled || uploadingImage" @mousedown.prevent="rememberSelection" @click="attachmentInput?.click()"><Paperclip :size="16" aria-hidden="true" /></button>
      <input ref="fileInput" class="sr-only" type="file" accept="image/*" tabindex="-1" @change="selectImage" />
      <input ref="attachmentInput" class="sr-only" type="file" tabindex="-1" @change="selectAttachment" />
    </div>
    <p v-if="imageError" class="ui-markdown-editor__error" role="alert">{{ imageError }}</p>
    <div class="ui-markdown-editor__canvas" @click.capture="handleContentClick" @keydown.capture="handleContentKeydown">
      <EditorContent v-if="editor" :editor="editor" />
      <button v-if="selectedImage" type="button" class="ui-markdown-editor__preview-button" :style="previewButtonStyle" :aria-label="labels.imagePreview" :title="labels.imagePreview" @click.stop="openSelectedImage"><ZoomIn :size="17" aria-hidden="true" /></button>
      <button v-if="selectedImage && !disabled" type="button" class="ui-markdown-editor__resize-handle" :style="resizeHandleStyle" :aria-label="labels.resizeImage" :title="labels.resizeImage" @pointerdown="startResize" @click.stop />
    </div>
    <UiImageViewer
      :open="viewerOpen"
      :src="viewerSource"
      :alt="viewerAlt"
      :labels="{ dialog: labels.imagePreview, close: labels.close, zoomIn: labels.zoomIn, zoomOut: labels.zoomOut, fit: labels.fitImage, reset: labels.resetImage }"
      @close="viewerOpen = false"
    />
    <UiAttachmentDialog
      :open="attachmentDialogOpen"
      :href="attachmentSource"
      :file-name="attachmentName"
      :labels="labels"
      @close="attachmentDialogOpen = false"
    />
  </div>
</template>

<style scoped>
.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}.ui-markdown-editor{border:1px solid var(--kk-border);border-radius:6px;background:var(--kk-surface)}.ui-markdown-editor:focus-within{border-color:var(--kk-accent);box-shadow:0 0 0 2px var(--kk-accent-soft)}.ui-markdown-editor--disabled .ui-markdown-editor__toolbar{opacity:.5}.ui-markdown-editor__toolbar{display:flex;align-items:center;gap:4px;padding:5px;border-bottom:1px solid var(--kk-border);background:var(--kk-surface-subtle)}.ui-markdown-editor__toolbar button{display:grid;place-items:center;min-width:28px;height:28px;padding:0 6px;border:0;border-radius:4px;background:transparent;color:var(--kk-text);cursor:pointer}.ui-markdown-editor__toolbar button:hover,.ui-markdown-editor__toolbar button.active{background:var(--kk-accent-soft);color:var(--kk-accent)}.ui-markdown-editor__toolbar button:disabled{cursor:not-allowed}.ui-markdown-editor__size select,.ui-markdown-editor__language select{height:28px;padding:0 22px 0 7px;border:0;border-radius:4px;background:transparent;color:var(--kk-text);font:inherit;font-size:.78rem;cursor:pointer}.ui-markdown-editor__size select:hover,.ui-markdown-editor__size select:focus,.ui-markdown-editor__language select:hover,.ui-markdown-editor__language select:focus{background:var(--kk-accent-soft);color:var(--kk-accent);outline:0}.ui-markdown-editor__error{margin:0;padding:7px 10px;color:#b42318;background:#fff1f0;font-size:.8rem}.ui-markdown-editor__canvas{position:relative}.ui-markdown-editor__resize-handle{position:absolute;z-index:2;width:15px;height:15px;padding:0;border:2px solid #fff;border-radius:50%;background:var(--kk-accent);box-shadow:0 1px 4px rgb(0 0 0 / .35);cursor:nwse-resize;touch-action:none}.ui-markdown-editor :deep(.ui-markdown-editor__content){min-height:120px;padding:10px;outline:0;white-space:pre-wrap;font-synthesis:weight style}.ui-markdown-editor--disabled :deep(.ui-markdown-editor__content){cursor:default}.ui-markdown-editor :deep(.ui-markdown-editor__content p){margin:0 0 .7em;line-height:1.55}.ui-markdown-editor :deep(.ui-markdown-editor__content h1),.ui-markdown-editor :deep(.ui-markdown-editor__content h2),.ui-markdown-editor :deep(.ui-markdown-editor__content h3){margin:.85em 0 .45em;line-height:1.25;font-weight:750}.ui-markdown-editor :deep(.ui-markdown-editor__content h1){font-size:1.5rem}.ui-markdown-editor :deep(.ui-markdown-editor__content h2){font-size:1.25rem}.ui-markdown-editor :deep(.ui-markdown-editor__content h3){font-size:1.1rem}.ui-markdown-editor :deep(.ui-markdown-editor__content strong){font-weight:800}.ui-markdown-editor :deep(.ui-markdown-editor__content em){font-style:italic}.ui-markdown-editor :deep(.ui-markdown-editor__content ul),.ui-markdown-editor :deep(.ui-markdown-editor__content ol){padding-left:1.5rem;margin:.25em 0 .7em}.ui-markdown-editor :deep(.ui-markdown-editor__content img){display:block;max-width:100%;height:auto;margin:.75em 0;border:1px solid var(--kk-border);border-radius:6px;cursor:zoom-in}.ui-markdown-editor :deep(.ui-markdown-editor__content img.ProseMirror-selectednode){outline:2px solid var(--kk-accent);outline-offset:2px}.ui-markdown-editor :deep(.ui-markdown-editor__content pre){overflow-x:auto;margin:.75em 0;padding:12px 14px;color:#25332b;background:#f2f5f3;border:1px solid var(--kk-border);border-left:3px solid var(--kk-accent);border-radius:6px;line-height:1.55;white-space:pre-wrap}.ui-markdown-editor :deep(.ui-markdown-editor__content pre code){padding:0;color:inherit;background:transparent;border:0;border-radius:0;font-family:ui-monospace,SFMono-Regular,Consolas,"Liberation Mono",monospace;font-size:.9em}.ui-markdown-editor :deep(.ui-markdown-editor__content code:not(pre code)){padding:1px 4px;color:#9b3f12;background:#fff3e8;border-radius:4px;font-family:ui-monospace,SFMono-Regular,Consolas,"Liberation Mono",monospace;font-size:.9em}.ui-markdown-editor :deep(.ui-markdown-editor__content p:first-child),.ui-markdown-editor :deep(.ui-markdown-editor__content h1:first-child),.ui-markdown-editor :deep(.ui-markdown-editor__content h2:first-child),.ui-markdown-editor :deep(.ui-markdown-editor__content h3:first-child),.ui-markdown-editor :deep(.ui-markdown-editor__content pre:first-child){margin-top:0}.ui-markdown-editor :deep(.ui-markdown-editor__content p:last-child),.ui-markdown-editor :deep(.ui-markdown-editor__content pre:last-child){margin-bottom:0}.ui-markdown-editor :deep(.ui-markdown-editor__content.is-editor-empty:first-child::before){content:attr(data-placeholder);float:left;height:0;color:var(--kk-text-muted);pointer-events:none}
.ui-markdown-editor__preview-button{position:absolute;z-index:2;display:grid;place-items:center;width:30px;height:30px;padding:0;border:1px solid rgb(255 255 255 / .8);border-radius:6px;background:rgb(25 34 29 / .82);color:#fff;box-shadow:0 1px 5px rgb(0 0 0 / .25);cursor:zoom-in}.ui-markdown-editor__preview-button:hover,.ui-markdown-editor__preview-button:focus-visible{background:var(--kk-accent);outline:0}
.ui-markdown-editor :deep(a[data-attachment-href]){display:flex;width:fit-content;max-width:100%;align-items:center;gap:7px;margin:.55em 0;padding:7px 10px;border:1px solid var(--kk-border);border-radius:6px;background:var(--kk-surface-subtle);color:var(--kk-accent);font-weight:650;line-height:1.35;text-decoration:none;overflow-wrap:anywhere;cursor:pointer}.ui-markdown-editor :deep(a[data-attachment-href]::before){content:"";flex:0 0 16px;width:16px;height:16px;background:currentColor;mask:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'/%3E%3C/svg%3E") center/contain no-repeat}.ui-markdown-editor :deep(a[data-attachment-href]:hover),.ui-markdown-editor :deep(a[data-attachment-href]:focus-visible){border-color:var(--kk-accent);background:var(--kk-accent-soft);outline:0}
</style>
