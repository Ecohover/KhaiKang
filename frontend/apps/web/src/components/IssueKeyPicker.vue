<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Search } from '@lucide/vue'
import { UiButton, UiInput } from '@khaikang/ui'
import type { IssueResponse } from '../api/contracts'
import { findIssueByExactKey } from '../issues/issueKeySearch'

const props = withDefaults(defineProps<{
  modelValue: string
  issues: IssueResponse[]
  id: string
  label: string
  placeholder: string
  searchLabel: string
  notFoundMessage: string
  disabled?: boolean
}>(), {
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const query = ref('')
const notFound = ref(false)
const retainQueryOnClear = ref(false)
const selectedIssue = computed(() => props.issues.find(issue => issue.id === props.modelValue))

watch(
  () => props.modelValue,
  (value) => {
    if (!value && retainQueryOnClear.value) {
      retainQueryOnClear.value = false
      notFound.value = false
      return
    }
    query.value = props.issues.find(issue => issue.id === value)?.key ?? ''
    notFound.value = false
  },
  { immediate: true },
)

watch(query, (value) => {
  notFound.value = false
  if (selectedIssue.value && !findIssueByExactKey([selectedIssue.value], value)) {
    retainQueryOnClear.value = true
    emit('update:modelValue', '')
  }
})

function search(): void {
  if (props.disabled) return

  const match = findIssueByExactKey(props.issues, query.value)
  if (!match) {
    emit('update:modelValue', '')
    notFound.value = true
    return
  }

  query.value = match.key
  emit('update:modelValue', match.id)
  notFound.value = false
}
</script>

<template>
  <div class="issue-key-picker">
    <label :for="id">{{ label }}</label>
    <form class="issue-key-picker__search" role="search" @submit.prevent="search">
      <UiInput
        :id="id"
        v-model="query"
        :disabled="disabled"
        :placeholder="placeholder"
        autocomplete="off"
        spellcheck="false"
      />
      <UiButton type="submit" variant="secondary" :disabled="disabled || !query.trim()">
        <Search :size="15" aria-hidden="true" />{{ searchLabel }}
      </UiButton>
    </form>
    <p v-if="notFound" class="issue-key-picker__error" role="alert">{{ notFoundMessage }}</p>
    <div v-else-if="selectedIssue" class="issue-key-picker__result" aria-live="polite">
      <strong>{{ selectedIssue.key }}</strong>
      <span>{{ selectedIssue.title }}</span>
    </div>
  </div>
</template>

<style scoped>
.issue-key-picker{display:grid;gap:6px;min-width:0}.issue-key-picker>label{font-size:.84rem;font-weight:650}.issue-key-picker__search{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:8px}.issue-key-picker__search button{min-height:32px}.issue-key-picker__result{display:flex;min-width:0;gap:8px;padding:7px 10px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-radius:var(--kk-radius);font-size:.84rem}.issue-key-picker__result span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.issue-key-picker__error{margin:0;color:var(--kk-danger);font-size:.8rem}@media(max-width:520px){.issue-key-picker__search{grid-template-columns:1fr}}
</style>
