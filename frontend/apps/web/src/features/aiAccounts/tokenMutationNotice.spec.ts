import { beforeEach, describe, expect, it } from 'vitest'
import { useSaveNotice } from '../../composables/useSaveNotice'
import { showTokenCreatedNotice, showTokenRecreatedNotice } from './tokenMutationNotice'

describe('AI token mutation notice', () => {
  beforeEach(() => useSaveNotice().clearSaveNotice())

  it('records successful token creation and re-creation in the global save notices', () => {
    const translate = (key: string): string => key === 'system.aiAccounts.record'
      ? 'AI account'
      : key

    showTokenCreatedNotice('ai-development', translate)
    showTokenRecreatedNotice('ai-development', translate)

    expect(useSaveNotice().saveNotices.value.map(({ mode, recordLabel, recordKey }) => ({
      mode,
      recordLabel,
      recordKey,
    }))).toEqual([
      { mode: 'created', recordLabel: 'AI account', recordKey: 'ai-development' },
      { mode: 'updated', recordLabel: 'AI account', recordKey: 'ai-development' },
    ])
  })
})
