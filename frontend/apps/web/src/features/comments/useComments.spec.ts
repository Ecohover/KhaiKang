import { describe, expect, it, vi } from 'vitest'
import type { ApiResult } from '../../api/client'
import type { CommentPageResponse, CommentResponse } from '../../api/contracts'
import { createCommentsController } from './useComments'
import { validateCommentMarkdown } from './commentPolicy'

const emptyPage: CommentPageResponse = {
  items: [],
  page: 1,
  hasNextPage: false,
  canCreate: true,
}

describe('comment controller', () => {
  it('retries three times before exposing a load error and supports explicit reload', async () => {
    const list = vi.fn<() => Promise<ApiResult<CommentPageResponse>>>()
      .mockResolvedValueOnce({ error: { title: 'busy' } })
      .mockResolvedValueOnce({ error: { title: 'busy' } })
      .mockResolvedValueOnce({ error: { title: 'busy' } })
      .mockResolvedValueOnce({ error: { title: 'busy' } })
      .mockResolvedValueOnce({ data: emptyPage })
    const controller = createCommentsController({
      list,
      create: vi.fn<() => Promise<ApiResult<CommentResponse>>>(),
      createIdempotencyKey: () => 'comment-key',
    })

    await controller.load(1)
    expect(list).toHaveBeenCalledTimes(4)
    expect(controller.state.loadFailed).toBe(true)

    await controller.reload()
    expect(list).toHaveBeenCalledTimes(5)
    expect(controller.state.loadFailed).toBe(false)
  })

  it('reloads page one after create and reuses the idempotency key only for explicit retry', async () => {
    const created = { id: 'comment-id' } as CommentResponse
    const list = vi.fn<() => Promise<ApiResult<CommentPageResponse>>>()
      .mockResolvedValue({ data: emptyPage })
    const create = vi.fn<() => Promise<ApiResult<CommentResponse>>>()
      .mockResolvedValueOnce({ error: { title: 'busy' } })
      .mockResolvedValueOnce({ data: created })
    const controller = createCommentsController({
      list,
      create,
      createIdempotencyKey: () => 'comment-key',
    })
    controller.state.canCreate = true
    controller.state.content = 'Verification evidence.'

    await controller.submit()
    await controller.submit()

    expect(create).toHaveBeenNthCalledWith(1, 'Verification evidence.', 'comment-key')
    expect(create).toHaveBeenNthCalledWith(2, 'Verification evidence.', 'comment-key')
    expect(list).toHaveBeenLastCalledWith(1)
    expect(controller.state.page).toBe(1)
    expect(controller.state.content).toBe('')
  })
})

describe('comment Markdown policy', () => {
  it('accepts ordinary Markdown and rejects raw HTML, external images, and over 10000 Unicode characters', () => {
    expect(validateCommentMarkdown('## Result\n\n- Passed\n\nhttps://example.com')).toBeUndefined()
    expect(validateCommentMarkdown('<script>alert(1)</script>')).toBeDefined()
    expect(validateCommentMarkdown('![external](https://example.com/image.png)')).toBeDefined()
    expect(validateCommentMarkdown('😀'.repeat(10_001))).toBeDefined()
  })
})
