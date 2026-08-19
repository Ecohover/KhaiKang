<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiCreateActions } from '@khaikang/ui'
import ProjectCreateFormFields from '../components/ProjectCreateFormFields.vue'
import ResourceFormLayout from '../components/ResourceFormLayout.vue'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { useSaveNotice } from '../composables/useSaveNotice'

const router = useRouter()
const { t } = useI18n()
const code = ref('')
const name = ref('')
const description = ref('')
const creating = ref(false)
const error = ref('')
const { showCreated } = useSaveNotice()
const codeError = computed(() => {
  if (!code.value) return ''
  return /^[A-Za-z0-9][A-Za-z0-9_-]{0,99}$/.test(code.value) ? '' : t('projects.create.codeInvalid')
})
const disabled = computed(() => !code.value.trim() || !name.value.trim() || Boolean(codeError.value))

async function create(): Promise<void> {
  if (disabled.value || creating.value) return
  creating.value = true
  error.value = ''
  try {
    const result = await apiClient.createProject({
      code: code.value.trim(),
      name: name.value.trim(),
      description: description.value.trim() || null,
    }, await secureHeaders())
    if (!result.data) {
      error.value = problemMessage(result.error, t('projects.create.failed'))
      return
    }
    showCreated(t('projects.record'), result.data.code)
    await router.push({ name: 'project-detail', params: { projectId: result.data.id } })
  } catch {
    error.value = t('projects.detail.connectionError')
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <ResourceFormLayout
    :back-to="{ name: 'projects' }"
    :back-label="t('projects.create.back')"
    :meta="t('projects.management')"
    :title="t('projects.create.title')"
    :description="t('projects.create.description')"
    :error="error"
  >
    <form @submit.prevent="create">
      <ProjectCreateFormFields
        v-model:code="code"
        v-model:name="name"
        v-model:description="description"
        :code-error="codeError"
        :disabled="creating"
        :labels="{ sectionTitle: t('projects.create.sectionTitle'), sectionDescription: t('projects.create.sectionDescription'), code: t('projects.create.code'), name: t('projects.create.name'), description: t('projects.create.descriptionLabel') }"
      />
    </form>
    <template #actions>
      <UiCreateActions
        :loading="creating"
        :disabled="disabled"
        :allow-continue="false"
        :cancel-label="t('common.actions.cancel')"
        :create-label="t('projects.create.submit')"
        @cancel="router.push({ name: 'projects' })"
        @create="create"
      />
    </template>
  </ResourceFormLayout>
</template>
