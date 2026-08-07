<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { UiMarkdown } from '@khaikang/ui'

withDefaults(defineProps<{
  modelValue: string
  mode?: 'edit' | 'display'
  size?: 'default' | 'compact'
  disabled?: boolean
  placeholder?: string
  uploadImage?: ((file: File) => Promise<{ src: string, alt?: string }>) | undefined
  uploadAttachment?: ((file: File) => Promise<{ src: string, name?: string }>) | undefined
}>(), {
  mode: 'edit',
  size: 'default',
  disabled: false,
  placeholder: '',
})

defineEmits<{
  'update:modelValue': [value: string]
  blur: []
}>()

const { t } = useI18n()
const labels = computed(() => ({
  modeLabel: t('common.markdown.modeLabel'),
  visualMode: t('common.markdown.visualMode'),
  sourceMode: t('common.markdown.sourceMode'),
  sourceLabel: t('common.markdown.sourceLabel'),
  toolbar: t('common.markdown.toolbar'),
  bold: t('common.markdown.bold'),
  italic: t('common.markdown.italic'),
  blockStyle: t('common.markdown.blockStyle'),
  blockStyleHint: t('common.markdown.blockStyleHint'),
  paragraph: t('common.markdown.paragraph'),
  heading1: t('common.markdown.heading1'),
  heading2: t('common.markdown.heading2'),
  heading3: t('common.markdown.heading3'),
  bulletList: t('common.markdown.bulletList'),
  orderedList: t('common.markdown.orderedList'),
  codeBlock: t('common.markdown.codeBlock'),
  codeBlockHint: t('common.markdown.codeBlockHint'),
  codeLanguage: t('common.markdown.codeLanguage'),
  plainText: t('common.markdown.plainText'),
  link: t('common.markdown.link'),
  linkPrompt: t('common.markdown.linkPrompt'),
  image: t('common.markdown.image'),
  imageUploadFailed: t('common.markdown.imageUploadFailed'),
  attachment: t('common.markdown.attachment'),
  attachmentUploadFailed: t('common.markdown.attachmentUploadFailed'),
  attachmentDialog: t('common.markdown.attachmentDialog'),
  downloadAttachment: t('common.markdown.downloadAttachment'),
  resizeImage: t('common.markdown.resizeImage'),
  imagePreview: t('common.markdown.imagePreview'),
  close: t('common.actions.close'),
  zoomIn: t('common.markdown.zoomIn'),
  zoomOut: t('common.markdown.zoomOut'),
  fitImage: t('common.markdown.fitImage'),
  resetImage: t('common.markdown.resetImage'),
}))
</script>

<template>
  <UiMarkdown
    :model-value="modelValue"
    :mode="mode"
    :size="size"
    :disabled="disabled"
    :placeholder="placeholder"
    :labels="labels"
    :upload-image="uploadImage"
    :upload-attachment="uploadAttachment"
    @update:model-value="$emit('update:modelValue', $event)"
    @blur="$emit('blur')"
  />
</template>
