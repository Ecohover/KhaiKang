<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ArrowRight, FolderKanban, Plus, X } from '@lucide/vue'
import { RouterLink, useRouter } from 'vue-router'
import { UiButton, UiField } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ProjectResponse } from '../api/contracts'
import { PROJECT_CREATE_PERMISSION } from '../navigation'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const projects = ref<ProjectResponse[]>([])
const loading = ref(true)
const loadingError = ref('')
const showCreateForm = ref(false)
const creating = ref(false)
const createError = ref('')
const code = ref('')
const name = ref('')
const description = ref('')

const canCreate = computed(() =>
  auth.user?.systemPermissions.includes(PROJECT_CREATE_PERMISSION) ?? false,
)
const codeError = computed(() => {
  if (!code.value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$/.test(code.value)
    ? ''
    : '只能使用英文字母、數字、連字號或底線。'
})

onMounted(loadProjects)

async function loadProjects(): Promise<void> {
  loading.value = true
  loadingError.value = ''
  try {
    const result = await apiClient.listProjects()
    if (result.error) {
      loadingError.value = problemMessage(result.error, '無法載入專案，請稍後再試。')
      return
    }
    projects.value = result.data ?? []
  } catch {
    loadingError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    loading.value = false
  }
}

function closeCreateForm(): void {
  showCreateForm.value = false
  createError.value = ''
  code.value = ''
  name.value = ''
  description.value = ''
}

async function createProject(): Promise<void> {
  if (!code.value.trim() || !name.value.trim() || codeError.value) return

  creating.value = true
  createError.value = ''
  try {
    const result = await apiClient.createProject(
      {
        code: code.value.trim(),
        name: name.value.trim(),
        description: description.value.trim() || null,
      },
      await secureHeaders(),
    )
    if (!result.data) {
      createError.value = problemMessage(result.error, '建立專案失敗，請稍後再試。')
      return
    }

    closeCreateForm()
    await router.push({ name: 'project-detail', params: { projectId: result.data.id } })
  } catch {
    createError.value = '無法連線到伺服器，請稍後再試。'
  } finally {
    creating.value = false
  }
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'medium' }).format(new Date(value))
}
</script>

<template>
  <section class="projects-page">
    <header class="page-heading">
      <div>
        <p class="eyebrow">Project management</p>
        <h2>專案</h2>
        <p>管理你參與的專案與基本設定。</p>
      </div>
      <UiButton v-if="canCreate" @click="showCreateForm = true">
        <Plus :size="18" aria-hidden="true" />
        建立專案
      </UiButton>
    </header>

    <form v-if="showCreateForm" class="create-panel" @submit.prevent="createProject">
      <div class="create-panel__header">
        <div>
          <h3>建立新專案</h3>
          <p>建立後，你會自動成為這個專案的 Owner。</p>
        </div>
        <button type="button" aria-label="關閉建立表單" @click="closeCreateForm">
          <X :size="20" aria-hidden="true" />
        </button>
      </div>

      <div class="create-panel__fields">
        <UiField
          id="project-code"
          v-model="code"
          label="專案代號"
          :disabled="creating"
          :error="codeError"
        />
        <UiField
          id="project-name"
          v-model="name"
          label="專案名稱"
          :disabled="creating"
        />
        <label class="text-area-field">
          <span>專案說明</span>
          <textarea v-model="description" rows="4" maxlength="4000" :disabled="creating" />
        </label>
      </div>

      <p v-if="createError" class="form-error" role="alert">{{ createError }}</p>
      <div class="create-panel__actions">
        <UiButton variant="secondary" :disabled="creating" @click="closeCreateForm">
          取消
        </UiButton>
        <UiButton
          type="submit"
          :loading="creating"
          :disabled="!code.trim() || !name.trim() || Boolean(codeError)"
        >
          建立專案
        </UiButton>
      </div>
    </form>

    <p v-if="loading" class="state-panel" aria-live="polite">正在載入專案…</p>
    <div v-else-if="loadingError" class="state-panel state-panel--error" role="alert">
      <p>{{ loadingError }}</p>
      <UiButton variant="secondary" @click="loadProjects">重新載入</UiButton>
    </div>
    <div v-else-if="projects.length" class="project-grid">
      <RouterLink
        v-for="project in projects"
        :key="project.id"
        :to="{ name: 'project-detail', params: { projectId: project.id } }"
        class="project-card"
      >
        <div class="project-card__icon">
          <FolderKanban :size="22" aria-hidden="true" />
        </div>
        <div class="project-card__body">
          <div class="project-card__title">
            <div>
              <span>{{ project.code }}</span>
              <h3>{{ project.name }}</h3>
            </div>
            <span class="status-badge" :class="`status-badge--${project.status}`">
              {{ project.status === 'active' ? '啟用中' : '已停用' }}
            </span>
          </div>
          <p>{{ project.description || '尚未填寫專案說明。' }}</p>
          <footer>
            <span>{{ project.currentUserRoles.join(' · ') }}</span>
            <span>更新於 {{ formatDate(project.updatedAt) }}</span>
            <ArrowRight :size="17" aria-hidden="true" />
          </footer>
        </div>
      </RouterLink>
    </div>
    <div v-else class="empty-state">
      <FolderKanban :size="30" aria-hidden="true" />
      <h3>目前沒有可用的專案</h3>
      <p>{{ canCreate ? '建立第一個專案，開始整理工作內容。' : '請聯絡系統管理員將你加入專案。' }}</p>
      <UiButton v-if="canCreate" @click="showCreateForm = true">
        <Plus :size="18" aria-hidden="true" />
        建立專案
      </UiButton>
    </div>
  </section>
</template>

<style scoped>
.projects-page {
  display: grid;
  gap: 28px;
}

.page-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
}

.page-heading h2,
.create-panel h3,
.project-card h3,
.empty-state h3 {
  margin: 0;
}

.page-heading h2 {
  margin-top: 3px;
  font-size: clamp(1.65rem, 3vw, 2.2rem);
}

.page-heading > div > p:last-child,
.create-panel__header p,
.empty-state p {
  margin: 7px 0 0;
  color: var(--kk-text-muted);
}

.eyebrow {
  margin: 0;
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 750;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.create-panel {
  display: grid;
  gap: 22px;
  padding: 24px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  box-shadow: var(--kk-shadow);
}

.create-panel__header,
.create-panel__actions {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.create-panel__header > button {
  display: grid;
  width: 36px;
  height: 36px;
  padding: 0;
  place-items: center;
  color: var(--kk-text-muted);
  background: transparent;
  border: 0;
  border-radius: var(--kk-radius);
}

.create-panel__fields {
  display: grid;
  grid-template-columns: minmax(180px, 0.7fr) minmax(240px, 1.3fr);
  gap: 18px;
}

.text-area-field {
  display: grid;
  grid-column: 1 / -1;
  gap: 7px;
  font-size: 0.875rem;
  font-weight: 650;
}

.text-area-field textarea {
  width: 100%;
  padding: 11px;
  resize: vertical;
  color: var(--kk-text);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border-strong);
  border-radius: var(--kk-radius);
  font: inherit;
}

.create-panel__actions {
  justify-content: flex-end;
}

.project-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 330px), 1fr));
  gap: 16px;
}

.project-card {
  display: flex;
  min-width: 0;
  gap: 15px;
  padding: 20px;
  color: inherit;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  text-decoration: none;
  transition: border-color 140ms ease, box-shadow 140ms ease, transform 140ms ease;
}

.project-card:hover {
  border-color: var(--kk-border-strong);
  box-shadow: var(--kk-shadow);
  transform: translateY(-2px);
}

.project-card__icon {
  display: grid;
  width: 42px;
  height: 42px;
  flex: 0 0 auto;
  place-items: center;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: var(--kk-radius);
}

.project-card__body {
  display: grid;
  min-width: 0;
  flex: 1;
  gap: 15px;
}

.project-card__title,
.project-card footer {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.project-card__title > div > span {
  color: var(--kk-text-muted);
  font-size: 0.72rem;
  font-weight: 750;
  letter-spacing: 0.06em;
}

.project-card h3 {
  margin-top: 3px;
  font-size: 1rem;
}

.project-card__body > p {
  display: -webkit-box;
  min-height: 42px;
  margin: 0;
  overflow: hidden;
  color: var(--kk-text-muted);
  font-size: 0.875rem;
  line-height: 1.5;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.project-card footer {
  align-items: center;
  justify-content: flex-start;
  color: var(--kk-text-muted);
  font-size: 0.75rem;
}

.project-card footer span:nth-child(2) {
  margin-left: auto;
}

.status-badge {
  flex: 0 0 auto;
  padding: 4px 7px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 999px;
  font-size: 0.72rem;
  font-weight: 700;
}

.status-badge--inactive {
  color: var(--kk-text-muted);
  background: var(--kk-surface-subtle);
}

.state-panel,
.empty-state {
  margin: 0;
  padding: 42px 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px dashed var(--kk-border-strong);
  border-radius: var(--kk-radius);
}

.state-panel--error {
  color: var(--kk-danger);
}

.empty-state {
  display: grid;
  place-items: center;
  gap: 10px;
}

@media (max-width: 620px) {
  .page-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .create-panel__fields {
    grid-template-columns: 1fr;
  }
}
</style>
