<script setup lang="ts">
import { UiField, UiFormGrid, UiFormSection, UiSelect } from '@khaikang/ui'
import type { ProjectRoleResponse } from '../api/contracts'

defineProps<{
  roles: ProjectRoleResponse[]
  disabled: boolean
  labels: {
    sectionTitle: string
    sectionDescription: string
    username: string
    usernamePlaceholder: string
    roles: string
  }
}>()

const username = defineModel<string>('username', { required: true })
const roleCode = defineModel<string>('roleCode', { required: true })
</script>

<template>
  <UiFormSection>
    <template #header><div><h3>{{ labels.sectionTitle }}</h3><p>{{ labels.sectionDescription }}</p></div></template>
    <UiFormGrid :columns="2">
      <UiField id="project-member-username" v-model="username" :label="labels.username" :placeholder="labels.usernamePlaceholder" required :disabled="disabled" />
      <label class="role-field"><span>{{ labels.roles }} *</span><UiSelect v-model="roleCode" :disabled="disabled"><option v-for="role in roles" :key="role.code" :value="role.code">{{ role.name }}</option></UiSelect></label>
    </UiFormGrid>
  </UiFormSection>
</template>

<style scoped>
.role-field { display: grid; gap: 7px; }
.role-field > span { color: var(--kk-text); font-size: .875rem; font-weight: 650; }
.role-field :deep(.ui-select) { width: 100%; min-height: 44px; padding: 9px 11px; font-size: 1rem; }
</style>
