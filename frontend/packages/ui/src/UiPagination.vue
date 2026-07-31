<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
    disabled?: boolean
    pageSizeOptions?: number[]
  }>(),
  {
    disabled: false,
    pageSizeOptions: () => [10, 20, 50, 100],
  },
)

const emit = defineEmits<{
  pageChange: [page: number]
  pageSizeChange: [pageSize: number]
}>()

const displayPage = computed(() => props.totalPages === 0 ? 0 : props.page)
</script>

<template>
  <nav class="ui-pagination" aria-label="分頁導覽">
    <span class="ui-pagination__summary">共 {{ totalCount }} 筆</span>
    <label>
      <span>每頁</span>
      <select
        :value="pageSize"
        :disabled="disabled"
        @change="emit('pageSizeChange', Number(($event.target as HTMLSelectElement).value))"
      >
        <option v-for="option in pageSizeOptions" :key="option" :value="option">
          {{ option }}
        </option>
      </select>
    </label>
    <div class="ui-pagination__controls">
      <button
        type="button"
        :disabled="disabled || page <= 1"
        @click="emit('pageChange', page - 1)"
      >
        上一頁
      </button>
      <span>第 {{ displayPage }} / {{ totalPages }} 頁</span>
      <button
        type="button"
        :disabled="disabled || totalPages === 0 || page >= totalPages"
        @click="emit('pageChange', page + 1)"
      >
        下一頁
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

.ui-pagination select,
.ui-pagination button {
  min-height: 34px;
  padding: 6px 10px;
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
