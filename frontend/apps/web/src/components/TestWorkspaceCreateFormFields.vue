<script setup lang="ts">
import { UiField, UiFormGrid, UiFormSection } from '@khaikang/ui'

defineProps<{
  prefixError: string
  disabled: boolean
  labels: {
    sectionTitle: string
    sectionDescription: string
    name: string
    prefix: string
    description: string
  }
}>()

const name = defineModel<string>('name', { required: true })
const prefix = defineModel<string>('prefix', { required: true })
const description = defineModel<string>('description', { required: true })
</script>

<template>
  <UiFormSection>
    <template #header><div><h3>{{ labels.sectionTitle }}</h3><p>{{ labels.sectionDescription }}</p></div></template>
    <UiFormGrid :columns="2">
      <UiField id="test-workspace-name" v-model="name" :label="labels.name" required :disabled="disabled" />
      <UiField id="test-workspace-prefix" v-model="prefix" :label="labels.prefix" :disabled="disabled" :error="prefixError" />
      <UiField id="test-workspace-description" v-model="description" class="ui-form-grid__full" :label="labels.description" multiline :disabled="disabled" />
    </UiFormGrid>
  </UiFormSection>
</template>
