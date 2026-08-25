import { reactive } from 'vue'
import type { ApiResult } from '../../api/client'
import type { CommentPageResponse, CommentResponse } from '../../api/contracts'
import { validateCommentMarkdown } from './commentPolicy'

interface CommentsGateway {
  list(page: number): Promise<ApiResult<CommentPageResponse>>
  create(content: string, idempotencyKey: string): Promise<ApiResult<CommentResponse>>
  createIdempotencyKey(): string
}

interface CommentsState {
  items: CommentResponse[]
  page: number
  hasNextPage: boolean
  canCreate: boolean
  content: string
  loading: boolean
  submitting: boolean
  loadFailed: boolean
  submitError: string
  validationError: string
}

export function createCommentsController(gateway: CommentsGateway) {
  const state = reactive<CommentsState>({
    items: [],
    page: 1,
    hasNextPage: false,
    canCreate: false,
    content: '',
    loading: false,
    submitting: false,
    loadFailed: false,
    submitError: '',
    validationError: '',
  })
  let pendingSubmission: { content: string, idempotencyKey: string } | undefined

  async function load(page = state.page): Promise<void> {
    state.loading = true
    state.loadFailed = false
    for (let attempt = 0; attempt <= 3; attempt += 1) {
      try {
        const result = await gateway.list(page)
        if (result.data) {
          state.items = result.data.items
          state.page = result.data.page
          state.hasNextPage = result.data.hasNextPage
          state.canCreate = result.data.canCreate
          state.loading = false
          return
        }
      } catch {
        // A transport failure follows the same bounded retry policy as an API failure.
      }
    }
    state.loading = false
    state.loadFailed = true
  }

  async function reload(): Promise<void> {
    await load(state.page)
  }

  async function submit(): Promise<boolean> {
    const content = state.content
    const validationError = validateCommentMarkdown(content)
    state.validationError = validationError ?? ''
    if (validationError || !state.canCreate || state.submitting) return false

    const command = pendingSubmission?.content === content
      ? pendingSubmission
      : { content, idempotencyKey: gateway.createIdempotencyKey() }
    pendingSubmission = command
    state.submitting = true
    state.submitError = ''
    try {
      const result = await gateway.create(command.content, command.idempotencyKey)
      if (!result.data) {
        state.submitError = result.error?.detail ?? result.error?.title ?? 'create_failed'
        return false
      }
      pendingSubmission = undefined
      state.content = ''
      await load(1)
      return true
    } catch {
      state.submitError = 'create_failed'
      return false
    } finally {
      state.submitting = false
    }
  }

  return { state, load, reload, submit }
}
