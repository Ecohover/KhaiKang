<script setup lang="ts">
import { computed, ref } from 'vue'
import UiMarkdownEditor from './UiMarkdownEditor.vue'

const props = withDefaults(defineProps<{
  modelValue: string
  mode?: 'edit' | 'display'
  size?: 'default' | 'compact'
  disabled?: boolean
  placeholder?: string
  labels?: Record<string, string>
  uploadImage?: ((file: File) => Promise<{ src: string, alt?: string }>) | undefined
  uploadAttachment?: ((file: File) => Promise<{ src: string, name?: string }>) | undefined
}>(), {
  mode: 'edit',
  size: 'default',
  disabled: false,
  placeholder: '',
  labels: () => ({}),
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  blur: []
}>()

const editorMode = ref<'visual' | 'source'>('visual')
const readOnly = computed(() => props.disabled || props.mode === 'display')
const displayOnly = computed(() => props.mode === 'display')
const resolvedLabels = computed(() => ({
  modeLabel: 'Markdown editing mode',
  visualMode: 'Visual editor',
  sourceMode: 'Markdown',
  sourceLabel: 'Markdown source',
  ...props.labels,
}))

function updateSource(event: Event): void {
  emit('update:modelValue', (event.target as HTMLTextAreaElement).value)
}

function handleFocusOut(event: FocusEvent): void {
  const container = event.currentTarget as HTMLElement
  const nextTarget = event.relatedTarget as Node | null

  if (nextTarget && container.contains(nextTarget)) return
  emit('blur')
}
</script>

<template>
  <div
    class="ui-markdown"
    :class="[`ui-markdown--${size}`, { 'ui-markdown--readonly': readOnly, 'ui-markdown--display': displayOnly }]"
    @focusout="handleFocusOut"
  >
    <UiMarkdownEditor
      v-if="displayOnly"
      :model-value="modelValue"
      disabled
      :show-toolbar="false"
      :placeholder="placeholder"
      :labels="resolvedLabels"
    />
    <template v-else>
      <div class="ui-markdown__tabs" role="tablist" :aria-label="resolvedLabels.modeLabel">
        <button type="button" role="tab" :aria-selected="editorMode === 'visual'" :class="{ active: editorMode === 'visual' }" @click="editorMode = 'visual'">{{ resolvedLabels.visualMode }}</button>
        <button type="button" role="tab" :aria-selected="editorMode === 'source'" :class="{ active: editorMode === 'source' }" @click="editorMode = 'source'">{{ resolvedLabels.sourceMode }}</button>
      </div>
      <UiMarkdownEditor
        v-if="editorMode === 'visual'"
        :model-value="modelValue"
        :disabled="readOnly"
        :placeholder="placeholder"
        :labels="resolvedLabels"
        :upload-image="uploadImage"
        :upload-attachment="uploadAttachment"
        @update:model-value="$emit('update:modelValue', $event)"
      />
      <textarea
        v-else
        class="ui-markdown__source"
        :value="modelValue"
        :disabled="readOnly"
        :placeholder="placeholder"
        :aria-label="resolvedLabels.sourceLabel"
        spellcheck="false"
        @input="updateSource"
      />
    </template>
  </div>
</template>

<style scoped>
.ui-markdown{display:grid;min-width:0}.ui-markdown__tabs{display:flex;width:fit-content;gap:2px;padding:0 4px;border:1px solid var(--kk-border);border-bottom:0;border-top-left-radius:6px;border-top-right-radius:6px;background:var(--kk-surface-subtle)}.ui-markdown__tabs button{min-height:32px;padding:5px 10px;margin-bottom:-1px;color:var(--kk-text-muted);background:transparent;border:0;border-bottom:2px solid transparent;font:inherit;font-size:.78rem;font-weight:650;cursor:pointer}.ui-markdown__tabs button:hover,.ui-markdown__tabs button.active{color:var(--kk-accent);background:var(--kk-surface)}.ui-markdown__tabs button.active{border-bottom-color:var(--kk-accent)}.ui-markdown :deep(.ui-markdown-editor){border-top-left-radius:0}.ui-markdown__source{width:100%;min-height:159px;padding:12px 14px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:0 6px 6px 6px;outline:0;resize:vertical;font-family:ui-monospace,SFMono-Regular,Consolas,"Liberation Mono",monospace;font-size:.875rem;line-height:1.55;tab-size:2}.ui-markdown--compact :deep(.ui-markdown-editor__content){min-height:48px;padding:8px 10px}.ui-markdown--compact .ui-markdown__source{min-height:88px;padding:8px 10px}.ui-markdown--readonly .ui-markdown__tabs{background:#edf1ef;border-color:#cfd7d2}.ui-markdown--readonly .ui-markdown__tabs button{color:#69736e}.ui-markdown--readonly .ui-markdown__tabs button.active{color:#385846;background:#f4f7f5;border-bottom-color:#7f9b89}.ui-markdown--readonly :deep(.ui-markdown-editor){background:#f7f9f8;border-color:#cfd7d2}.ui-markdown--readonly :deep(.ui-markdown-editor__toolbar){background:#edf1ef;opacity:.58}.ui-markdown--readonly :deep(.ui-markdown-editor__content){color:#4d5852;background:#f7f9f8}.ui-markdown--readonly .ui-markdown__source{color:#4d5852;background:#f7f9f8;border-color:#cfd7d2}.ui-markdown__source:focus{border-color:var(--kk-accent);box-shadow:0 0 0 2px var(--kk-accent-soft)}.ui-markdown__source:disabled{cursor:default;opacity:1}
.ui-markdown--display :deep(.ui-markdown-editor){border-radius:6px}
.ui-markdown--display :deep(.ui-markdown-editor__content){min-height:0}
</style>
