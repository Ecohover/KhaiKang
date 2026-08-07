<script setup lang="ts">
import { onMounted, watch } from 'vue'

const props = withDefaults(defineProps<{
  modelValue: 'list' | 'grid'
  storageKey?: string
  listLabel?: string
  gridLabel?: string
  groupLabel?: string
}>(), {
  storageKey: '',
  listLabel: 'List',
  gridLabel: 'Cards',
  groupLabel: 'View mode',
})
const emit = defineEmits<{ 'update:modelValue': [value: 'list' | 'grid'] }>()

onMounted(() => {
  if (!props.storageKey) return
  const saved = window.localStorage.getItem(props.storageKey)
  if (saved === 'list' || saved === 'grid') emit('update:modelValue', saved)
})

watch(() => props.modelValue, (value) => {
  if (props.storageKey) window.localStorage.setItem(props.storageKey, value)
})
</script>

<template>
  <div class="ui-view-mode-toggle" role="group" :aria-label="groupLabel">
    <button type="button" :class="{ active: modelValue === 'list' }" :aria-pressed="modelValue === 'list'" @click="$emit('update:modelValue', 'list')">
      <svg viewBox="0 0 24 24" width="17" height="17" aria-hidden="true"><path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" /></svg><span>{{ listLabel }}</span>
    </button>
    <button type="button" :class="{ active: modelValue === 'grid' }" :aria-pressed="modelValue === 'grid'" @click="$emit('update:modelValue', 'grid')">
      <svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true"><path d="M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z" /></svg><span>{{ gridLabel }}</span>
    </button>
  </div>
</template>

<style scoped>
.ui-view-mode-toggle{display:flex;align-self:stretch;width:fit-content;gap:6px;border-bottom:2px solid var(--kk-border)}.ui-view-mode-toggle button{display:flex;min-height:42px;align-items:center;gap:7px;padding:8px 16px;margin-bottom:-2px;color:var(--kk-text-muted);background:transparent;border:0;border-bottom:2px solid transparent;border-top-left-radius:6px;border-top-right-radius:6px;cursor:pointer;font:inherit;font-size:.88rem;font-weight:600}.ui-view-mode-toggle svg{fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round}.ui-view-mode-toggle button:hover{color:var(--kk-text);background:var(--kk-surface-subtle)}.ui-view-mode-toggle button.active{color:var(--kk-accent);background:var(--kk-surface);border-bottom-color:var(--kk-accent);font-weight:700}
@media(max-width:520px){.ui-view-mode-toggle button span{display:none}}
</style>
