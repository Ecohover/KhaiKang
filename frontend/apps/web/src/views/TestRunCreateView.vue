<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ArrowLeft } from '@lucide/vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { UiButton, UiFormActionBar } from '@khaikang/ui'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { TestPlanResponse, TestWorkspaceResponse } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { showCreated } = useSaveNotice()
const workspaceId = computed(() => String(route.params.workspaceId))
const workspace = ref<TestWorkspaceResponse>()
const plans = ref<TestPlanResponse[]>([])
const planId = ref('')
const name = ref('')
const loading = ref(true)
const creating = ref(false)
const error = ref('')

const activePlans = computed(() => plans.value.filter((plan) => plan.status === 'active'))
const selectedPlan = computed(() => activePlans.value.find((plan) => plan.id === planId.value))
const isValid = computed(() => Boolean(planId.value && name.value.trim()))

function selectPlan(id: string): void {
  planId.value = id
  const plan = activePlans.value.find((item) => item.id === id)
  if (plan && !name.value.trim()) name.value = `${plan.name} Run`
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, planResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestPlans(workspaceId.value),
  ])
  workspace.value = workspaceResult.data
  plans.value = planResult.data ?? []
  const preferredPlanId = typeof route.query.planId === 'string' ? route.query.planId : ''
  const preferredPlan = activePlans.value.find((plan) => plan.id === preferredPlanId)
  const plan = preferredPlan ?? activePlans.value[0]
  if (plan) {
    planId.value = plan.id
    name.value = `${plan.name} Run`
  }
  error.value = problemMessage(
    workspaceResult.error ?? planResult.error,
    workspace.value ? '' : t('tests.run.loadFailed'),
  )
  loading.value = false
}

async function create(): Promise<void> {
  if (!isValid.value || creating.value) return
  creating.value = true
  error.value = ''
  const result = await apiClient.createTestRun(
    workspaceId.value,
    { planId: planId.value, name: name.value.trim() },
    await secureHeaders(),
  )
  if (result.data) {
    showCreated(t('tests.run.record'), result.data.name)
    await router.push({ name: 'test-run-detail', params: { workspaceId: workspaceId.value, runId: result.data.id } })
  } else {
    error.value = problemMessage(result.error, t('tests.run.createFailed'))
  }
  creating.value = false
}

onMounted(load)
</script>

<template>
  <section class="create-page">
    <button type="button" class="back-link" @click="router.push({ name: 'test-runs', params: { workspaceId } })">
      <ArrowLeft :size="16" />{{ t('tests.run.backToList') }}
    </button>
    <header>
      <p class="eyebrow">{{ t('tests.management') }}</p>
      <h2>{{ t('tests.run.create') }}</h2>
      <span>{{ workspace?.prefix }} · {{ workspace?.name }}</span>
    </header>

    <p v-if="loading" class="state-panel">{{ t('tests.run.loading') }}</p>
    <form v-else-if="workspace" class="create-form" @submit.prevent="create">
      <section class="form-section">
        <header><div><h3>{{ t('tests.run.record') }}</h3><p>{{ t('tests.run.snapshotHint') }}</p></div></header>
        <label>
          <span>{{ t('tests.run.plan') }}</span>
          <select :value="planId" :disabled="creating || !activePlans.length" @change="selectPlan(($event.target as HTMLSelectElement).value)">
            <option value="" disabled>{{ t('tests.run.plan') }}</option>
            <option v-for="plan in activePlans" :key="plan.id" :value="plan.id">{{ plan.code }} · {{ plan.name }}</option>
          </select>
        </label>
        <p v-if="!activePlans.length" class="plan-hint">
          {{ t('tests.run.noActivePlan') }}
          <RouterLink :to="{ name: 'test-plans', params: { workspaceId } }">{{ t('tests.run.managePlans') }}</RouterLink>
        </p>
        <label>
          <span>{{ t('tests.run.name') }}</span>
          <input v-model="name" maxlength="200" :disabled="creating || !selectedPlan" />
        </label>
      </section>
      <UiFormActionBar mode="floating">
        <template #status><p v-if="error" class="error" role="alert">{{ error }}</p></template>
        <div class="actions">
          <UiButton type="button" variant="secondary" :disabled="creating" @click="router.push({ name: 'test-runs', params: { workspaceId } })">{{ t('common.actions.cancel') }}</UiButton>
          <UiButton type="submit" :loading="creating" :disabled="!isValid">{{ t('tests.run.create') }}</UiButton>
        </div>
      </UiFormActionBar>
    </form>
    <div v-else class="state-panel state-panel--error" role="alert">{{ error }}</div>
  </section>
</template>

<style scoped>
.create-page{display:grid;max-width:960px;gap:22px;margin:0 auto;padding-bottom:40px}.back-link{display:flex;width:fit-content;align-items:center;gap:6px;padding:0;color:var(--kk-text-muted);background:transparent;border:0;cursor:pointer}.create-page>header h2{margin:3px 0 7px;font-size:clamp(1.65rem,3vw,2.2rem)}.create-page>header span,.form-section p{color:var(--kk-text-muted)}.eyebrow{margin:0;color:var(--kk-accent);font-size:.75rem;font-weight:750;letter-spacing:.08em;text-transform:uppercase}.create-form{display:grid;gap:18px}.form-section{display:grid;gap:18px;padding:24px;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:var(--kk-radius);box-shadow:var(--kk-shadow)}.form-section>header{display:flex;justify-content:space-between;gap:16px}.form-section h3,.form-section p{margin:0}.form-section p{margin-top:4px;font-size:.84rem}.form-section label{display:grid;gap:7px;font-size:.875rem;font-weight:650}.form-section input,.form-section select{padding:11px;color:var(--kk-text);background:var(--kk-surface);border:1px solid var(--kk-border-strong);border-radius:var(--kk-radius);font:inherit}.plan-hint{display:flex;gap:5px;flex-wrap:wrap}.plan-hint a{color:var(--kk-accent);font-weight:650}.actions{display:flex;gap:10px;margin-left:auto}.error,.state-panel--error{color:var(--kk-danger)}.error{margin:0;font-size:.84rem;font-weight:600}.state-panel{margin:0;padding:42px 24px;text-align:center;background:var(--kk-surface);border:1px dashed var(--kk-border-strong);border-radius:var(--kk-radius)}
</style>
