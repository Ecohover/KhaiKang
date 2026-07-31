<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, UserPlus, Trash2, UserCheck, Shield } from '@lucide/vue'
import { UiButton, UiField } from '@khaikang/ui'

export interface MemberItem {
  id: string
  username: string
  role: string
  status?: string
  joinedAt?: string
  raw?: any
}

export interface RoleOption {
  code: string
  label: string
}

const props = withDefaults(
  defineProps<{
    title: string
    description?: string
    members: MemberItem[]
    availableRoles: RoleOption[]
    canAdd?: boolean
    canEditRole?: boolean
    canRemove?: boolean
    addMemberPlaceholder?: string
    loading?: boolean
    saving?: boolean
  }>(),
  {
    description: '',
    canAdd: true,
    canEditRole: true,
    canRemove: true,
    addMemberPlaceholder: '請輸入使用者名稱',
    loading: false,
    saving: false,
  },
)

const emit = defineEmits<{
  (e: 'add-member', payload: { username: string; role: string }): void
  (e: 'update-role', payload: { memberId: string; role: string }): void
  (e: 'remove-member', memberId: string): void
}>()

const newUsername = ref('')
const newRole = ref('')

watch(
  () => props.availableRoles,
  (roles) => {
    if (roles && roles.length > 0 && roles[0] && (!newRole.value || !roles.some((r) => r.code === newRole.value))) {
      newRole.value = roles[0].code
    }
  },
  { immediate: true },
)

function handleAdd(): void {
  if (!newUsername.value.trim()) return
  emit('add-member', { username: newUsername.value.trim(), role: newRole.value })
  newUsername.value = ''
}

function handleRoleChange(memberId: string, event: Event): void {
  const target = event.target as HTMLSelectElement
  emit('update-role', { memberId, role: target.value })
}

function handleRemove(memberId: string): void {
  emit('remove-member', memberId)
}
</script>

<template>
  <div class="shared-member-management">
    <header class="panel-header">
      <div>
        <h3>{{ title }}</h3>
        <p v-if="description" class="sub-desc">{{ description }}</p>
      </div>
      <span class="member-count-badge">共 {{ members.length }} 位成員</span>
    </header>

    <!-- ADD MEMBER FORM -->
    <div v-if="canAdd" class="add-member-card">
      <h4><UserPlus :size="15" /> 新增成員</h4>
      <div class="add-member-form">
        <input
          v-model="newUsername"
          class="inline-input"
          :placeholder="addMemberPlaceholder"
          :disabled="saving"
          @keyup.enter="handleAdd"
        />
        <select v-if="availableRoles.length" v-model="newRole" class="inline-select" :disabled="saving">
          <option v-for="r in availableRoles" :key="r.code" :value="r.code">
            {{ r.label }}
          </option>
        </select>
        <button
          type="button"
          class="btn-primary"
          :disabled="saving || !newUsername.trim()"
          @click="handleAdd"
        >
          <Plus :size="14" /> {{ saving ? '處理中...' : '新增成員' }}
        </button>
      </div>
    </div>

    <!-- MEMBER LIST TABLE -->
    <div v-if="loading" class="list-state">載入成員列表中...</div>
    <div v-else-if="members.length" class="member-table-container">
      <table class="member-table">
        <thead>
          <tr>
            <th>使用者名稱</th>
            <th>角色權限</th>
            <th v-if="members.some(m => m.joinedAt)">加入時間</th>
            <th v-if="canRemove" class="text-right">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="member in members" :key="member.id">
            <td class="user-cell">
              <div class="user-avatar"><UserCheck :size="15" /></div>
              <span class="username">{{ member.username }}</span>
            </td>
            <td class="role-cell">
              <select
                v-if="canEditRole && availableRoles.length"
                :value="member.role"
                class="inline-select role-select"
                :disabled="saving"
                @change="handleRoleChange(member.id, $event)"
              >
                <option v-for="r in availableRoles" :key="r.code" :value="r.code">
                  {{ r.label }}
                </option>
              </select>
              <span v-else class="role-badge">{{ member.role }}</span>
            </td>
            <td v-if="members.some(m => m.joinedAt)" class="date-cell">
              {{ member.joinedAt || '-' }}
            </td>
            <td v-if="canRemove" class="text-right">
              <button
                type="button"
                class="btn-subtle btn-danger-subtle"
                :disabled="saving"
                @click="handleRemove(member.id)"
              >
                <Trash2 :size="13" /> 移除
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="list-state empty">目前尚無成員。</div>
  </div>
</template>

<style scoped>
.shared-member-management {
  display: flex;
  flex-direction: column;
  gap: 16px;
  background: #ffffff;
  border-radius: 8px;
  padding: 16px 20px;
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

.btn-subtle,
.btn-primary,
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

.btn-primary {
  border: 1px solid #059669;
  background: #059669;
  color: #ffffff;
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
</style>
