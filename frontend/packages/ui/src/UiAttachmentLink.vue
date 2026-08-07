<script setup lang="ts">
import { ref } from 'vue'
import UiAttachmentDialog from './UiAttachmentDialog.vue'

withDefaults(defineProps<{
  href: string
  fileName: string
  labels?: Record<string, string>
}>(), {
  labels: () => ({}),
})

const dialogOpen = ref(false)

function openDialog(): void {
  dialogOpen.value = true
}
</script>

<template>
  <button type="button" class="ui-attachment-link" @click="openDialog"><slot>{{ fileName }}</slot></button>
  <UiAttachmentDialog
    :open="dialogOpen"
    :href="href"
    :file-name="fileName"
    :labels="labels"
    @close="dialogOpen = false"
  />
</template>

<style scoped>
.ui-attachment-link{display:inline-flex;max-width:100%;align-items:center;gap:7px;padding:7px 10px;border:1px solid var(--kk-border);border-radius:6px;background:var(--kk-surface-subtle);color:var(--kk-accent);font:inherit;font-weight:650;line-height:1.35;text-align:left;overflow-wrap:anywhere;cursor:pointer}.ui-attachment-link::before{content:"";flex:0 0 16px;width:16px;height:16px;background:currentColor;-webkit-mask:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'/%3E%3C/svg%3E") center/contain no-repeat;mask:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48'/%3E%3C/svg%3E") center/contain no-repeat}.ui-attachment-link:hover,.ui-attachment-link:focus-visible{border-color:var(--kk-accent);background:var(--kk-accent-soft);outline:0}
</style>
