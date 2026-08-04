<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions, UiField } from '@khaikang/ui'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { useSaveNotice } from '../composables/useSaveNotice'

const router = useRouter()
const { t } = useI18n()
const name = ref('')
const prefix = ref('')
const description = ref('')
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()
const prefixError = computed(() => {
  if (!prefix.value) return ''
  return /^[A-Za-z][A-Za-z0-9]{1,9}$/.test(prefix.value)
    ? ''
    : t('tests.workspace.prefixInvalid')
})

async function create(continueCreating: boolean): Promise<void> {
  if (!name.value.trim() || prefixError.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.createTestWorkspace({
      name: name.value.trim(),
      prefix: prefix.value.trim() || null,
      description: description.value.trim() || null,
    }, await secureHeaders())
    if (result.data) {
      showCreated(t('tests.workspace.createdRecord'), result.data.name)
      if (continueCreating) {
        name.value = ''
        prefix.value = ''
        description.value = ''
        await nextTick()
        document.getElementById('test-workspace-name')?.focus()
      } else {
        await router.push({ name: 'test-suites', params: { workspaceId: result.data.id } })
      }
    } else {
      error.value = result.error?.code === 'workspace_prefix_conflict'
        ? t('tests.workspace.prefixConflict')
        : problemMessage(result.error, t('tests.workspace.createFailed'))
    }
  } catch {
    error.value = t('tests.workspace.connectionFailed')
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <section class="create-page">
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'test-workspaces' }"
      :back-label="t('shell.navigation.backToWorkspaces')"
      :items="[
        { label: t('shell.navigation.workspaceList', '測試工作區'), to: { name: 'test-workspaces' } },
        { label: t('tests.workspace.create', '建立工作區'), active: true },
      ]"
    />

    <form class="form-card" @submit.prevent="create(false)">
      <header>
        <h2>{{ t('tests.workspace.createTitle') }}</h2>
        <p>{{ t('tests.workspace.createDescription') }}</p>
      </header>

      <p v-if="error" class="error-banner">{{ error }}</p>

      <div class="field-grid">
        <UiField
          id="test-workspace-name"
          v-model="name"
          :label="t('tests.workspace.name')"
          required
          :disabled="creating"
        />
        <UiField
          id="test-workspace-prefix"
          v-model="prefix"
          :label="t('tests.workspace.prefix')"
          :disabled="creating"
          :error="prefixError"
        />
        <UiField
          id="test-workspace-description"
          v-model="description"
          :label="t('tests.workspace.descriptionLabel')"
          multiline
          :disabled="creating"
        />
      </div>

      <UiCreateActions
        :loading="creating"
        :disabled="!name.trim() || Boolean(prefixError)"
        :create-label="t('tests.workspace.create')"
        :continue-label="t('tests.workspace.createAndContinue')"
        @create="create(false)"
        @create-continue="create(true)"
      />
    </form>
  </section>
</template>

<style scoped>
.create-page {
  display: grid;
  max-width: 960px;
  gap: 20px;
  width: 100%;
  box-sizing: border-box;
}

.form-card {
  display: grid;
  gap: 20px;
  background: white;
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
  padding: 24px;
  width: 100%;
  box-sizing: border-box;
}

.form-card :deep(.ui-create-actions) {
  position: sticky;
  bottom: 0;
  justify-content: flex-end;
  padding: 14px 0 0;
  background: var(--kk-surface);
  border-top: 1px solid var(--kk-border);
}

.form-card header h2 {
  margin: 0 0 4px;
  font-size: 1.4rem;
}
.form-card header p {
  margin: 0;
  color: var(--kk-text-muted);
  font-size: 0.88rem;
}

.error-banner {
  padding: 10px 14px;
  background: #fef2f2;
  color: #dc2626;
  border-radius: 6px;
  font-size: 0.85rem;
}

.field-grid {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>
