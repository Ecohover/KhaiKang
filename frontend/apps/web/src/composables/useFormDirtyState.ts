import { computed, ref } from 'vue'

export function useFormDirtyState(getSnapshot: () => unknown) {
  const cleanSnapshot = ref(serialize(getSnapshot()))

  const isDirty = computed(() => serialize(getSnapshot()) !== cleanSnapshot.value)

  function markClean(): void {
    cleanSnapshot.value = serialize(getSnapshot())
  }

  return {
    isDirty,
    markClean,
  }
}

function serialize(value: unknown): string {
  return JSON.stringify(value)
}
