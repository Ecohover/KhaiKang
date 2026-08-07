<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { Maximize2, Minus, Plus, RotateCcw, X } from '@lucide/vue'

const props = withDefaults(defineProps<{
  open: boolean
  src: string
  alt?: string
  labels?: Record<string, string>
}>(), {
  alt: '',
  labels: () => ({}),
})

const emit = defineEmits<{ close: [] }>()
const labels = computed(() => ({
  dialog: 'Image preview',
  close: 'Close',
  zoomIn: 'Zoom in',
  zoomOut: 'Zoom out',
  fit: 'Fit to window',
  reset: 'Reset view',
  ...props.labels,
}))

const zoom = ref(1)
const offsetX = ref(0)
const offsetY = ref(0)
let dragStart: { x: number, y: number, offsetX: number, offsetY: number } | undefined

const imageTransform = computed(() => ({
  transform: `translate(${offsetX.value}px, ${offsetY.value}px) scale(${zoom.value})`,
}))

function reset(): void {
  zoom.value = 1
  offsetX.value = 0
  offsetY.value = 0
}

function setZoom(value: number): void {
  zoom.value = Math.min(5, Math.max(.25, Number(value.toFixed(2))))
}

function handleWheel(event: WheelEvent): void {
  setZoom(zoom.value + (event.deltaY < 0 ? .15 : -.15))
}

function startDrag(event: PointerEvent): void {
  dragStart = { x: event.clientX, y: event.clientY, offsetX: offsetX.value, offsetY: offsetY.value }
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
}

function moveDrag(event: PointerEvent): void {
  if (!dragStart) return
  offsetX.value = dragStart.offsetX + event.clientX - dragStart.x
  offsetY.value = dragStart.offsetY + event.clientY - dragStart.y
}

function stopDrag(): void {
  dragStart = undefined
}

function handleKeydown(event: KeyboardEvent): void {
  if (props.open && event.key === 'Escape') emit('close')
}

watch(() => props.open, (open) => {
  if (open) reset()
})

onMounted(() => window.addEventListener('keydown', handleKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', handleKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="ui-image-viewer" role="dialog" aria-modal="true" :aria-label="labels.dialog" @mousedown.self="$emit('close')">
      <div class="ui-image-viewer__toolbar">
        <button type="button" :aria-label="labels.zoomOut" :title="labels.zoomOut" @click="setZoom(zoom - .25)"><Minus :size="18" /></button>
        <span class="ui-image-viewer__zoom">{{ Math.round(zoom * 100) }}%</span>
        <button type="button" :aria-label="labels.zoomIn" :title="labels.zoomIn" @click="setZoom(zoom + .25)"><Plus :size="18" /></button>
        <button type="button" :aria-label="labels.fit" :title="labels.fit" @click="reset"><Maximize2 :size="18" /></button>
        <button type="button" :aria-label="labels.reset" :title="labels.reset" @click="reset"><RotateCcw :size="18" /></button>
        <button type="button" :aria-label="labels.close" :title="labels.close" @click="$emit('close')"><X :size="20" /></button>
      </div>
      <div class="ui-image-viewer__stage" @wheel.prevent="handleWheel" @pointerdown="startDrag" @pointermove="moveDrag" @pointerup="stopDrag" @pointercancel="stopDrag">
        <img :src="src" :alt="alt" draggable="false" :style="imageTransform" />
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.ui-image-viewer{position:fixed;inset:0;z-index:1000;display:grid;grid-template-rows:auto 1fr;padding:18px;background:rgb(18 24 21 / .88);backdrop-filter:blur(3px)}.ui-image-viewer__toolbar{z-index:1;display:flex;align-items:center;justify-content:center;gap:6px;width:fit-content;margin:0 auto 12px;padding:6px;border:1px solid rgb(255 255 255 / .18);border-radius:8px;background:rgb(25 34 29 / .94);color:#fff}.ui-image-viewer__toolbar button{display:grid;place-items:center;width:34px;height:34px;padding:0;border:0;border-radius:5px;background:transparent;color:inherit;cursor:pointer}.ui-image-viewer__toolbar button:hover,.ui-image-viewer__toolbar button:focus-visible{background:rgb(255 255 255 / .14);outline:0}.ui-image-viewer__zoom{min-width:54px;text-align:center;font-size:.8rem;font-variant-numeric:tabular-nums}.ui-image-viewer__stage{display:grid;place-items:center;min-width:0;min-height:0;overflow:hidden;cursor:grab;touch-action:none}.ui-image-viewer__stage:active{cursor:grabbing}.ui-image-viewer__stage img{display:block;max-width:100%;max-height:100%;object-fit:contain;transform-origin:center;transition:transform .08s ease-out;user-select:none;pointer-events:none}
</style>
