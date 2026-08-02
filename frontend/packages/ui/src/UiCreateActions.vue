<script setup lang="ts">
import UiButton from './UiButton.vue'

withDefaults(defineProps<{
  disabled?: boolean
  loading?: boolean
  cancelLabel?: string
  createLabel?: string
  continueLabel?: string
  allowContinue?: boolean
  showCancel?: boolean
}>(), {
  disabled: false,
  loading: false,
  cancelLabel: 'Cancel',
  createLabel: 'Create',
  continueLabel: 'Create and continue',
  allowContinue: true,
  showCancel: true,
})

defineEmits<{ cancel: []; create: []; createContinue: [] }>()
</script>

<template>
  <div class="ui-create-actions">
    <UiButton v-if="showCancel" type="button" variant="secondary" :disabled="loading" @click="$emit('cancel')">{{ cancelLabel }}</UiButton>
    <UiButton type="button" :loading="loading" :disabled="disabled" @click="$emit('create')">{{ createLabel }}</UiButton>
    <UiButton v-if="allowContinue" type="button" variant="secondary" :disabled="disabled || loading" @click="$emit('createContinue')">{{ continueLabel }}</UiButton>
  </div>
</template>

<style scoped>
.ui-create-actions{display:flex;justify-content:flex-end;gap:10px;flex-wrap:wrap}
</style>
