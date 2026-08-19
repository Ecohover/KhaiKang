<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions } from '@khaikang/ui'
import ResourceFormLayout from '../components/ResourceFormLayout.vue'
import TestWorkspaceCreateFormFields from '../components/TestWorkspaceCreateFormFields.vue'
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

async function create(): Promise<void> {
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
      await router.push({ name: 'test-suites', params: { workspaceId: result.data.id } })
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
  <ResourceFormLayout
    :back-to="{ name: 'test-workspaces' }"
    :back-label="t('shell.navigation.backToWorkspaces')"
    :meta="t('tests.management')"
    :title="t('tests.workspace.createTitle')"
    :description="t('tests.workspace.createDescription')"
    :error="error"
  >
    <form @submit.prevent="create">
      <TestWorkspaceCreateFormFields
        v-model:name="name"
        v-model:prefix="prefix"
        v-model:description="description"
        :prefix-error="prefixError"
        :disabled="creating"
        :labels="{ sectionTitle: t('tests.workspace.createSectionTitle'), sectionDescription: t('tests.workspace.createSectionDescription'), name: t('tests.workspace.name'), prefix: t('tests.workspace.prefix'), description: t('tests.workspace.descriptionLabel') }"
      />
    </form>
    <template #actions>
      <UiCreateActions
        :loading="creating"
        :disabled="!name.trim() || Boolean(prefixError)"
        :allow-continue="false"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('tests.workspace.create')"
        @cancel="router.push({ name: 'test-workspaces' })"
        @create="create"
      />
    </template>
  </ResourceFormLayout>
</template>
