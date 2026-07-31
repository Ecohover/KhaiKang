import { computed, ref } from 'vue'

export interface SaveNotice {
  id: number
  mode: 'created' | 'updated'
  recordKey: string
  initialPassword?: string
}

let nextSaveNoticeId = 1

export function useSaveNotice() {
  const saveNotices = ref<SaveNotice[]>([])
  const saveNotice = computed(() => saveNotices.value.at(-1))

  function showCreated(recordKey: string, initialPassword?: string): void {
    saveNotices.value.push({
      id: nextSaveNoticeId++,
      mode: 'created',
      recordKey,
      ...(initialPassword ? { initialPassword } : {}),
    })
  }

  function showUpdated(recordKey: string): void {
    saveNotices.value.push({
      id: nextSaveNoticeId++,
      mode: 'updated',
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
