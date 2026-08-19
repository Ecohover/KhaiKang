<script setup lang="ts">
import { UiField, UiFormGrid, UiFormSection } from '@khaikang/ui'

defineProps<{
  codeError: string
  disabled: boolean
  labels: {
    sectionTitle: string
    sectionDescription: string
    code: string
    name: string
    description: string
  }
}>()

const code = defineModel<string>('code', { required: true })
const name = defineModel<string>('name', { required: true })
const description = defineModel<string>('description', { required: true })
</script>

<template>
  <UiFormSection>
    <template #header><div><h3>{{ labels.sectionTitle }}</h3><p>{{ labels.sectionDescription }}</p></div></template>
    <UiFormGrid :columns="2">
      <UiField id="project-code" v-model="code" :label="labels.code" required :disabled="disabled" :error="codeError" />
      <UiField id="project-name" v-model="name" :label="labels.name" required :disabled="disabled" />
      <UiField id="project-description" v-model="description" class="ui-form-grid__full" :label="labels.description" multiline :disabled="disabled" />
    </UiFormGrid>
  </UiFormSection>
</template>
