import { describe, expect, it } from 'vitest'
import { useSaveNotice } from './useSaveNotice'

describe('useSaveNotice', () => {
  it('stores the created record key and optional one-time value', () => {
    const { saveNotices, showCreated } = useSaveNotice()

    showCreated('reviewer.one', 'initial-password')

    expect(saveNotices.value).toEqual([{
      id: expect.any(Number),
      mode: 'created',
      recordKey: 'reviewer.one',
      initialPassword: 'initial-password',
    }])
  })

  it('appends independent notices instead of replacing an active notice', () => {
    const { saveNotices, showCreated, showUpdated } = useSaveNotice()
    showCreated('reviewer.one', 'initial-password')

    showUpdated('reviewer.two')

    expect(saveNotices.value).toHaveLength(2)
    expect(saveNotices.value[1]).toEqual({
      id: expect.any(Number),
      mode: 'updated',
      recordKey: 'reviewer.two',
    })
    expect(saveNotices.value[0]?.initialPassword).toBe('initial-password')
    expect(saveNotices.value[1]).not.toHaveProperty('initialPassword')
    expect(saveNotices.value[0]?.id).not.toBe(saveNotices.value[1]?.id)
  })

  it('clears only the selected notice while keeping the rest of the stack', () => {
    const { saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()
    showCreated('reviewer.one')
    showUpdated('reviewer.two')
    const createdNoticeId = saveNotices.value[0]!.id

    clearSaveNotice(createdNoticeId)

    expect(saveNotices.value).toHaveLength(1)
    expect(saveNotices.value[0]?.recordKey).toBe('reviewer.two')
  })

  it('can clear the complete notice stack', () => {
    const { saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()
    showCreated('reviewer.one')
    showUpdated('reviewer.two')
    clearSaveNotice()

    expect(saveNotices.value).toEqual([])
  })
})
