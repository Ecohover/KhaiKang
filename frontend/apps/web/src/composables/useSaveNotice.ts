import { computed, ref } from 'vue'

export interface SaveNotice {
  id: number
  mode: 'created' | 'updated'
  recordLabel: string
  recordKey: string
}

let nextSaveNoticeId = 1
const saveNotices = ref<SaveNotice[]>([])

export function useSaveNotice() {
  const saveNotice = computed(() => saveNotices.value.at(-1))

  function showCreated(recordLabel: string, recordKey: string): void {
    saveNotices.value.push({
      id: nextSaveNoticeId++,
      mode: 'created',
      recordLabel,
      recordKey,
    })
  }

  function showUpdated(recordLabel: string, recordKey: string): void {
    saveNotices.value.push({
      id: nextSaveNoticeId++,
      mode: 'updated',
      recordLabel,
      recordKey,
    })
  }

  function clearSaveNotice(id?: number): void {
    if (id === undefined) {
      saveNotices.value = []
      return
    }

    saveNotices.value = saveNotices.value.filter((notice) => notice.id !== id)
  }

  return {
    saveNotice,
    saveNotices,
    showCreated,
    showUpdated,
    clearSaveNotice,
  }
}
