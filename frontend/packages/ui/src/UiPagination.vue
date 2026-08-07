<script setup lang="ts">
import { computed } from 'vue'
import UiSelect from './UiSelect.vue'

const props = withDefaults(
  defineProps<{
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
    disabled?: boolean
    pageSizeOptions?: number[]
    navigationLabel?: string
    summaryLabel?: string
    pageSizeLabel?: string
    previousLabel?: string
    nextLabel?: string
    pageLabel?: string
  }>(),
  {
    disabled: false,
    pageSizeOptions: () => [10, 20, 50, 100],
    navigationLabel: 'Pagination',
    summaryLabel: '{count} records',
    pageSizeLabel: 'Per page',
    previousLabel: 'Previous',
    nextLabel: 'Next',
    pageLabel: 'Page {page} / {total}',
  },
)

const emit = defineEmits<{
  pageChange: [page: number]
  pageSizeChange: [pageSize: number]
}>()

const displayPage = computed(() => props.totalPages === 0 ? 0 : props.page)
const summary = computed(() => props.summaryLabel.replace('{count}', String(props.totalCount)))
const pageSummary = computed(() => props.pageLabel
  .replace('{page}', String(displayPage.value))
  .replace('{total}', String(props.totalPages)))
</script>

<template>
  <nav class="ui-pagination" :aria-label="navigationLabel">
    <span class="ui-pagination__summary">{{ summary }}</span>
    <label>
      <span>{{ pageSizeLabel }}</span>
      <UiSelect
        :model-value="pageSize"
        :disabled="disabled"
        @update:model-value="emit('pageSizeChange', Number($event))"
      >
        <option v-for="option in pageSizeOptions" :key="option" :value="option">
          {{ option }}
        </option>
      </UiSelect>
    </label>
    <div class="ui-pagination__controls">
      <button
        type="button"
        :disabled="disabled || page <= 1"
        @click="emit('pageChange', page - 1)"
      >
        {{ previousLabel }}
      </button>
      <span>{{ pageSummary }}</span>
      <button
        type="button"
        :disabled="disabled || totalPages === 0 || page >= totalPages"
        @click="emit('pageChange', page + 1)"
      >
        {{ nextLabel }}
      </button>
    </div>
  </nav>
</template>

<style scoped>
.ui-pagination {
  display: flex;
  min-height: 44px;
  align-items: center;
  justify-content: flex-end;
  gap: 18px;
  color: var(--kk-text-muted);
  font-size: .8rem;
}

.ui-pagination label,
.ui-pagination__controls {
  display: flex;
  align-items: center;
  gap: 8px;
}

.ui-pagination :deep(.ui-select),
.ui-pagination button {
  min-height: 32px;
  padding: 5px 10px;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: 6px;
  font: inherit;
}

.ui-pagination button {
  cursor: pointer;
  font-weight: 650;
}

.ui-pagination button:disabled {
  cursor: not-allowed;
  opacity: .5;
}

@media (max-width: 620px) {
  .ui-pagination {
    align-items: stretch;
    flex-direction: column;
    gap: 10px;
  }

  .ui-pagination label,
  .ui-pagination__controls {
    justify-content: space-between;
  }
}
</style>
