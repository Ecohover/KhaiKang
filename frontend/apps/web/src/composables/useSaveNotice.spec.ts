import { beforeEach, describe, expect, it } from 'vitest'
import { useSaveNotice } from './useSaveNotice'

describe('useSaveNotice', () => {
  beforeEach(() => {
    useSaveNotice().clearSaveNotice()
  })

  it('stores the created record label and key', () => {
    const { saveNotices, showCreated } = useSaveNotice()

    showCreated('使用者帳號', 'reviewer.one')

    expect(saveNotices.value).toEqual([{
      id: expect.any(Number),
      mode: 'created',
      recordLabel: '使用者帳號',
      recordKey: 'reviewer.one',
    }])
  })

  it('appends independent notices instead of replacing an active notice', () => {
    const { saveNotices, showCreated, showUpdated } = useSaveNotice()
    showCreated('使用者帳號', 'reviewer.one')

    showUpdated('使用者帳號', 'reviewer.two')

    expect(saveNotices.value).toHaveLength(2)
    expect(saveNotices.value[1]).toEqual({
      id: expect.any(Number),
      mode: 'updated',
      recordLabel: '使用者帳號',
      recordKey: 'reviewer.two',
    })
    expect(saveNotices.value[0]?.id).not.toBe(saveNotices.value[1]?.id)
  })

  it('clears only the selected notice while keeping the rest of the stack', () => {
    const { saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()
    showCreated('使用者帳號', 'reviewer.one')
    showUpdated('使用者帳號', 'reviewer.two')
    const createdNoticeId = saveNotices.value[0]!.id

    clearSaveNotice(createdNoticeId)

    expect(saveNotices.value).toHaveLength(1)
    expect(saveNotices.value[0]?.recordKey).toBe('reviewer.two')
  })

  it('can clear the complete notice stack', () => {
    const { saveNotices, showCreated, showUpdated, clearSaveNotice } = useSaveNotice()
    showCreated('使用者帳號', 'reviewer.one')
    showUpdated('使用者帳號', 'reviewer.two')
    clearSaveNotice()

    expect(saveNotices.value).toEqual([])
  })
})
