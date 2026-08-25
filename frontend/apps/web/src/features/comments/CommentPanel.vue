<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ChevronLeft, ChevronRight, LoaderCircle, MessageSquareText, RefreshCw, Send } from '@lucide/vue'
import { UiButton, UiMarkdownContent } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { apiClient, secureHeaders } from '../../api/client'
import type { CommentResourceType } from '../../api/contracts'
import { useSaveNotice } from '../../composables/useSaveNotice'
import { validateCommentMarkdown } from './commentPolicy'
import { createCommentsController } from './useComments'

const props = defineProps<{
  resourceType: CommentResourceType
  resourceId: string
}>()
const { t, d } = useI18n()
const { showCreated } = useSaveNotice()
const previewing = ref(false)
const controller = createCommentsController({
  list: (page) => apiClient.listComments(props.resourceType, props.resourceId, page),
  create: async (content, idempotencyKey) => apiClient.createComment({
    resourceType: props.resourceType,
    resourceId: props.resourceId,
    content,
    idempotencyKey,
  }, await secureHeaders()),
  createIdempotencyKey: () => crypto.randomUUID(),
})
const state = controller.state
const policyError = computed(() => validateCommentMarkdown(state.content))
const validationMessage = computed(() => {
  if (!state.validationError && !policyError.value) return ''
  const code = state.validationError || policyError.value
  return t(code === 'required'
    ? 'common.comments.validation.required'
    : code === 'too_long'
      ? 'common.comments.validation.tooLong'
      : 'common.comments.validation.unsafe')
})

async function submit(): Promise<void> {
  if (await controller.submit()) {
    previewing.value = false
    showCreated(t('common.comments.record'), t('common.comments.record'))
  }
}

function actorDescription(accountType: 'human' | 'ai_agent', fixedRole: string | null, owner: string | null): string {
  if (accountType === 'human') return t('common.comments.human')
  return [t('common.comments.ai'), fixedRole, owner ? t('common.comments.owner', { owner }) : '']
    .filter(Boolean)
    .join(' · ')
}

onMounted(() => controller.load(1))
</script>

<template>
  <section class="comment-panel" aria-labelledby="comment-panel-title">
    <header>
      <div>
        <MessageSquareText :size="20" aria-hidden="true" />
        <div><h3 id="comment-panel-title">{{ t('common.comments.title') }}</h3><p>{{ t('common.comments.description') }}</p></div>
      </div>
      <UiButton v-if="state.loadFailed" variant="secondary" type="button" @click="controller.reload">
        <RefreshCw :size="16" aria-hidden="true" />{{ t('common.comments.reload') }}
      </UiButton>
    </header>

    <form v-if="state.canCreate" class="comment-editor" @submit.prevent="submit">
      <div class="editor-tabs" role="tablist" :aria-label="t('common.comments.mode')">
        <button type="button" role="tab" :aria-selected="!previewing" @click="previewing = false">{{ t('common.comments.edit') }}</button>
        <button type="button" role="tab" :aria-selected="previewing" :disabled="Boolean(policyError)" @click="previewing = true">{{ t('common.comments.preview') }}</button>
      </div>
      <label v-if="!previewing" for="comment-content" class="sr-only">{{ t('common.comments.content') }}</label>
      <textarea
        v-if="!previewing"
        id="comment-content"
        v-model="state.content"
        rows="6"
        :aria-invalid="Boolean(validationMessage)"
        :aria-describedby="validationMessage ? 'comment-validation' : undefined"
        :placeholder="t('common.comments.placeholder')"
        :disabled="state.submitting"
      />
      <div v-else class="comment-preview"><UiMarkdownContent :content="state.content" /></div>
      <div class="editor-footer">
        <div>
          <span>{{ Array.from(state.content).length }} / 10000</span>
          <p v-if="validationMessage" id="comment-validation" role="alert">{{ validationMessage }}</p>
          <p v-else-if="state.submitError" role="alert">{{ t('common.comments.createFailed') }}</p>
        </div>
        <UiButton type="submit" :loading="state.submitting" :disabled="Boolean(policyError) || state.submitting">
          <Send :size="16" aria-hidden="true" />{{ state.submitError ? t('common.comments.retryCreate') : t('common.comments.create') }}
        </UiButton>
      </div>
    </form>

    <div v-if="state.loading" class="comment-state" role="status"><LoaderCircle class="spin" :size="20" aria-hidden="true" />{{ t('common.comments.loading') }}</div>
    <div v-else-if="state.loadFailed" class="comment-state comment-state--error" role="alert">
      <p>{{ t('common.comments.busy') }}</p>
    </div>
    <p v-else-if="!state.items.length" class="comment-state">{{ t('common.comments.empty') }}</p>
    <ol v-else class="comment-list">
      <li v-for="comment in state.items" :key="comment.id">
        <header>
          <div><strong>{{ comment.authorDisplayName }}</strong><span :class="['actor-badge', `actor-badge--${comment.accountType}`]">{{ actorDescription(comment.accountType, comment.fixedRole, comment.ownerDisplayName) }}</span></div>
          <time :datetime="comment.createdAt">{{ d(new Date(comment.createdAt), 'medium') }}</time>
        </header>
        <UiMarkdownContent :content="comment.content" />
      </li>
    </ol>
    <nav v-if="!state.loadFailed && (state.page > 1 || state.hasNextPage)" class="comment-pagination" :aria-label="t('common.comments.pagination')">
      <UiButton variant="secondary" type="button" :disabled="state.page <= 1 || state.loading" @click="controller.load(state.page - 1)"><ChevronLeft :size="16" aria-hidden="true" />{{ t('common.pagination.previous') }}</UiButton>
      <span>{{ t('common.comments.page', { page: state.page }) }}</span>
      <UiButton variant="secondary" type="button" :disabled="!state.hasNextPage || state.loading" @click="controller.load(state.page + 1)">{{ t('common.pagination.next') }}<ChevronRight :size="16" aria-hidden="true" /></UiButton>
    </nav>
  </section>
</template>

<style scoped>
.comment-panel{display:grid;gap:16px;padding:20px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.comment-panel>header,.comment-panel>header>div,.comment-list li>header,.comment-list li>header>div,.editor-footer,.comment-pagination{display:flex;align-items:center;justify-content:space-between;gap:12px}.comment-panel>header>div{align-items:flex-start;justify-content:flex-start}.comment-panel h3,.comment-panel p{margin:0}.comment-panel>header p{margin-top:3px;color:var(--kk-text-muted);font-size:.84rem}.comment-editor{display:grid}.editor-tabs{display:flex;width:fit-content;padding:0 4px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-bottom:0;border-radius:6px 6px 0 0}.editor-tabs button{min-height:32px;padding:5px 11px;color:var(--kk-text-muted);background:transparent;border:0;border-bottom:2px solid transparent;font:inherit;font-size:.78rem;font-weight:650;cursor:pointer}.editor-tabs button[aria-selected=true]{color:var(--kk-accent);background:var(--kk-surface);border-bottom-color:var(--kk-accent)}.comment-editor textarea,.comment-preview{min-height:150px;padding:12px 14px;border:1px solid var(--kk-border-strong);border-radius:0 6px 6px 6px;background:var(--kk-surface);font:inherit;line-height:1.55;resize:vertical}.comment-editor textarea:focus{border-color:var(--kk-accent);box-shadow:0 0 0 2px var(--kk-accent-soft);outline:0}.editor-footer{align-items:flex-start;padding-top:9px}.editor-footer>div{display:grid;gap:3px;color:var(--kk-text-muted);font-size:.75rem}.editor-footer p,.comment-state--error{color:var(--kk-danger)}.comment-state{display:flex;align-items:center;justify-content:center;gap:8px;padding:24px;color:var(--kk-text-muted);background:var(--kk-surface-subtle);border-radius:6px}.comment-list{display:grid;gap:10px;margin:0;padding:0;list-style:none}.comment-list li{display:grid;gap:10px;padding:14px;border:1px solid var(--kk-border);border-radius:6px}.comment-list li>header{align-items:flex-start}.comment-list li>header>div{justify-content:flex-start;flex-wrap:wrap}.comment-list time{color:var(--kk-text-muted);font-size:.76rem}.actor-badge{display:inline-flex;padding:2px 7px;border-radius:999px;background:var(--kk-surface-subtle);font-size:.72rem;font-weight:650}.actor-badge--ai_agent{color:var(--kk-accent);background:var(--kk-accent-soft)}.comment-pagination span{color:var(--kk-text-muted);font-size:.82rem}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}.spin{animation:spin 1s linear infinite}@keyframes spin{to{transform:rotate(360deg)}}@media(max-width:640px){.comment-panel>header,.editor-footer,.comment-pagination{align-items:stretch;flex-direction:column}.comment-pagination span{text-align:center}.comment-list li>header{flex-direction:column}}
</style>
