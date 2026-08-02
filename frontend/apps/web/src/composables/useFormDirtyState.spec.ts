import { reactive } from 'vue'
import { describe, expect, it } from 'vitest'
import { useFormDirtyState } from './useFormDirtyState'

describe('useFormDirtyState', () => {
  it('becomes dirty when a tracked field changes', () => {
    const form = reactive({ title: '', assigneeAccountId: null as string | null })
    const { isDirty } = useFormDirtyState(() => ({ ...form }))

    form.title = 'First task'

    expect(isDirty.value).toBe(true)
  })

  it('marks the current values as the new clean baseline after save', () => {
    const form = reactive({ title: '' })
    const { isDirty, markClean } = useFormDirtyState(() => ({ ...form }))
    form.title = 'Saved task'

    markClean()

    expect(isDirty.value).toBe(false)
  })

  it('detects clearing a previously saved form for continue-create', () => {
    const form = reactive({ title: 'Saved task' })
    const { isDirty, markClean } = useFormDirtyState(() => ({ ...form }))
    markClean()

    form.title = ''

    expect(isDirty.value).toBe(true)
    markClean()
    expect(isDirty.value).toBe(false)
  })
})
