<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { Plus, Trash2, UserCheck, UserPlus } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import { UiButton, UiPagination } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { useSaveNotice } from '../composables/useSaveNotice'
import type { TestWorkspaceRole } from '../api/contracts'
import SharedSearchField from './SharedSearchField.vue'

export interface ResourceMemberItem {
  id: string
  username: string
  role: string
  joinedAt: string
  version: number
}

export interface RoleOption {
  code: string
  label: string
}

const props = withDefaults(
  defineProps<{
    resourceType: 'project' | 'test-workspace'
    resourceId: string
    title?: string
    description?: string
    canAdd?: boolean
    canEditRole?: boolean
    canRemove?: boolean
    addMemberPlaceholder?: string
    showAddAction?: boolean
  }>(),
  {
    canAdd: true,
    canEditRole: true,
    canRemove: true,
    showAddAction: true,
  },
)

const { d, t } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()

const members = ref<ResourceMemberItem[]>([])
const roles = ref<RoleOption[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')

const searchQuery = ref('')
const isAddingMember = defineModel<boolean>('adding', { default: false })
const newUsername = ref('')
const newRole = ref('')

// Pagination state
const page = ref(1)
const pageSize = ref(10)

// Default static roles for test workspace
const defaultWorkspaceRoles: RoleOption[] = [
  { code: 'owner', label: 'Owner' },
  { code: 'manager', label: 'Manager' },
  { code: 'tester', label: 'Tester' },
  { code: 'viewer', label: 'Viewer' },
]

// Filtered members based on search
const filteredMembers = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  if (!query) return members.value
  return members.value.filter(
    (m) => m.username.toLowerCase().includes(query) || m.role.toLowerCase().includes(query),
  )
})

// Paginated members slice
const paginatedMembers = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredMembers.value.slice(start, start + pageSize.value)
})

const totalCount = computed(() => filteredMembers.value.length)
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value) || 1)
const memberRecordLabel = computed(() => t(props.resourceType === 'project'
  ? 'common.members.projectRecord'
  : 'common.members.workspaceRecord'))
const displayTitle = computed(() => props.title ?? t('common.members.title'))
const displayDescription = computed(() => props.description ?? t('common.members.description'))
const usernamePlaceholder = computed(() => props.addMemberPlaceholder ?? t('common.members.addPlaceholder'))

// Reset page when search query changes
watch(searchQuery, () => {
  page.value = 1
})

onMounted(loadData)
watch(() => [props.resourceType, props.resourceId], loadData)

async function loadData(): Promise<void> {
  if (!props.resourceId) return
  loading.value = true
  error.value = ''
  try {
    if (props.resourceType === 'project') {
      const [memberRes, roleRes] = await Promise.all([
        apiClient.listProjectMembers(props.resourceId),
        apiClient.listProjectRoles(props.resourceId),
      ])
      if (memberRes.data && roleRes.data) {
        members.value = memberRes.data.map((m) => ({
          id: m.id,
          username: m.username,
          role: m.roleCodes[0] ?? 'contributor',
          joinedAt: d(new Date(m.joinedAt), 'dateTime'),
          version: m.version,
        }))
        roles.value = roleRes.data.map((r) => ({ code: r.code, label: r.name }))
      } else {
        error.value = problemMessage(memberRes.error ?? roleRes.error, t('common.members.loadFailed'))
      }
    } else {
      const memberRes = await apiClient.listTestWorkspaceMembers(props.resourceId)
      if (memberRes.data) {
        members.value = memberRes.data.map((m) => ({
          id: m.id,
          username: m.username,
          role: m.role,
          joinedAt: d(new Date(m.joinedAt), 'dateTime'),
          version: m.version,
        }))
        roles.value = defaultWorkspaceRoles
      } else {
        error.value = problemMessage(memberRes.error, t('common.members.loadFailed'))
      }
    }

    if (roles.value.length > 0 && roles.value[0] && !newRole.value) {
      newRole.value = roles.value[0].code
    }
  } catch {
    error.value = t('common.errors.connectionFailed')
  } finally {
    loading.value = false
  }
}

async function handleAddMember(): Promise<void> {
  if (!newUsername.value.trim() || !props.resourceId) return
  saving.value = true
  error.value = ''
  try {
    if (props.resourceType === 'project') {
      const result = await apiClient.addProjectMember(
        props.resourceId,
        { username: newUsername.value.trim(), roleCodes: [newRole.value] },
        await secureHeaders(),
      )
      if (result.data) {
        showCreated(memberRecordLabel.value, result.data.username)
        newUsername.value = ''
        isAddingMember.value = false
        await loadData()
      } else {
        error.value = problemMessage(result.error, t('common.members.addFailed'))
      }
    } else {
      const result = await apiClient.addTestWorkspaceMember(
        props.resourceId,
        { username: newUsername.value.trim(), role: newRole.value as TestWorkspaceRole },
        await secureHeaders(),
      )
      if (result.data) {
        showCreated(memberRecordLabel.value, result.data.username)
        newUsername.value = ''
        isAddingMember.value = false
        await loadData()
      } else {
        error.value = problemMessage(result.error, t('common.members.addFailed'))
      }
    }
  } catch {
    error.value = t('common.errors.connectionFailed')
  } finally {
    saving.value = false
  }
}

async function handleRoleChange(member: ResourceMemberItem, event: Event): Promise<void> {
  const target = event.target as HTMLSelectElement
  const selectedRole = target.value
  if (!props.resourceId) return

  saving.value = true
  error.value = ''
  try {
    if (props.resourceType === 'project') {
      const result = await apiClient.updateProjectMemberRoles(
        props.resourceId,
        member.id,
        { roleCodes: [selectedRole], version: member.version },
        await secureHeaders(),
      )
      if (result.data) {
        showUpdated(memberRecordLabel.value, result.data.username)
        await loadData()
      } else {
        error.value = problemMessage(result.error, t('common.members.updateFailed'))
      }
    } else {
      const result = await apiClient.updateTestWorkspaceMember(
        props.resourceId,
        member.id,
        { role: selectedRole as TestWorkspaceRole, version: member.version },
        await secureHeaders(),
      )
      if (result.data) {
        showUpdated(memberRecordLabel.value, result.data.username)
        await loadData()
      } else {
        error.value = problemMessage(result.error, t('common.members.updateFailed'))
      }
    }
  } catch {
    error.value = t('common.errors.connectionFailed')
  } finally {
    saving.value = false
  }
}

async function handleRemoveMember(member: ResourceMemberItem): Promise<void> {
  if (!window.confirm(t('common.members.removeConfirm', { username: member.username }))) return
  if (!props.resourceId) return

  saving.value = true
  error.value = ''
  try {
    if (props.resourceType === 'project') {
      const result = await apiClient.removeProjectMember(
        props.resourceId,
        member.id,
        member.version,
        await secureHeaders(),
      )
      if (result.error) {
        error.value = problemMessage(result.error, t('common.members.removeFailed'))
      } else {
        await loadData()
      }
    } else {
      const result = await apiClient.removeTestWorkspaceMember(
        props.resourceId,
        member.id,
        member.version,
        await secureHeaders(),
      )
      if (result.error) {
        error.value = problemMessage(result.error, t('common.members.removeFailed'))
      } else {
        await loadData()
      }
    }
  } catch {
    error.value = t('common.errors.connectionFailed')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="resource-member-manager">
    <!-- PANEL HEADER -->
    <header class="panel-header">
      <div>
        <h3>{{ displayTitle }}</h3>
        <p v-if="displayDescription" class="sub-desc">{{ displayDescription }}</p>
      </div>
      <span class="member-count-badge">{{ t('common.members.count', { count: totalCount }) }}</span>
    </header>

    <div v-if="error" class="error-banner" role="alert">
      {{ error }}
    </div>

    <!-- TOOLBAR: SEARCH & ACTION AREA (清單上面的查詢區塊與新增成員按鈕) -->
    <div class="list-toolbar">
      <SharedSearchField
        v-model="searchQuery"
        :placeholder="t('common.members.searchPlaceholder')"
        :clear-label="t('common.search.clear')"
      />

      <UiButton
        v-if="canAdd && showAddAction"
        @click="isAddingMember = !isAddingMember"
      >
        <Plus :size="16" aria-hidden="true" />
        {{ isAddingMember ? t('common.members.cancelAdd') : t('common.members.add') }}
      </UiButton>
    </div>

    <!-- EXPANDABLE ADD MEMBER FORM PANEL -->
    <div v-if="canAdd && isAddingMember" class="add-member-card">
      <h4><UserPlus :size="15" /> {{ t('common.members.addDetails') }}</h4>
      <div class="add-member-form">
        <input
          v-model="newUsername"
          class="inline-input"
          :placeholder="usernamePlaceholder"
          :disabled="saving"
          @keyup.enter="handleAddMember"
        />
        <select v-if="roles.length" v-model="newRole" class="inline-select" :disabled="saving">
          <option v-for="r in roles" :key="r.code" :value="r.code">
            {{ r.label }}
          </option>
        </select>
        <UiButton
          :disabled="saving || !newUsername.trim()"
          @click="handleAddMember"
        >
          <Plus :size="16" aria-hidden="true" />
          {{ saving ? t('common.settings.saving') : t('common.members.confirmAdd') }}
        </UiButton>
      </div>
    </div>

    <!-- MEMBER LIST TABLE -->
    <div v-if="loading" class="list-state">{{ t('common.members.loading') }}</div>
    <div v-else-if="paginatedMembers.length" class="member-table-container">
      <table class="member-table">
        <thead>
          <tr>
            <th>{{ t('common.fields.username') }}</th>
            <th>{{ t('common.fields.role') }}</th>
            <th>{{ t('common.fields.joinedAt') }}</th>
            <th v-if="canRemove" class="text-right">{{ t('common.actions.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="member in paginatedMembers" :key="member.id">
            <td class="user-cell">
              <div class="user-avatar"><UserCheck :size="15" /></div>
              <span class="username">{{ member.username }}</span>
            </td>
            <td class="role-cell">
              <select
                v-if="canEditRole && roles.length"
                :value="member.role"
                class="inline-select role-select"
                :disabled="saving"
                @change="handleRoleChange(member, $event)"
              >
                <option v-for="r in roles" :key="r.code" :value="r.code">
                  {{ r.label }}
                </option>
              </select>
              <span v-else class="role-badge">{{ member.role }}</span>
            </td>
            <td class="date-cell">
              {{ member.joinedAt || '-' }}
            </td>
            <td v-if="canRemove" class="text-right">
              <button
                type="button"
                class="btn-subtle btn-danger-subtle"
                :disabled="saving"
                @click="handleRemoveMember(member)"
              >
                <Trash2 :size="13" /> {{ t('common.members.remove') }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="list-state empty">
      {{ searchQuery ? t('common.members.emptySearch') : t('common.members.empty') }}
    </div>

    <!-- PAGINATION (成員頁面分頁區塊) -->
    <footer v-if="totalCount > 0" class="pagination-footer">
      <UiPagination
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-count="totalCount"
        :total-pages="totalPages"
      />
    </footer>
  </div>
</template>

<style scoped>
.resource-member-manager {
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #ffffff;
  border-radius: 10px;
  padding: 20px 24px;
  border: 1px solid var(--kk-border);
  width: 100%;
  box-sizing: border-box;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.panel-header h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--kk-text);
}

.sub-desc {
  margin: 4px 0 0;
  font-size: 0.85rem;
  color: var(--kk-text-muted);
}

.member-count-badge {
  font-size: 0.78rem;
  padding: 2px 8px;
  background: #f3f4f6;
  color: #4b5563;
  border-radius: 12px;
}

.error-banner {
  padding: 10px 14px;
  background: #fef2f2;
  color: #dc2626;
  border-radius: 6px;
  font-size: 0.85rem;
  border: 1px solid #fecaca;
}

.list-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
}

.add-member-card {
  border: 1px solid var(--kk-border);
  background: #f9fafb;
  border-radius: 8px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.add-member-card h4 {
  margin: 0;
  font-size: 0.88rem;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--kk-text);
}

.add-member-form {
  display: flex;
  align-items: center;
  gap: 10px;
}

.inline-input {
  height: 32px;
  padding: 0 10px;
  font-size: 0.85rem;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  flex: 1;
  background: white;
}

.inline-select {
  height: 32px;
  padding: 0 8px;
  font-size: 0.85rem;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  background: white;
}

.member-table-container {
  overflow-x: auto;
  border: 1px solid var(--kk-border);
  border-radius: 8px;
}

.member-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.86rem;
}

.member-table th {
  background: #f9fafb;
  padding: 10px 14px;
  text-align: left;
  font-weight: 600;
  color: var(--kk-text-muted);
  border-bottom: 1px solid var(--kk-border);
}

.member-table td {
  padding: 10px 14px;
  border-bottom: 1px solid var(--kk-border);
  vertical-align: middle;
}

.member-table tr:last-child td {
  border-bottom: none;
}

.user-cell {
  display: flex;
  align-items: center;
  gap: 10px;
}

.user-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #e0f2fe;
  color: #0369a1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.username {
  font-weight: 600;
  color: var(--kk-text);
}

.role-select {
  font-weight: 500;
}

.role-badge {
  padding: 2px 8px;
  background: #eef2ff;
  color: #3730a3;
  border-radius: 4px;
  font-size: 0.78rem;
  font-weight: 600;
}

.date-cell {
  color: var(--kk-text-muted);
  font-size: 0.82rem;
}

.text-right {
  text-align: right;
}

.list-state {
  padding: 24px;
  text-align: center;
  color: var(--kk-text-muted);
  font-size: 0.88rem;
}

.pagination-footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 4px;
}

.btn-subtle,
.btn-danger-subtle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 32px;
  padding: 0 12px;
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1;
  border-radius: 6px;
  cursor: pointer;
  white-space: nowrap;
}

.btn-subtle {
  border: 1px solid var(--kk-border);
  background: #ffffff;
  color: var(--kk-text);
}

.btn-danger-subtle {
  border: 1px solid #fecaca;
  background: #ffffff;
  color: #dc2626;
}
.btn-danger-subtle:hover {
  background: #fef2f2;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 640px) {
  .list-toolbar {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
