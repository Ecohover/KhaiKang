<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Link2, Plus, Trash2 } from '@lucide/vue'
import { UiButton, UiSelect } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import { listAllProjectIssues } from '../api/issueOptions'
import IssueKeyPicker from './IssueKeyPicker.vue'
import type {
  IssueRelationDirection,
  IssueRelationResponse,
  IssueRelationTypeResponse,
  IssueResponse,
} from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const props = defineProps<{
  projectId: string
  issueId: string
  canCreate: boolean
  canDelete: boolean
}>()

const { t, te } = useI18n()
const { showCreated, showUpdated } = useSaveNotice()
const relationTypes = ref<IssueRelationTypeResponse[]>([])
const relations = ref<IssueRelationResponse[]>([])
const candidates = ref<IssueResponse[]>([])
const relationTypeCode = ref('related')
const relatedIssueId = ref('')
const direction = ref<IssueRelationDirection>('forward')
const loading = ref(true)
const saving = ref(false)
const error = ref('')

const selectedType = computed(() => relationTypes.value.find(
  item => item.code === relationTypeCode.value,
))

onMounted(load)

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  const [typeResult, relationResult, issueResult] = await Promise.all([
    apiClient.listIssueRelationTypes(props.projectId),
    apiClient.listIssueRelations(props.projectId, props.issueId),
    listAllProjectIssues(props.projectId),
  ])
  relationTypes.value = typeResult.data ?? []
  relations.value = relationResult.data ?? []
  candidates.value = issueResult.issues.filter(item => item.id !== props.issueId)
  const loadError = typeResult.error ?? relationResult.error ?? issueResult.error
  error.value = loadError
    ? problemMessage(loadError, t('projects.issues.relations.loadFailed'))
    : ''
  loading.value = false
}

function relatedIssue(relation: IssueRelationResponse) {
  return relation.sourceIssue.id === props.issueId
    ? relation.targetIssue
    : relation.sourceIssue
}

function relationLabel(relation: IssueRelationResponse): string {
  const side = relation.sourceIssue.id === props.issueId ? 'forward' : 'reverse'
  const key = `projects.issues.relations.types.${relation.relationTypeCode}.${side}`
  const fallback = side === 'forward' ? relation.forwardLabel : relation.reverseLabel
  return te(key) ? t(key) : fallback
}

function typeLabel(type: IssueRelationTypeResponse): string {
  const key = `projects.issues.relations.types.${type.code}.name`
  return te(key) ? t(key) : type.forwardLabel
}

function directionLabel(side: IssueRelationDirection): string {
  const type = selectedType.value
  if (!type) return ''
  const key = `projects.issues.relations.types.${type.code}.${side}`
  const fallback = side === 'forward' ? type.forwardLabel : type.reverseLabel
  return `${side === 'forward' ? t('projects.issues.relations.currentIssue') : t('projects.issues.relations.relatedIssue')} ${te(key) ? t(key) : fallback}`
}

async function createRelation(): Promise<void> {
  if (!relatedIssueId.value || !relationTypeCode.value || saving.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.createIssueRelation(
    props.projectId,
    props.issueId,
    {
      relationTypeCode: relationTypeCode.value,
      relatedIssueId: relatedIssueId.value,
      direction: selectedType.value?.directionKind === 'symmetric' ? 'forward' : direction.value,
    },
    await secureHeaders(),
  )
  if (result.data) {
    relations.value = [result.data, ...relations.value]
    relatedIssueId.value = ''
    showCreated(t('projects.issues.relations.record'), relationLabel(result.data))
  } else {
    error.value = problemMessage(result.error, t('projects.issues.relations.createFailed'))
  }
  saving.value = false
}

async function deleteRelation(relation: IssueRelationResponse): Promise<void> {
  if (saving.value || !window.confirm(t('projects.issues.relations.deleteConfirm'))) return
  saving.value = true
  error.value = ''
  const result = await apiClient.deleteIssueRelation(
    props.projectId,
    props.issueId,
    relation.id,
    relation.version,
    await secureHeaders(),
  )
  if (result.error) {
    error.value = problemMessage(result.error, t('projects.issues.relations.deleteFailed'))
  } else {
    relations.value = relations.value.filter(item => item.id !== relation.id)
    showUpdated(t('projects.issues.relations.record'), relationLabel(relation))
  }
  saving.value = false
}
</script>

<template>
  <section class="relations-panel">
    <header>
      <div class="heading">
        <Link2 :size="19" aria-hidden="true" />
        <div>
          <h3>{{ t('projects.issues.relations.title') }}</h3>
          <p>{{ t('projects.issues.relations.description') }}</p>
        </div>
      </div>
    </header>

    <p v-if="error" class="relation-error" role="alert">{{ error }}</p>
    <p v-if="loading" class="relation-empty">{{ t('projects.issues.relations.loading') }}</p>
    <template v-else>
      <div v-if="canCreate" class="relation-create">
        <label>
          <span>{{ t('projects.issues.relations.type') }}</span>
          <UiSelect v-model="relationTypeCode" :disabled="saving">
            <option v-for="type in relationTypes" :key="type.id" :value="type.code">
              {{ typeLabel(type) }}
            </option>
          </UiSelect>
        </label>
        <label v-if="selectedType?.directionKind !== 'symmetric'">
          <span>{{ t('projects.issues.relations.direction') }}</span>
          <UiSelect v-model="direction" :disabled="saving">
            <option value="forward">{{ directionLabel('forward') }}</option>
            <option value="reverse">{{ directionLabel('reverse') }}</option>
          </UiSelect>
        </label>
        <IssueKeyPicker
          id="issue-relation-target"
          v-model="relatedIssueId"
          class="relation-target"
          :issues="candidates"
          :label="t('projects.issues.relations.target')"
          :placeholder="t('projects.issues.relations.targetPlaceholder')"
          :search-label="t('projects.issues.relations.searchTarget')"
          :not-found-message="t('projects.issues.relations.targetNotFound')"
          :disabled="saving"
        />
        <UiButton type="button" :disabled="!relatedIssueId || saving" :loading="saving" @click="createRelation">
          <Plus :size="16" aria-hidden="true" />{{ t('projects.issues.relations.add') }}
        </UiButton>
      </div>

      <p v-if="relations.length === 0" class="relation-empty">
        {{ t('projects.issues.relations.empty') }}
      </p>
      <ul v-else class="relation-list">
        <li v-for="relation in relations" :key="relation.id">
          <span class="relation-kind">{{ relationLabel(relation) }}</span>
          <RouterLink
            :to="{ name: 'project-issue-edit', params: { projectId, issueId: relatedIssue(relation).id } }"
          >
            <strong>{{ relatedIssue(relation).key }}</strong>
            <span>{{ relatedIssue(relation).title }}</span>
          </RouterLink>
          <UiButton
            v-if="canDelete"
            type="button"
            variant="secondary"
            :disabled="saving"
            :aria-label="t('projects.issues.relations.delete')"
            @click="deleteRelation(relation)"
          >
            <Trash2 :size="15" aria-hidden="true" />
          </UiButton>
        </li>
      </ul>
    </template>
  </section>
</template>

<style scoped>
.relations-panel{display:grid;gap:18px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}
.relations-panel header,.heading{display:flex;align-items:flex-start;gap:10px}.relations-panel h3,.relations-panel p{margin:0}.relations-panel header p,.relation-empty{color:var(--kk-text-muted);font-size:.84rem}.relation-create{display:grid;grid-template-columns:minmax(150px,.75fr) minmax(180px,1fr) minmax(280px,1.5fr) auto;align-items:end;gap:12px}.relation-create label{display:grid;gap:6px;font-size:.84rem;font-weight:650}.relation-create>button{min-height:32px}.relation-list{display:grid;gap:8px;margin:0;padding:0;list-style:none}.relation-list li{display:grid;grid-template-columns:minmax(110px,.45fr) minmax(0,1.55fr) auto;align-items:center;gap:12px;padding:10px 12px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-radius:7px}.relation-list a{display:flex;min-width:0;gap:8px;color:inherit;text-decoration:none}.relation-list a span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.relation-kind{color:var(--kk-accent);font-size:.82rem;font-weight:700}.relation-error{color:var(--kk-danger)}
@media(max-width:800px){.relation-create{grid-template-columns:1fr}.relation-list li{grid-template-columns:1fr auto}.relation-kind{grid-column:1/-1}}
</style>
