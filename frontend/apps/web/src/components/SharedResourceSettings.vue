<script setup lang="ts">
import { ref, watch } from 'vue'
import { Save, Settings, AlertCircle, CheckCircle2 } from '@lucide/vue'

const props = withDefaults(
  defineProps<{
    title: string
    sectionDescription?: string
    version?: number | string
    name: string
    codeOrPrefix: string
    codeLabel?: string
    description: string
    status: 'active' | 'inactive'
    canEdit?: boolean
    canChangeStatus?: boolean
    loading?: boolean
    saving?: boolean
    error?: string
    saved?: boolean
  }>(),
  {
    sectionDescription: '更新資源基本資料與狀態',
    codeLabel: '資源代碼',
    canEdit: true,
    canChangeStatus: true,
    loading: false,
    saving: false,
    error: '',
    saved: false,
  },
)

const emit = defineEmits<{
  (e: 'save', payload: { name: string; description: string; status: 'active' | 'inactive' }): void
}>()

const formName = ref(props.name)
const formDescription = ref(props.description)
const formStatus = ref<'active' | 'inactive'>(props.status)

watch(
  () => props.name,
  (val) => {
    formName.value = val
  },
)

watch(
  () => props.description,
  (val) => {
    formDescription.value = val
  },
)

watch(
  () => props.status,
  (val) => {
    formStatus.value = val
  },
)

function handleSubmit(): void {
  if (!formName.value.trim() || !props.canEdit) return
  emit('save', {
    name: formName.value.trim(),
    description: formDescription.value.trim(),
    status: formStatus.value,
  })
}
</script>

<template>
  <div class="shared-resource-settings">
    <header class="settings-header">
      <div class="header-title">
        <Settings :size="20" class="header-icon" />
        <div>
          <h3>{{ title }}</h3>
          <p class="sub-desc">{{ sectionDescription }}</p>
        </div>
      </div>
      <span v-if="version !== undefined" class="version-tag">版本 v{{ version }}</span>
    </header>

    <div v-if="loading" class="form-state">載入設定資料中...</div>

    <form v-else class="settings-form" @submit.prevent="handleSubmit">
      <div class="form-grid">
        <!-- NAME FIELD -->
        <label class="form-field">
          <span class="label-text">名稱 *</span>
          <input
            v-model="formName"
            class="input-control"
            placeholder="請輸入名稱"
            :disabled="saving || !canEdit"
            required
          />
        </label>

        <!-- CODE / PREFIX FIELD (READONLY) -->
        <label class="form-field">
          <span class="label-text">{{ codeLabel }}</span>
          <input :value="codeOrPrefix" class="input-control readonly-input" disabled />
          <small class="help-text">建立後此代碼/前綴無法修改</small>
        </label>

        <!-- DESCRIPTION FIELD -->
        <label class="form-field full-width">
          <span class="label-text">說明 / 簡介</span>
          <textarea
            v-model="formDescription"
            class="textarea-control"
            rows="4"
            placeholder="填寫簡介說明..."
            :disabled="saving || !canEdit"
          />
        </label>

        <!-- STATUS FIELD -->
        <label class="form-field">
          <span class="label-text">狀態</span>
          <select
            v-model="formStatus"
            class="select-control"
            :disabled="saving || !canEdit || !canChangeStatus"
          >
            <option value="active">使用中 (Active)</option>
            <option value="inactive">已停用 (Inactive)</option>
          </select>
          <small v-if="!canChangeStatus" class="help-text">缺乏變更狀態權限</small>
        </label>
      </div>

      <!-- FEEDBACK MESSAGES -->
      <div v-if="error" class="message-banner error-banner" role="alert">
        <AlertCircle :size="16" /> {{ error }}
      </div>
      <div v-if="saved" class="message-banner success-banner" role="status">
        <CheckCircle2 :size="16" /> 設定已成功儲存！
      </div>

      <!-- FORM ACTIONS -->
      <div class="form-actions">
        <span v-if="!canEdit" class="permission-hint">僅供檢視，無編輯權限</span>
        <button
          v-if="canEdit"
          type="submit"
          class="btn-primary"
          :disabled="saving || !formName.trim()"
        >
          <Save :size="15" /> {{ saving ? '儲存中...' : '儲存設定' }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.shared-resource-settings {
  display: flex;
  flex-direction: column;
  gap: 20px;
  background: #ffffff;
  border-radius: 10px;
  padding: 20px 24px;
  border: 1px solid var(--kk-border);
  width: 100%;
  box-sizing: border-box;
}

.settings-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--kk-border);
  padding-bottom: 14px;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-icon {
  color: var(--kk-accent);
}

.header-title h3 {
  margin: 0;
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--kk-text);
}

.sub-desc {
  margin: 2px 0 0;
  font-size: 0.85rem;
  color: var(--kk-text-muted);
}

.version-tag {
  font-size: 0.78rem;
  padding: 3px 8px;
  background: #f3f4f6;
  color: #4b5563;
  border-radius: 6px;
  font-weight: 600;
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.form-field.full-width {
  grid-column: span 2;
}

.label-text {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--kk-text);
}

.input-control,
.select-control {
  height: 36px;
  padding: 0 10px;
  font-size: 0.88rem;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  background: white;
  box-sizing: border-box;
}

.readonly-input {
  background: #f9fafb;
  color: var(--kk-text-muted);
}

.textarea-control {
  padding: 10px;
  font-size: 0.88rem;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  background: white;
  box-sizing: border-box;
  font-family: inherit;
}

.input-control:focus,
.select-control:focus,
.textarea-control:focus {
  outline: none;
  border-color: #059669;
  box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.15);
}

.help-text {
  font-size: 0.76rem;
  color: var(--kk-text-muted);
}

.message-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border-radius: 6px;
  font-size: 0.85rem;
  font-weight: 500;
}

.error-banner {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.success-banner {
  background: #ecfdf5;
  color: #059669;
  border: 1px solid #a7f3d0;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: 12px;
  margin-top: 8px;
}

.permission-hint {
  font-size: 0.82rem;
  color: var(--kk-text-muted);
}

.btn-primary {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  height: 34px;
  padding: 0 16px;
  font-size: 0.88rem;
  font-weight: 600;
  border-radius: 6px;
  border: 1px solid #059669;
  background: #059669;
  color: white;
  cursor: pointer;
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 768px) {
  .form-grid {
    grid-template-columns: 1fr;
  }
  .form-field.full-width {
    grid-column: span 1;
  }
}
</style>
