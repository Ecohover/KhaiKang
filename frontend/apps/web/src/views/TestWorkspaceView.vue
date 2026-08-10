<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ChevronDown, ChevronRight, Edit3, ExternalLink, FileCheck2, Folder, FolderKanban, FolderOpen, FolderPlus, FolderTree, GripVertical, Layers, LayoutDashboard, Link2, List, Plus, Save, Trash2, Unlink, UserPlus } from '@lucide/vue'
import { UiButton, UiEmptyState, UiSelect, UiStatusBadge } from '@khaikang/ui'
import ResourceMemberManager from '../components/ResourceMemberManager.vue'
import ResourcePageHeader from '../components/ResourcePageHeader.vue'
import SharedBreadcrumb from '../components/SharedBreadcrumb.vue'
import SharedCardSection from '../components/SharedCardSection.vue'
import SharedResourceSettings from '../components/SharedResourceSettings.vue'
import SharedStateBanner from '../components/SharedStateBanner.vue'
import SharedFilterToolbar from '../components/SharedFilterToolbar.vue'
import SharedSearchField from '../components/SharedSearchField.vue'
import SharedViewTabs from '../components/SharedViewTabs.vue'
import TestCaseEditForm from '../components/TestCaseEditForm.vue'
import { apiClient, problemMessage, secureHeaders } from '../api/client'
import type { ProjectResponse, TestCaseResponse, TestSuiteResponse, TestTagResponse, TestWorkspaceMemberResponse, TestWorkspaceProjectResponse, TestWorkspaceResponse, TestWorkspaceRole } from '../api/contracts'
import { useSaveNotice } from '../composables/useSaveNotice'

const route = useRoute()
const router = useRouter()
const { t, d } = useI18n()
const workspaceId = computed(() => String(route.params.workspaceId))
const workspace = ref<TestWorkspaceResponse>()
const members = ref<TestWorkspaceMemberResponse[]>([])
const linkedProjects = ref<TestWorkspaceProjectResponse[]>([])
const projects = ref<ProjectResponse[]>([])
const suites = ref<TestSuiteResponse[]>([])
const cases = ref<TestCaseResponse[]>([])
const tags = ref<TestTagResponse[]>([])
const loading = ref(true)
const saving = ref(false)
const linkingProject = ref(false)
const error = ref('')
const selectedProjectId = ref('')

const selectedSuiteId = ref<string | null>(null)
const selectedCaseId = ref<string | null>(null)
const collapsedSuiteIds = ref<Set<string>>(new Set())

const tab = computed<'home' | 'suites' | 'members' | 'settings'>(() => {
  if (route.name === 'test-home') return 'home'
  if (route.name === 'test-members') return 'members'
  if (route.name === 'test-settings') return 'settings'
  return 'suites'
})

const tabLabel = computed(() => {
  if (tab.value === 'home') return t('projects.detail.homeTab')
  if (tab.value === 'members') return t('tests.member.title')
  if (tab.value === 'settings') return t('tests.workspace.settings')
  return t('routes.testSuites')
})
const username = ref('')
const memberRole = ref<TestWorkspaceRole>('tester')
const { showCreated, showUpdated } = useSaveNotice()

const canManage = computed(() => ['owner', 'manager'].includes(workspace.value?.currentUserRole ?? ''))
const canManageProjectLinks = computed(() => canManage.value && workspace.value?.status === 'active')
const availableProjects = computed(() => {
  const linkedIds = new Set(linkedProjects.value.map((link) => link.projectId))
  return projects.value
    .filter((project) => project.status === 'active' && !linkedIds.has(project.id))
    .sort((left, right) => left.code.localeCompare(right.code))
})
const suiteView = ref<'tree' | 'list'>('tree')
const caseQuery = ref('')
const caseStatusFilter = ref<'active' | 'inactive' | ''>('')
const caseTagFilter = ref('')
const addingMember = ref(false)

interface TreeNode {
  type: 'suite' | 'case'
  id: string
  depth: number
  suite?: TestSuiteResponse
  testCase?: TestCaseResponse
  caseIndex?: number
  parentId: string | null
}

const orderedSuites = computed(() => {
  const result: TestSuiteResponse[] = []
  const appendChildren = (parentId: string | null): void => {
    suites.value
      .filter((suite) => suite.parentId === parentId)
      .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))
      .forEach((suite) => {
        result.push(suite)
        appendChildren(suite.id)
      })
  }
  appendChildren(null)
  return result
})

const visibleTreeNodes = computed(() => {
  const nodes: TreeNode[] = []

  const appendTreeNodes = (parentId: string | null) => {
    // 1. ALL child suites of parentId come FIRST
    const childSuites = suites.value
      .filter((s) => s.parentId === parentId)
      .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))

    for (const suite of childSuites) {
      if (!isSuiteVisible(suite)) continue

      nodes.push({
        type: 'suite',
        id: `suite-${suite.id}`,
        depth: suite.depth,
        suite,
        parentId: suite.parentId,
      })

      // If suite is expanded, recursively append its sub-tree
      if (!isCollapsed(suite.id)) {
        appendTreeNodes(suite.id)
      }
    }

    // 2. ALL test cases directly under parentId come SECOND (after all child suites)
    if (parentId !== null) {
      const suiteCases = (allCasesBySuite.value.get(parentId) ?? [])
        .sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title))

      suiteCases.forEach((testCase, idx) => {
        const parentSuite = suites.value.find((s) => s.id === parentId)
        const caseDepth = parentSuite ? parentSuite.depth + 1 : 1
        nodes.push({
          type: 'case',
          id: `case-${testCase.id}`,
          depth: caseDepth,
          testCase,
          caseIndex: idx + 1,
          parentId,
        })
      })
    }
  }

  appendTreeNodes(null)
  return nodes
})

const allCasesBySuite = computed(() => {
  const result = new Map<string, TestCaseResponse[]>()
  for (const testCase of cases.value) {
    const values = result.get(testCase.suiteId) ?? []
    values.push(testCase)
    result.set(testCase.suiteId, values)
  }
  for (const values of result.values()) {
    values.sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title))
  }
  return result
})

const filteredCases = computed(() => {
  const query = caseQuery.value.trim().toLocaleLowerCase()
  return cases.value.filter((item) =>
    (!caseStatusFilter.value || item.status === caseStatusFilter.value) &&
    (!caseTagFilter.value || item.tags.some((tag) => tag.id === caseTagFilter.value)) &&
    (!query || item.title.toLocaleLowerCase().includes(query)),
  )
})

const selectedSuite = computed(() => {
  if (!selectedSuiteId.value) return null
  return suites.value.find((s) => s.id === selectedSuiteId.value) ?? null
})

const selectedCase = computed(() => {
  if (!selectedCaseId.value) return null
  return cases.value.find((c) => c.id === selectedCaseId.value) ?? null
})

const casesForSelectedSuite = computed(() => {
  if (selectedSuiteId.value) {
    return allCasesBySuite.value.get(selectedSuiteId.value) ?? []
  }
  return cases.value
})

// Keeps the legacy block type-safe while the shared form above owns the live case editor.
const legacySelectedCase = computed(() => selectedCase.value)

const childSuitesForSelectedSuite = computed(() => {
  if (!selectedSuiteId.value) return []
  return suites.value
    .filter((suite) => suite.parentId === selectedSuiteId.value)
    .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))
})

function hasChildren(suiteId: string): boolean {
  return suites.value.some((s) => s.parentId === suiteId)
}

function isCollapsed(suiteId: string): boolean {
  return collapsedSuiteIds.value.has(suiteId)
}

function toggleCollapse(suiteId: string): void {
  const next = new Set(collapsedSuiteIds.value)
  if (next.has(suiteId)) {
    next.delete(suiteId)
  } else {
    next.add(suiteId)
  }
  collapsedSuiteIds.value = next
}

function isSuiteVisible(suite: TestSuiteResponse): boolean {
  let currentParentId = suite.parentId
  while (currentParentId) {
    if (collapsedSuiteIds.value.has(currentParentId)) {
      return false
    }
    const parent = suites.value.find((s) => s.id === currentParentId)
    currentParentId = parent ? parent.parentId : null
  }
  return true
}

// SUITE EDIT STATE & COMPUTED
const suiteForm = ref({
  name: '',
  parentId: null as string | null,
  description: '',
  status: 'active' as 'active' | 'inactive',
})

// Prevent selecting self or descendant suites as parent
const isDescendant = (candidateId: string, targetAncestorId: string): boolean => {
  let current = suites.value.find((s) => s.id === candidateId)
  while (current && current.parentId) {
    if (current.parentId === targetAncestorId) return true
    current = suites.value.find((s) => s.id === current!.parentId)
  }
  return false
}

function getSuiteFullPath(suiteId: string | null): string {
  if (!suiteId) return t('tests.suite.root')
  const parts: string[] = []
  let current: TestSuiteResponse | undefined = suites.value.find((s) => s.id === suiteId)
  while (current) {
    parts.unshift(current.name)
    current = current.parentId ? suites.value.find((s) => s.id === current!.parentId) : undefined
  }
  return parts.join(' / ')
}

const eligibleParentSuites = computed(() => {
  if (!selectedSuite.value) return []
  return suites.value
    .filter((s) => s.id !== selectedSuite.value!.id && !isDescendant(s.id, selectedSuite.value!.id))
    .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))
})

function populateSuiteForm(): void {
  if (!selectedSuite.value) return
  suiteForm.value = {
    name: selectedSuite.value.name,
    parentId: selectedSuite.value.parentId,
    description: selectedSuite.value.description ?? '',
    status: selectedSuite.value.status,
  }
}

watch(selectedSuite, populateSuiteForm, { immediate: true })

const isSuiteDirty = computed(() => {
  if (!selectedSuite.value) return false
  return (
    suiteForm.value.name.trim() !== selectedSuite.value.name ||
    suiteForm.value.parentId !== selectedSuite.value.parentId ||
    suiteForm.value.description.trim() !== (selectedSuite.value.description ?? '') ||
    suiteForm.value.status !== selectedSuite.value.status
  )
})

async function saveSuiteForm(): Promise<void> {
  if (!selectedSuite.value || saving.value || !suiteForm.value.name.trim()) return
  saving.value = true
  const result = await apiClient.updateTestSuite(
    workspaceId.value,
    selectedSuite.value.id,
    {
      parentId: suiteForm.value.parentId,
      name: suiteForm.value.name.trim(),
      description: suiteForm.value.description.trim() || null,
      sortOrder: selectedSuite.value.sortOrder,
      status: suiteForm.value.status,
      version: selectedSuite.value.version,
    },
    await secureHeaders(),
  )
  if (result.data) {
    const idx = suites.value.findIndex((s) => s.id === result.data!.id)
    if (idx !== -1) suites.value[idx] = result.data
    showUpdated(t('tests.suite.record'), result.data.name)
  } else {
    error.value = problemMessage(result.error, t('tests.suite.updateFailed'))
  }
  saving.value = false
}

// CASE EDIT STATE & COMPUTED
const caseForm = ref({
  suiteId: '',
  title: '',
  description: '',
  preconditions: '',
  overallExpectedResult: '',
  status: 'active' as 'active' | 'inactive',
  tagIds: [] as string[],
  steps: [] as Array<{ key: number; action: string; expectedResult: string }>,
})
const nextCaseStepKey = ref(1)

function populateCaseForm(): void {
  if (!selectedCase.value) return
  caseForm.value = {
    suiteId: selectedCase.value.suiteId,
    title: selectedCase.value.title,
    description: selectedCase.value.description ?? '',
    preconditions: selectedCase.value.preconditions ?? '',
    overallExpectedResult: selectedCase.value.overallExpectedResult ?? '',
    status: selectedCase.value.status,
    tagIds: selectedCase.value.tags.map((tag) => tag.id),
    steps: selectedCase.value.steps.map((step, idx) => ({
      key: idx + 1,
      action: step.action,
      expectedResult: step.expectedResult,
    })),
  }
  nextCaseStepKey.value = selectedCase.value.steps.length + 1
}

watch(selectedCase, populateCaseForm, { immediate: true })

const isCaseDirty = computed(() => {
  if (!selectedCase.value) return false
  if (caseForm.value.title.trim() !== selectedCase.value.title) return true
  if (caseForm.value.suiteId !== selectedCase.value.suiteId) return true
  if (caseForm.value.description.trim() !== (selectedCase.value.description ?? '')) return true
  if (caseForm.value.preconditions.trim() !== (selectedCase.value.preconditions ?? '')) return true
  if (caseForm.value.overallExpectedResult.trim() !== (selectedCase.value.overallExpectedResult ?? '')) return true
  if (caseForm.value.status !== selectedCase.value.status) return true
  if (caseForm.value.tagIds.join(',') !== selectedCase.value.tags.map((tag) => tag.id).sort().join(',')) return true
  if (caseForm.value.steps.length !== selectedCase.value.steps.length) return true
  return caseForm.value.steps.some((step, idx) => {
    const orig = selectedCase.value!.steps[idx]
    return !orig || step.action.trim() !== orig.action || step.expectedResult.trim() !== orig.expectedResult
  })
})

function addCaseStep(): void {
  caseForm.value.steps.push({ key: nextCaseStepKey.value++, action: '', expectedResult: '' })
}

function removeCaseStep(index: number): void {
  if (caseForm.value.steps.length > 1) {
    caseForm.value.steps.splice(index, 1)
  }
}

async function saveCaseForm(): Promise<void> {
  if (!selectedCase.value || saving.value || !caseForm.value.title.trim() || !caseForm.value.steps.length) return
  saving.value = true
  const result = await apiClient.updateTestCase(
    workspaceId.value,
    selectedCase.value.id,
    {
      suiteId: caseForm.value.suiteId,
      title: caseForm.value.title.trim(),
      description: caseForm.value.description.trim() || null,
      preconditions: caseForm.value.preconditions.trim() || null,
      overallExpectedResult: caseForm.value.overallExpectedResult.trim() || null,
      sortOrder: selectedCase.value.sortOrder,
      status: caseForm.value.status,
      tagIds: caseForm.value.tagIds,
      version: selectedCase.value.version,
      steps: caseForm.value.steps.map((step) => ({
        action: step.action.trim(),
        expectedResult: step.expectedResult.trim(),
      })),
    },
    await secureHeaders(),
  )
  if (result.data) {
    const idx = cases.value.findIndex((c) => c.id === result.data!.id)
    if (idx !== -1) cases.value[idx] = result.data
    showUpdated(t('tests.testCase.record'), result.data.title)
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.updateFailed'))
  }
  saving.value = false
}

const isCreatingCase = ref(false)

function startCreateCase(suiteId?: string | null): void {
  router.push({
    name: 'test-case-new',
    params: { workspaceId: workspaceId.value },
    query: suiteId ? { suiteId } : {},
  })
}

function cancelCreateCase(): void {
  isCreatingCase.value = false
}

async function saveCreatedCase(): Promise<void> {
  const validSteps = caseForm.value.steps
    .map((s) => ({ action: s.action.trim(), expectedResult: s.expectedResult.trim() }))
    .filter((s) => s.action || s.expectedResult)

  if (saving.value || !caseForm.value.title.trim() || !validSteps.length) return
  saving.value = true

  const result = await apiClient.createTestCase(
    workspaceId.value,
    {
      suiteId: caseForm.value.suiteId,
      title: caseForm.value.title.trim(),
      description: caseForm.value.description.trim() || null,
      preconditions: caseForm.value.preconditions.trim() || null,
      overallExpectedResult: caseForm.value.overallExpectedResult.trim() || null,
      sortOrder: (allCasesBySuite.value.get(caseForm.value.suiteId)?.length ?? 0) + 1,
      tagIds: caseForm.value.tagIds,
      steps: validSteps,
    },
    await secureHeaders(),
  )

  if (result.data) {
    cases.value.push(result.data)
    isCreatingCase.value = false
    selectCaseItem(result.data)
    showUpdated(t('tests.testCase.record'), result.data.title)
  } else {
    error.value = problemMessage(result.error, t('tests.testCase.createFailed', '建立測試案例失敗'))
  }
  saving.value = false
}

function selectSuiteItem(suiteId: string | null): void {
  isCreatingCase.value = false
  selectedSuiteId.value = suiteId
  selectedCaseId.value = null
  populateSuiteForm()
}

function selectCaseItem(testCase: TestCaseResponse): void {
  isCreatingCase.value = false
  selectedCaseId.value = testCase.id
  selectedSuiteId.value = testCase.suiteId
  populateCaseForm()
}

function openCaseFromList(testCase: TestCaseResponse): void {
  suiteView.value = 'tree'
  selectCaseItem(testCase)
}

function handleEmbeddedCaseSaved(updatedCase: TestCaseResponse): void {
  const index = cases.value.findIndex((item) => item.id === updatedCase.id)
  if (index !== -1) cases.value[index] = updatedCase
  showUpdated(t('tests.testCase.record'), updatedCase.title)
}

function handleEmbeddedCaseCancel(): void {
  selectSuiteItem(selectedCase.value?.suiteId ?? selectedSuiteId.value)
}

async function load(): Promise<void> {
  loading.value = true
  const [workspaceResult, memberResult, suiteResult, caseResult, tagResult, linkedProjectResult, projectResult] = await Promise.all([
    apiClient.getTestWorkspace(workspaceId.value),
    apiClient.listTestWorkspaceMembers(workspaceId.value),
    apiClient.listTestSuites(workspaceId.value),
    apiClient.listTestCases(workspaceId.value),
    apiClient.listTestTags(),
    apiClient.listTestWorkspaceProjects(workspaceId.value),
    apiClient.listProjects(),
  ])
  workspace.value = workspaceResult.data
  members.value = memberResult.data ?? []
  suites.value = suiteResult.data ?? []
  cases.value = caseResult.data ?? []
  tags.value = tagResult.data?.filter((tag) => tag.status === 'active') ?? []
  linkedProjects.value = (linkedProjectResult.data ?? [])
    .sort((left, right) => left.code.localeCompare(right.code))
  projects.value = projectResult.data ?? []
  error.value = problemMessage(
    workspaceResult.error ?? memberResult.error ?? suiteResult.error ?? caseResult.error
      ?? linkedProjectResult.error ?? projectResult.error,
    workspace.value ? '' : t('tests.workspace.loadFailed'),
  )

  if (typeof route.query.caseId === 'string') {
    const targetCase = cases.value.find((c) => c.id === route.query.caseId)
    if (targetCase) {
      selectCaseItem(targetCase)
    }
  } else {
    const firstSuite = orderedSuites.value[0]
    if (firstSuite && selectedSuiteId.value === null) {
      selectedSuiteId.value = firstSuite.id
    }
  }

  loading.value = false
}

async function linkProject(): Promise<void> {
  if (!selectedProjectId.value) return
  linkingProject.value = true
  error.value = ''
  const result = await apiClient.linkTestWorkspaceProject(
    workspaceId.value,
    { projectId: selectedProjectId.value },
    await secureHeaders(),
  )
  if (result.data) {
    linkedProjects.value = [...linkedProjects.value, result.data]
      .sort((left, right) => left.code.localeCompare(right.code))
    selectedProjectId.value = ''
    showCreated(t('tests.workspace.projectLinkRecord'), result.data.name)
  } else {
    error.value = problemMessage(result.error, t('tests.workspace.projectLinkFailed'))
  }
  linkingProject.value = false
}

async function unlinkProject(link: TestWorkspaceProjectResponse): Promise<void> {
  linkingProject.value = true
  error.value = ''
  const result = await apiClient.unlinkTestWorkspaceProject(
    workspaceId.value,
    link.projectId,
    link.version,
    await secureHeaders(),
  )
  if (!result.error) {
    linkedProjects.value = linkedProjects.value.filter((item) => item.id !== link.id)
    showUpdated(t('tests.workspace.projectLinkRecord'), link.name)
  } else {
    error.value = problemMessage(result.error, t('tests.workspace.projectUnlinkFailed'))
  }
  linkingProject.value = false
}

function openProject(link: TestWorkspaceProjectResponse): void {
  void router.push({ name: 'project-detail', params: { projectId: link.projectId } })
}

async function addMember(): Promise<void> {
  saving.value = true
  const result = await apiClient.addTestWorkspaceMember(
    workspaceId.value,
    { username: username.value.trim(), role: memberRole.value },
    await secureHeaders(),
  )
  if (result.data) {
    members.value.push(result.data)
    showCreated(t('tests.member.record'), result.data.username)
    username.value = ''
  } else {
    error.value = problemMessage(result.error, t('tests.member.addFailed'))
  }
  saving.value = false
}

async function changeRole(member: TestWorkspaceMemberResponse): Promise<void> {
  saving.value = true
  const result = await apiClient.updateTestWorkspaceMember(
    workspaceId.value,
    member.id,
    { role: member.role, version: member.version },
    await secureHeaders(),
  )
  if (result.data) {
    Object.assign(member, result.data)
    showUpdated(t('tests.member.record'), result.data.username)
  } else {
    error.value = problemMessage(result.error, t('tests.member.updateFailed'))
    await load()
  }
  saving.value = false
}

async function removeMember(member: TestWorkspaceMemberResponse): Promise<void> {
  saving.value = true
  const result = await apiClient.removeTestWorkspaceMember(
    workspaceId.value,
    member.id,
    member.version,
    await secureHeaders(),
  )
  if (!result.error) {
    members.value = members.value.filter((item) => item.id !== member.id)
  } else {
    error.value = problemMessage(result.error, t('tests.member.removeFailed'))
  }
  saving.value = false
}

async function saveWorkspace(): Promise<void> {
  if (!workspace.value) return
  saving.value = true
  const result = await apiClient.updateTestWorkspace(workspaceId.value, {
    name: workspace.value.name,
    description: workspace.value.description,
    status: workspace.value.status,
    version: workspace.value.version,
  }, await secureHeaders())
  if (result.data) {
    workspace.value = result.data
    showUpdated(t('tests.workspace.createdRecord'), result.data.name)
  } else {
    error.value = problemMessage(result.error, t('tests.workspace.updateFailed'))
  }
  saving.value = false
}



async function saveWorkspaceFromSharedSettings(payload: {
  name: string
  description: string
  status: 'active' | 'inactive'
}): Promise<void> {
  if (!workspace.value) return
  saving.value = true
  error.value = ''
  const result = await apiClient.updateTestWorkspace(
    workspaceId.value,
    {
      name: payload.name,
      description: payload.description || null,
      status: payload.status,
      version: workspace.value.version,
    },
    await secureHeaders(),
  )
  if (result.data) {
    workspace.value = result.data
    showUpdated(t('tests.workspace.createdRecord'), result.data.name)
  } else {
    error.value = problemMessage(result.error, t('tests.workspace.updateFailed'))
  }
  saving.value = false
}

function formatDate(value: string): string {
  return d(new Date(value), 'medium')
}

onMounted(load)
</script>

<template>
  <section v-if="workspace" class="page">
    <SharedBreadcrumb
      show-back
      :back-to="{ name: 'test-workspaces' }"
      :back-label="t('shell.navigation.backToWorkspaces')"
      :items="[
        { label: t('shell.navigation.workspaceList', '測試工作區'), to: { name: 'test-workspaces' } },
        { label: workspace.name, to: { name: 'test-home', params: { workspaceId } } },
        { label: tabLabel, active: true },
      ]"
    />
    <ResourcePageHeader
      :meta="`${workspace.prefix} · TEST WORKSPACE · ${workspace.currentUserRole}`"
      :title="workspace.name"
      :subtitle="workspace.description || t('tests.workspace.defaultDescription')"
      :status="tab === 'members' ? '' : workspace.status"
    >
      <UiButton
        v-if="tab === 'suites' && canManage"
        @click="startCreateCase()"
      >
        <Plus :size="16" /> {{ t('tests.testCase.create') }}
      </UiButton>
      <UiButton v-if="tab === 'members' && canManage" @click="addingMember = !addingMember">
        <Plus :size="16" />{{ t(addingMember ? 'common.members.cancelAdd' : 'common.members.add') }}
      </UiButton>
    </ResourcePageHeader>

    <p v-if="error" class="error">{{ error }}</p>

    <!-- VIEW TABS FOR HOME (分頁標籤列) -->
    <SharedViewTabs
      v-if="tab === 'home'"
      model-value="home"
      :tabs="[
        { key: 'home', label: t('tests.workspace.homeTitle'), icon: LayoutDashboard }
      ]"
    />

    <!-- TAB 0: LINKED PROJECTS -->
    <SharedCardSection
      v-if="tab === 'home'"
      :icon="FolderKanban"
      :title="t('tests.workspace.projectsTitle')"
      :description="t('tests.workspace.projectsDescription')"
    >
      <template v-if="canManageProjectLinks" #headerRight>
        <div class="project-link-controls">
          <UiSelect
            v-model="selectedProjectId"
            :disabled="linkingProject || availableProjects.length === 0"
            :aria-label="t('tests.workspace.selectProject')"
          >
            <option value="">
              {{ availableProjects.length
                ? t('tests.workspace.selectProject')
                : t('tests.workspace.noAvailableProjects') }}
            </option>
            <option v-for="project in availableProjects" :key="project.id" :value="project.id">
              {{ project.code }} · {{ project.name }}
            </option>
          </UiSelect>
          <UiButton
            :loading="linkingProject"
            :disabled="!selectedProjectId"
            @click="linkProject"
          >
            <Link2 :size="16" />{{ t('tests.workspace.linkProject') }}
          </UiButton>
        </div>
      </template>

      <div v-if="linkedProjects.length" class="linked-project-list">
        <article v-for="project in linkedProjects" :key="project.id" class="linked-project-card">
          <div class="linked-project-identity">
            <span class="linked-project-icon"><FolderKanban :size="20" /></span>
            <div>
              <code>{{ project.code }}</code>
              <strong>{{ project.name }}</strong>
              <small>{{ t('tests.workspace.linkedAt', { date: formatDate(project.linkedAt) }) }}</small>
            </div>
          </div>
          <div class="linked-project-actions">
            <UiStatusBadge :variant="project.status === 'active' ? 'success' : 'inactive'">
              {{ t(`common.status.${project.status}`) }}
            </UiStatusBadge>
            <UiButton variant="secondary" @click="openProject(project)">
              <ExternalLink :size="15" />{{ t('tests.workspace.openProject') }}
            </UiButton>
            <UiButton
              v-if="canManageProjectLinks"
              variant="ghost"
              :disabled="linkingProject"
              @click="unlinkProject(project)"
            >
              <Unlink :size="15" />{{ t('tests.workspace.unlinkProject') }}
            </UiButton>
          </div>
        </article>
      </div>
      <UiEmptyState
        v-else
        :icon="FolderKanban"
        :title="t('tests.workspace.noProjects')"
        :description="t('tests.workspace.noProjectsDescription')"
      />
    </SharedCardSection>

    <!-- VIEW TABS FOR SUITES (分頁標籤列) -->
    <SharedViewTabs
      v-if="tab === 'suites'"
      v-model="suiteView"
      :tabs="[
        { key: 'tree', label: t('tests.suite.treeView'), icon: FolderTree },
        { key: 'list', label: t('tests.suite.listView'), icon: List }
      ]"
    />

    <!-- TAB 1: SUITES & CASES VSCODE STYLE SPLIT VIEW -->
    <div v-if="tab === 'suites' && suiteView === 'tree'" class="suite-split-layout">
      <!-- LEFT SIDEBAR: VSCODE STYLE EXPLORER TREE -->
      <aside class="panel suite-sidebar">
        <header class="sidebar-header">
          <div>
            <h3>{{ t('tests.suite.treeTitle') }}</h3>
            <p>{{ t('tests.suite.treeDescription') }}</p>
          </div>
        </header>
        <!-- ALL CASES SELECTION -->
        <div
          class="tree-item all-cases-item"
          :class="{ active: selectedSuiteId === null && selectedCaseId === null }"
          @click="selectSuiteItem(null)"
        >
          <Layers :size="16" class="tree-item-icon" />
          <span class="tree-label">{{ t('tests.workspace.allCases') }}</span>
          <span class="badge">{{ cases.length }}</span>
        </div>

        <!-- TREE LIST (SUITES & NESTED TEST CASES) -->
        <div v-if="visibleTreeNodes.length" class="tree-list">
          <template v-for="node in visibleTreeNodes" :key="node.id">
            <!-- SUITE FOLDER ITEM -->
            <div
              v-if="node.type === 'suite' && node.suite"
              class="tree-item suite-tree-item"
              :class="{
                active: selectedSuiteId === node.suite.id && selectedCaseId === null,
                inactive: node.suite.status === 'inactive'
              }"
              :style="{ paddingLeft: `${(node.suite.depth - 1) * 16 + 8}px` }"
              @click="selectSuiteItem(node.suite.id)"
            >
              <!-- COLLAPSE CHEVRON -->
              <button
                v-if="hasChildren(node.suite.id) || (allCasesBySuite.get(node.suite.id)?.length ?? 0) > 0"
                type="button"
                class="chevron-btn"
                @click.stop="toggleCollapse(node.suite.id)"
              >
                <ChevronDown v-if="!isCollapsed(node.suite.id)" :size="14" />
                <ChevronRight v-else :size="14" />
              </button>
              <span v-else class="chevron-spacer"></span>

              <!-- FOLDER ICON -->
              <FolderOpen v-if="selectedSuiteId === node.suite.id || !isCollapsed(node.suite.id)" :size="16" class="tree-item-icon folder" />
              <Folder v-else :size="16" class="tree-item-icon folder" />

              <!-- SUITE NAME -->
              <span class="tree-label" :title="node.suite.name">{{ node.suite.name }}</span>

              <!-- CASE COUNT BADGE -->
              <span class="badge">{{ allCasesBySuite.get(node.suite.id)?.length ?? 0 }}</span>

              <!-- HOVER ACTIONS -->
              <div class="hover-actions" @click.stop>
                <button
                  v-if="canManage && node.suite.status === 'active'"
                  type="button"
                  class="action-btn"
                  :title="t('tests.testCase.add')"
                  @click="startCreateCase(node.suite.id)"
                >
                  <Plus :size="13" />
                </button>
                <button
                  v-if="canManage && node.suite.depth < 5"
                  type="button"
                  class="action-btn"
                  :title="t('tests.suite.addChild')"
                  @click="router.push({ name: 'test-suite-new', params: { workspaceId }, query: { parentId: node.suite.id } })"
                >
                  <FolderPlus :size="13" />
                </button>
              </div>
            </div>

            <!-- TEST CASE ITEM -->
            <div
              v-else-if="node.type === 'case' && node.testCase"
              class="tree-item case-tree-item"
              :class="{
                active: selectedCaseId === node.testCase.id,
                inactive: node.testCase.status === 'inactive'
              }"
              :style="{ paddingLeft: `${node.depth * 16 + 2}px` }"
              @click="selectCaseItem(node.testCase)"
            >
              <FileCheck2 :size="15" class="tree-item-icon case-file-icon" />
              <span class="case-code-prefix">{{ workspace.prefix }}-TC{{ node.testCase.caseNo }}</span>
              <span class="tree-label" :title="node.testCase.title">{{ node.testCase.title }}</span>
            </div>
          </template>
        </div>
        <div v-else class="empty-tree">
          <Folder :size="24" />
          <p>{{ t('tests.suite.empty') }}</p>
        </div>

        <footer v-if="canManage" class="tree-create-actions">
          <button
            type="button"
            class="tree-item create-suite-tree-item"
            @click="router.push({ name: 'test-suite-new', params: { workspaceId } })"
          >
            <span class="chevron-spacer"></span>
            <FolderPlus :size="16" class="tree-item-icon folder" />
            {{ t('tests.suite.create') }}
          </button>
        </footer>
      </aside>

      <!-- RIGHT MAIN PANEL: CASE DETAIL VIEW, EDIT FORM, OR SUITE CASES LIST -->
      <main
        class="panel case-main-panel"
        :class="{ 'mode-suite': !selectedCase && !isCreatingCase, 'mode-case': Boolean(selectedCase) || isCreatingCase }"
      >
        <!-- INLINE CASE CREATION FORM -->
        <template v-if="selectedCase">
          <TestCaseEditForm
            embedded
            :workspace-id="workspaceId"
            :workspace="workspace"
            :test-case="selectedCase"
            :suites="suites"
            @saved="handleEmbeddedCaseSaved"
            @cancel="handleEmbeddedCaseCancel"
          />
        </template>

        <template v-else-if="isCreatingCase">
          <header class="case-panel-header">
            <div class="header-main-col">
              <div class="header-top-row">
                <div class="breadcrumb">
                  <span>{{ workspace.name }}</span>
                </div>
              </div>
              <div class="header-title-row">
                <label class="case-title-field">
                  <span>{{ t('tests.testCase.title') }} *</span>
                  <input
                    v-model="caseForm.title"
                    class="editable-title-input"
                    :placeholder="t('tests.testCase.titlePlaceholder')"
                    required
                    autofocus
                  />
                </label>
              </div>
            </div>

            <div class="header-actions">
              <select v-model="caseForm.status" class="status-select-pill" :class="caseForm.status">
                <option value="active">{{ t('common.status.active') }}</option>
                <option value="inactive">{{ t('common.status.inactive') }}</option>
              </select>
              <button
                type="button"
                class="btn-primary"
                :disabled="saving || !caseForm.title.trim() || !caseForm.steps.some(s => s.action.trim() && s.expectedResult.trim())"
                @click="saveCreatedCase"
              >
                <Save :size="14" /> {{ saving ? t('tests.workspace.loading') : t('tests.testCase.createAction') }}
              </button>
              <button
                type="button"
                class="btn-subtle"
                :disabled="saving"
                @click="cancelCreateCase"
              >
                {{ t('common.actions.cancel') }}
              </button>
            </div>
          </header>

          <div class="case-detail-body">
            <section class="detail-section">
              <h4>{{ t('tests.testCase.description') }}</h4>
              <textarea v-model="caseForm.description" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.descriptionPlaceholder')"></textarea>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.tags') }}</h4>
              <select v-model="caseForm.tagIds" class="tag-select" multiple>
                <option v-for="tag in tags" :key="tag.id" :value="tag.id">{{ tag.name }}</option>
              </select>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.preconditions') }}</h4>
              <textarea v-model="caseForm.preconditions" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.preconditionsPlaceholder')"></textarea>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.overallExpectedResult') }}</h4>
              <textarea v-model="caseForm.overallExpectedResult" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.expectedPlaceholder')"></textarea>
            </section>

            <!-- STEPS SECTION -->
            <section class="detail-section">
              <div class="section-header-row">
                <h4>{{ t('tests.testCase.steps') }} ({{ caseForm.steps.length }})</h4>
                <button type="button" class="btn-subtle" @click="addCaseStep">
                  <Plus :size="14" /> {{ t('tests.testCase.addStep') }}
                </button>
              </div>
              <div class="step-editor-list">
                <article v-for="(stepItem, index) in caseForm.steps" :key="stepItem.key" class="step-editor-card">
                  <div class="step-card-header">
                    <span><GripVertical :size="15" /> {{ t('tests.testCase.stepNumber', { number: index + 1 }) }}</span>
                    <button
                      v-if="caseForm.steps.length > 1"
                      type="button"
                      class="btn-subtle btn-danger-subtle"
                      @click="removeCaseStep(index)"
                    >
                      <Trash2 :size="14" /> {{ t('tests.testCase.removeStep') }}
                    </button>
                  </div>

                  <div class="step-fields-grid">
                    <label class="form-field">
                      <span>{{ t('tests.testCase.action') }} *</span>
                      <textarea v-model="stepItem.action" class="editable-textarea" rows="2" required></textarea>
                    </label>
                    <label class="form-field">
                      <span>{{ t('tests.testCase.expectedResult') }} *</span>
                      <textarea v-model="stepItem.expectedResult" class="editable-textarea" rows="2" required></textarea>
                    </label>
                  </div>
                </article>
              </div>
            </section>
          </div>
        </template>

        <!-- ALWAYS-EDITABLE SINGLE CASE DETAIL VIEW -->
        <template v-else-if="legacySelectedCase">
          <!-- SINGLE CASE HEADER -->
          <header class="case-panel-header">
            <div class="header-main-col">
              <div class="header-top-row">
                <div class="breadcrumb">
                  <span>{{ workspace.name }}</span>
                </div>
              </div>
              <div class="header-title-row">
                <input v-model="caseForm.title" class="editable-title-input" :placeholder="t('tests.testCase.titlePlaceholderShort')" required />
              </div>
              <p class="suite-desc">{{ t('tests.workspace.caseNumber', { code: `${workspace.prefix}-TC${legacySelectedCase.caseNo}` }) }} · {{ t('tests.testCase.stepCount', { count: legacySelectedCase.steps.length }) }}</p>
            </div>

            <div class="header-actions">
              <select v-model="caseForm.status" class="status-select-pill" :class="caseForm.status">
                <option value="active">{{ t('common.status.active') }}</option>
                <option value="inactive">{{ t('common.status.inactive') }}</option>
              </select>
              <button
                type="button"
                class="btn-primary"
                :disabled="saving || !isCaseDirty || !caseForm.title.trim() || !caseForm.steps.length"
                @click="saveCaseForm"
              >
                <Save :size="14" /> {{ saving ? t('tests.workspace.loading') : t('tests.testCase.saveAction') }}
              </button>
            </div>
          </header>

          <!-- CASE DETAIL BODY -->
          <div class="case-detail-body">
            <section class="detail-section">
              <label class="form-field">
                <span>{{ t('tests.testCase.suite') }}</span>
                <select v-model="caseForm.suiteId" class="case-suite-select">
                  <option v-for="suite in suites" :key="suite.id" :value="suite.id">
                    {{ getSuiteFullPath(suite.id) }}
                  </option>
                </select>
              </label>
            </section>
            <section class="detail-section">
              <h4>{{ t('tests.testCase.description') }}</h4>
              <textarea v-model="caseForm.description" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.descriptionPlaceholder')"></textarea>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.tags') }}</h4>
              <select v-model="caseForm.tagIds" class="tag-select" multiple>
                <option v-for="tag in tags" :key="tag.id" :value="tag.id">{{ tag.name }}</option>
              </select>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.preconditions') }}</h4>
              <textarea v-model="caseForm.preconditions" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.preconditionsPlaceholder')"></textarea>
            </section>

            <section class="detail-section">
              <h4>{{ t('tests.testCase.overallExpectedResult') }}</h4>
              <textarea v-model="caseForm.overallExpectedResult" class="editable-textarea" rows="2" :placeholder="t('tests.testCase.expectedPlaceholder')"></textarea>
            </section>

            <!-- EDITABLE STEPS SECTION -->
            <section class="detail-section">
              <div class="section-header-row">
                <h4>{{ t('tests.testCase.steps') }} ({{ caseForm.steps.length }})</h4>
                <button type="button" class="btn-subtle" @click="addCaseStep">
                  <Plus :size="14" /> {{ t('tests.testCase.addStep') }}
                </button>
              </div>
              <div class="step-editor-list">
                <article v-for="(stepItem, index) in caseForm.steps" :key="stepItem.key" class="step-editor-card">
                  <div class="step-card-header">
                    <span><GripVertical :size="15" /> {{ t('tests.testCase.stepNumber', { number: index + 1 }) }}</span>
                    <button
                      v-if="caseForm.steps.length > 1"
                      type="button"
                      class="btn-subtle btn-danger-subtle"
                      @click="removeCaseStep(index)"
                    >
                      <Trash2 :size="14" /> {{ t('tests.testCase.removeStep') }}
                    </button>
                  </div>

                  <div class="step-fields-grid">
                    <label class="form-field">
                      <span>{{ t('tests.testCase.action') }} *</span>
                      <textarea v-model="stepItem.action" class="editable-textarea" rows="2" required></textarea>
                    </label>
                    <label class="form-field">
                      <span>{{ t('tests.testCase.expectedResult') }} *</span>
                      <textarea v-model="stepItem.expectedResult" class="editable-textarea" rows="2" required></textarea>
                    </label>
                  </div>
                </article>
              </div>
            </section>
          </div>
        </template>

        <!-- ALWAYS-EDITABLE SUITE VIEW -->
        <template v-else>
          <!-- SUITE HEADER -->
          <header class="case-panel-header">
            <div class="header-main-col">
              <div class="header-top-row">
                <div class="breadcrumb">
                  <span>{{ workspace.name }}</span>
                  <template v-if="selectedSuite">
                    <span> / </span>
                    <select v-model="suiteForm.parentId" class="select-breadcrumb">
                      <option :value="null">{{ t('tests.suite.root') }}</option>
                      <option v-for="s in eligibleParentSuites" :key="s.id" :value="s.id">
                        {{ getSuiteFullPath(s.id) }}
                      </option>
                    </select>
                  </template>
                </div>
              </div>
              <div v-if="selectedSuite" class="header-title-row">
                <input v-model="suiteForm.name" class="editable-title-input" placeholder="測試套件名稱" required />
              </div>
              <template v-else>
                <h2 class="header-static-title">{{ t('tests.workspace.allCases') }}</h2>
                <p class="suite-desc">{{ t('tests.workspace.caseCount', { count: cases.length }) }}</p>
              </template>
            </div>

            <div class="header-actions">
              <button
                v-if="canManage && selectedSuite"
                type="button"
                class="btn-primary"
                :disabled="saving || !isSuiteDirty || !suiteForm.name.trim()"
                @click="saveSuiteForm"
              >
                <Save :size="14" /> {{ saving ? t('tests.workspace.loading') : t('tests.testCase.saveAction') }}
              </button>
            </div>
            <textarea
              v-if="selectedSuite"
              v-model="suiteForm.description"
              class="editable-textarea desc-input"
              placeholder="描述此測試套件的目的..."
              rows="2"
            ></textarea>
          </header>

          <!-- DIRECT CHILD SUITES, THEN DIRECT CASES -->
          <div v-if="childSuitesForSelectedSuite.length || casesForSelectedSuite.length" class="case-card-list">
            <article
              v-for="suite in childSuitesForSelectedSuite"
              :key="suite.id"
              class="case-card suite-card"
              @click="selectSuiteItem(suite.id)"
            >
              <div class="case-card-icon"><Folder :size="18" /></div>
              <div class="case-card-content">
                <div class="case-card-title-row">
                  <strong class="case-title">{{ suite.name }}</strong>
                  <span class="child-suite-label">{{ t('tests.suite.record') }}</span>
                </div>
                <p class="case-desc-preview">{{ suite.description || t('tests.suite.noDescription') }}</p>
              </div>
            </article>
            <article
              v-for="testCase in casesForSelectedSuite"
              :key="testCase.id"
              class="case-card"
              :class="{ inactive: testCase.status === 'inactive' }"
              @click="selectCaseItem(testCase)"
            >
              <div class="case-card-icon">
                <FileCheck2 :size="18" />
              </div>
              <div class="case-card-content">
                <div class="case-card-title-row">
                  <strong class="case-title">{{ testCase.title }}</strong>
                  <div class="card-title-right">
                    <span class="case-status-badge" :class="testCase.status">
                      {{ t(`tests.workspace.${testCase.status}`) }}
                    </span>
                  </div>
                </div>
                <p v-if="testCase.description" class="case-desc-preview">{{ testCase.description }}</p>
                <div class="case-meta">
                  <span class="meta-item">{{ t('tests.testCase.stepCount', { count: testCase.steps.length }) }}</span>
                  <span v-if="testCase.preconditions" class="meta-item">前置：{{ testCase.preconditions }}</span>
                </div>
              </div>
            </article>

          </div>

          <!-- EMPTY STATE -->
          <div v-else class="empty-cases">
            <FileCheck2 :size="36" />
            <h4>{{ t('tests.testCase.suiteEmptyTitle') }}</h4>
            <p>{{ t('tests.testCase.suiteEmptyDescription') }}</p>
          </div>
          <footer
            v-if="canManage && selectedSuite"
            class="suite-create-actions"
          >
            <button
              v-if="selectedSuite.depth < 5"
              type="button"
              class="btn-subtle"
              @click="router.push({ name: 'test-suite-new', params: { workspaceId }, query: { parentId: selectedSuite.id } })"
            >
              <FolderPlus :size="15" /> {{ t('tests.suite.createChild') }}
            </button>
            <button type="button" class="btn-primary" @click="startCreateCase(selectedSuite.id)">
              <Plus :size="15" /> {{ t('tests.testCase.create') }}
            </button>
          </footer>
        </template>
      </main>
    </div>

    <SharedCardSection
      v-else-if="tab === 'suites' && suiteView === 'list'"
      class="case-list-view"
      :title="t('tests.workspace.allCases')"
      :description="t('tests.testCase.listDescription')"
    >
      <template #headerRight>
        <span class="count-badge">{{ t('tests.workspace.caseCount', { count: filteredCases.length }) }}</span>
      </template>
      <SharedFilterToolbar align="start" class="case-filter-toolbar">
          <SharedSearchField
            v-model="caseQuery"
            :placeholder="t('tests.workspace.caseSearchPlaceholder')"
            :clear-label="t('common.search.clear')"
          />
          <select v-model="caseStatusFilter">
            <option value="">{{ t('tests.workspace.allCaseStatuses') }}</option>
            <option value="active">{{ t('common.status.active') }}</option>
            <option value="inactive">{{ t('common.status.inactive') }}</option>
          </select>
          <select v-model="caseTagFilter">
            <option value="">{{ t('tests.workspace.allTags') }}</option>
            <option v-for="tag in tags" :key="tag.id" :value="tag.id">{{ tag.name }}</option>
          </select>
      </SharedFilterToolbar>
      <div v-if="filteredCases.length" class="case-table-wrap">
        <table class="case-table">
          <thead>
            <tr>
              <th>{{ t('tests.testCase.columns.code') }}</th>
              <th>{{ t('tests.testCase.columns.title') }}</th>
              <th>{{ t('tests.testCase.columns.path') }}</th>
              <th>{{ t('tests.testCase.columns.tags') }}</th>
              <th>{{ t('tests.testCase.columns.steps') }}</th>
              <th>{{ t('tests.testCase.columns.status') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
          v-for="testCase in filteredCases"
          :key="testCase.id"
          :class="{ inactive: testCase.status === 'inactive' }"
          @click="openCaseFromList(testCase)"
            >
              <td><code>{{ workspace.prefix }}-TC{{ testCase.caseNo }}</code></td>
              <td><strong>{{ testCase.title }}</strong><small>{{ testCase.description || t('tests.testCase.noDescription') }}</small></td>
              <td class="suite-path-cell">{{ getSuiteFullPath(testCase.suiteId) }}</td>
              <td>{{ testCase.tags.map((tag) => tag.name).join(' · ') || '—' }}</td>
              <td>{{ t('tests.testCase.stepCount', { count: testCase.steps.length }) }}</td>
              <td><span class="case-status-badge" :class="testCase.status">{{ t(`tests.workspace.${testCase.status}`) }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else class="empty-cases">
        <FileCheck2 :size="36" />
        <h4>{{ t('tests.testCase.suiteEmptyTitle') }}</h4>
        <p>{{ t('tests.testCase.suiteEmptyDescription') }}</p>
      </div>
    </SharedCardSection>

    <!-- VIEW TABS FOR MEMBERS (分頁標籤列) -->
    <SharedViewTabs
      v-if="tab === 'members'"
      model-value="list"
      :tabs="[
        { key: 'list', label: '列表', icon: List }
      ]"
    />

    <!-- TAB 2: MEMBERS -->
    <ResourceMemberManager
      v-if="tab === 'members'"
      resource-type="test-workspace"
      :resource-id="workspaceId"
      :title="t('tests.member.title')"
      :description="t('tests.member.description')"
      :can-add="canManage"
      :can-edit-role="canManage"
      :can-remove="canManage"
      :show-add-action="false"
      v-model:adding="addingMember"
    />

    <!-- VIEW TABS FOR SETTINGS (分頁標籤列) -->
    <SharedViewTabs
      v-if="tab === 'settings'"
      model-value="settings"
      :tabs="[
        { key: 'settings', label: '設定', icon: List }
      ]"
    />

    <!-- TAB 3: SETTINGS -->
    <SharedResourceSettings
      v-if="tab === 'settings' && workspace"
      :title="t('tests.workspace.settings')"
      :section-description="t('tests.workspace.settingsDescription')"
      :version="workspace.version"
      :name="workspace.name"
      :code-or-prefix="workspace.prefix"
      :code-label="t('tests.workspace.prefix')"
      :description="workspace.description ?? ''"
      :status="workspace.status"
      :can-edit="canManage"
      :can-change-status="canManage"
      :loading="loading"
      :saving="saving"
      :error="error"
      @save="saveWorkspaceFromSharedSettings"
    />
  </section>
  <p v-else class="loading">{{ loading ? t('tests.workspace.loading') : t('tests.workspace.notFound') }}</p>
</template>

<style scoped>
.page { display: flex; flex-direction: column; gap: 20px; width: 100%; box-sizing: border-box; }

/* VSCODE STYLE SPLIT VIEW LAYOUT */
.suite-split-layout {
  display: grid;
  grid-template-columns: 360px minmax(0, 1fr);
  gap: 20px;
  align-items: start;
}

.case-list-view {
  min-height: 400px;
}

.case-filter-toolbar { margin-bottom: 16px; }
.case-table-wrap {
  overflow: hidden;
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.case-table { width: 100%; border-collapse: collapse; font-size: .84rem; }
.case-table th,
.case-table td { padding: 12px 14px; text-align: left; border-bottom: 1px solid var(--kk-border); }
.case-table th { color: var(--kk-text-muted); background: var(--kk-surface-subtle); font-size: .75rem; font-weight: 700; }
.case-table tbody tr { cursor: pointer; }
.case-table tbody tr:hover { background: var(--kk-accent-soft); }
.case-table tbody tr:last-child td { border-bottom: 0; }
.case-table td small { display: block; margin-top: 4px; color: var(--kk-text-muted); }
.case-table td code { color: var(--kk-accent); font-weight: 700; }
.case-table tbody tr.inactive { opacity: .58; }
.suite-path-cell { color: var(--kk-text-muted); }


.suite-sidebar {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 16px;
  background: white;
  border: 1px solid var(--kk-border);
  border-radius: 12px;
}

.sidebar-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 6px;
}

.btn-icon-primary {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  padding: 0;
  color: white;
  background: var(--kk-accent);
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.15s ease;
}
.btn-icon-primary:hover {
  background: #1b5e37;
}

.tree-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.tree-create-actions {
  margin-top: 2px;
}

.create-suite-tree-item {
  width: 100%;
  color: var(--kk-accent);
  background: transparent;
  border: 0;
  font: inherit;
  font-weight: 700;
  text-align: left;
  cursor: pointer;
}

.create-suite-tree-item:hover {
  background: var(--kk-accent-soft);
}

.tree-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 8px;
  min-height: 34px;
  border-radius: 6px;
  cursor: pointer;
  user-select: none;
  transition: background 0.12s ease;
  color: var(--kk-text);
  font-size: 0.85rem;
}

.tree-item:hover {
  background: var(--kk-surface-subtle);
}

.tree-item.active {
  background: #eaf5ee;
  color: #1b5e37;
  font-weight: 600;
}

.case-tree-item {
  color: #4a5550;
  font-size: 0.82rem;
}
.case-tree-item.active {
  background: #e0f2e6;
  color: #154c2c;
  font-weight: 600;
}

.case-code-prefix {
  font-family: monospace;
  font-size: 0.75rem;
  padding: 1px 5px;
  border-radius: 4px;
  background: rgba(0, 0, 0, 0.05);
  color: var(--kk-text-muted);
  flex-shrink: 0;
}

.tree-item.inactive {
  opacity: 0.55;
}

.tree-item-icon {
  flex-shrink: 0;
  color: var(--kk-accent);
}
.tree-item-icon.case-file-icon {
  color: #4b8b65;
}

.tree-label {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.badge {
  padding: 2px 7px;
  font-size: 0.72rem;
  font-weight: 600;
  border-radius: 10px;
  background: rgba(0, 0, 0, 0.05);
  color: var(--kk-text-muted);
}
.tree-item.active .badge {
  background: #d4ebd9;
  color: #1b5e37;
}

.chevron-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  padding: 0;
  background: transparent;
  border: none;
  border-radius: 4px;
  color: var(--kk-text-muted);
  cursor: pointer;
  flex-shrink: 0;
}
.chevron-btn:hover {
  background: rgba(0, 0, 0, 0.08);
}
.chevron-spacer {
  width: 18px;
  flex-shrink: 0;
}

.hover-actions {
  display: flex;
  align-items: center;
  gap: 3px;
  opacity: 0;
  transition: opacity 0.12s ease;
}

.tree-item:hover .hover-actions {
  opacity: 1;
}

.action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  padding: 0;
  background: white;
  border: 1px solid var(--kk-border);
  border-radius: 4px;
  color: var(--kk-accent);
  cursor: pointer;
}
.action-btn:hover {
  background: var(--kk-accent);
  color: white;
  border-color: var(--kk-accent);
}

.all-cases-item {
  margin-bottom: 4px;
  border-bottom: 1px solid var(--kk-border);
  border-radius: 7px;
  padding-left: 10px !important;
}

.tree-filters {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 8px;
  padding: 8px 0 12px;
}

.tree-filters input,
.tree-filters select {
  min-width: 0;
  height: 32px;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  padding: 0 8px;
  color: var(--kk-text);
  background: #fff;
}

.empty-tree {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 24px 0;
  gap: 8px;
  color: var(--kk-text-muted);
}

/* RIGHT MAIN PANEL - one stable frame for suite, case, and creation modes */
.case-main-panel {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0;
  border-radius: 12px;
  background: #ffffff;
  border: 1px solid var(--kk-border);
  min-height: 400px;
  overflow: hidden;
}

.case-main-panel.mode-suite {
  background: #ffffff;
  border-color: #bee3cb;
}

.case-main-panel.mode-case {
  background: #ffffff;
  height: min(720px, calc(100dvh - 170px));
}

.case-main-panel.mode-case :deep(.edit-page--embedded) { width: 100%; }

.case-main-panel.mode-suite .case-panel-header {
  background: #eaf5ee;
  border-bottom-color: #bee3cb;
}

.case-panel-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}

.header-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.case-detail-title-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.breadcrumb {
  font-size: 0.78rem;
  color: var(--kk-text-muted);
  font-weight: 500;
}
.current-suite {
  color: var(--kk-accent);
  font-weight: 600;
}

.case-panel-header h2 {
  margin: 0;
  font-size: 1.5rem;
  color: var(--kk-text);
}

.suite-desc {
  margin: 0;
  font-size: 0.85rem;
  color: var(--kk-text-muted);
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.btn-subtle {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 7px 12px;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--kk-accent);
  background: white;
  border: 1px solid #bee3cb;
  border-radius: 7px;
  cursor: pointer;
  transition: background 0.12s ease;
}
.btn-subtle:hover {
  background: #d4ebd9;
}

.card-title-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn-edit-card {
  padding: 3px 8px;
  font-size: 0.76rem;
}

/* CASE DETAIL VIEW */
.case-detail-body {
  display: flex;
  flex-direction: column;
  gap: 20px;
  padding: 20px;
}

.detail-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.detail-section h4 {
  margin: 0;
  font-size: 0.92rem;
  color: var(--kk-accent);
  font-weight: 700;
}

.detail-section p {
  margin: 0;
  font-size: 0.88rem;
  color: var(--kk-text);
  line-height: 1.6;
}

.steps-table {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--kk-border);
  border-radius: 8px;
  overflow: hidden;
  background: white;
}

.steps-header {
  display: grid;
  grid-template-columns: 50px 1fr 1fr;
  padding: 10px 14px;
  background: var(--kk-surface-subtle);
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--kk-text-muted);
  border-bottom: 1px solid var(--kk-border);
}

.case-panel-header .desc-input {
  flex: 0 0 100%;
}

.step-row {
  display: grid;
  grid-template-columns: 50px 1fr 1fr;
  padding: 12px 14px;
  font-size: 0.86rem;
  border-bottom: 1px solid var(--kk-border);
}
.step-row:last-child {
  border-bottom: none;
}
.col-no {
  font-weight: 700;
  color: var(--kk-accent);
}

/* CASE CARDS LIST */
.case-card-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 20px;
}

.case-card {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 14px 16px;
  background: white;
  border: 1px solid #cce5d4;
  border-radius: 9px;
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}
.case-card:hover {
  border-color: #9ed3b6;
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.05);
}
.case-card.inactive {
  opacity: 0.55;
}

.suite-card {
  background: #eaf5ee;
  border-color: #8fc8a5;
}

.child-suite-label {
  color: var(--kk-accent);
  font-size: 0.76rem;
  font-weight: 700;
}

.case-card-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  background: #eef7f2;
  color: #1b5e37;
  border-radius: 8px;
  flex-shrink: 0;
  margin-top: 2px;
}

.case-card-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
  flex: 1;
}

.case-card-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.case-title {
  font-size: 0.95rem;
  color: var(--kk-text);
}

.case-status-badge {
  padding: 2px 8px;
  font-size: 0.72rem;
  font-weight: 700;
  border-radius: 99px;
  background: #e5f5ec;
  color: #237047;
}
.case-status-badge.inactive {
  background: #f1f3f2;
  color: #718076;
}

.case-desc-preview {
  margin: 0;
  font-size: 0.82rem;
  color: var(--kk-text-muted);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.case-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 4px;
  font-size: 0.76rem;
  color: var(--kk-text-muted);
}

.empty-cases {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  min-height: 240px;
  padding: 48px 20px;
  gap: 8px;
  color: var(--kk-text-muted);
  text-align: center;
}
.empty-cases h4 {
  margin: 4px 0 0;
  font-size: 1.1rem;
  color: var(--kk-text);
}
.empty-cases p {
  margin: 0 0 8px;
  font-size: 0.85rem;
}
.empty-cta {
  margin-top: 6px;
}

.suite-create-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 16px 20px;
  border-top: 1px solid var(--kk-border);
  background: var(--kk-surface-subtle);
}

.form label, .settings label { display: grid; gap: 7px; font-size: .8rem; font-weight: 700; }
input, textarea, select { min-width: 0; padding: 10px 11px; font: inherit; background: white; border: 1px solid var(--kk-border); border-radius: 7px; }
.member-list { display: grid; gap: 7px; }
.member-list article { display: flex; align-items: center; gap: 12px; padding: 11px 0; border-bottom: 1px solid var(--kk-border); }
.member-list article > div { display: grid; flex: 1; gap: 3px; }.member-list small { color: var(--kk-text-muted); }
.remove { color: #8c2f2f; padding: 5px 7px; background: white; border: 1px solid var(--kk-border); border-radius: 5px; font-size: .7rem; }
.empty, .loading { display: grid; min-height: 180px; place-items: center; align-content: center; gap: 7px; color: var(--kk-text-muted); }
.error { padding: 11px 13px; color: #8c2f2f; background: #fceded; border-radius: 7px; }
.settings { max-width: 680px; }

/* UNIFIED BUTTON DESIGN SYSTEM */
.btn-subtle,
.btn-primary,
.btn-secondary,
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
  box-sizing: border-box;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s ease-in-out;
}

.btn-subtle {
  border: 1px solid var(--kk-border);
  background: #ffffff;
  color: var(--kk-text);
}
.btn-subtle:hover:not(:disabled) {
  background: #f3f4f6;
  border-color: #9ca3af;
}

.btn-primary {
  border: 1px solid #059669;
  background: #059669;
  color: #ffffff;
}
.btn-primary:hover:not(:disabled) {
  background: #047857;
  border-color: #047857;
}

.btn-danger-subtle {
  border: 1px solid #fecaca;
  background: #ffffff;
  color: #dc2626;
}
.btn-danger-subtle:hover:not(:disabled) {
  background: #fef2f2;
  border-color: #f87171;
}

.btn-subtle:disabled,
.btn-primary:disabled,
.btn-danger-subtle:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* CASE/SUITE PANEL HEADER LAYOUT */
.case-panel-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  padding: 16px 20px;
  background: #ffffff;
  border-bottom: 1px solid var(--kk-border);
}

.header-main-col {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
  min-width: 0;
}

.header-top-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-title-row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}

.case-title-field {
  display: grid;
  flex: 1;
  gap: 5px;
  min-width: 0;
  color: var(--kk-text-muted);
  font-size: .72rem;
  font-weight: 700;
}

.case-title-field .editable-title-input {
  width: 100%;
}

.header-static-title {
  font-size: 1.35rem;
  font-weight: 700;
  color: var(--kk-text);
  margin: 0;
}

/* SEAMLESS BREADCRUMB SELECT */
.select-breadcrumb {
  background: transparent;
  border: none;
  font-size: 0.85rem;
  font-weight: 500;
  color: var(--kk-text-muted);
  padding: 2px 6px;
  border-radius: 4px;
  cursor: pointer;
  outline: none;
  transition: all 0.15s ease;
}
.select-breadcrumb:hover,
.select-breadcrumb:focus {
  background: #f3f4f6;
  color: var(--kk-text);
}

/* STATUS SELECT PILL */
.status-select-pill {
  height: 26px;
  padding: 0 8px;
  font-size: 0.78rem;
  font-weight: 600;
  border-radius: 13px;
  border: 1px solid transparent;
  cursor: pointer;
  outline: none;
}
.status-select-pill.active {
  background: #e6f4ea;
  color: #137333;
  border-color: #ceead6;
}
.status-select-pill.inactive {
  background: #f1f3f4;
  color: #5f6368;
  border-color: #dadce0;
}

/* EDITABLE TITLE INPUT (LOOKS LIKE A NATIVE H2) */
.editable-title-input {
  font-size: 1.35rem;
  font-weight: 700;
  color: var(--kk-text);
  border: 1px solid transparent;
  background: transparent;
  border-radius: 6px;
  padding: 4px 8px;
  margin-left: -8px;
  flex: 1;
  box-sizing: border-box;
  transition: all 0.15s ease-in-out;
}
.editable-title-input:hover {
  background: #f9fafb;
  border-color: var(--kk-border);
}
.editable-title-input:focus {
  background: #ffffff;
  border-color: #059669;
  outline: none;
  box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.15);
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
  padding-top: 2px;
}

.editable-textarea {
  width: 100%;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  padding: 8px 10px;
  font-size: 0.88rem;
  box-sizing: border-box;
  background: #ffffff;
  transition: all 0.15s ease-in-out;
}

.case-suite-select {
  min-height: 38px;
  padding: 8px 10px;
  color: var(--kk-text);
  background: #fff;
  border: 1px solid var(--kk-border);
  border-radius: 6px;
  font: inherit;
}
.editable-textarea:focus {
  outline: none;
  border-color: #059669;
  box-shadow: 0 0 0 2px rgba(5, 150, 105, 0.15);
}

.section-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.step-editor-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.step-editor-card {
  border: 1px solid var(--kk-border);
  background: #f9fafb;
  border-radius: 8px;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.step-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--kk-text);
}
.step-card-header span {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.step-fields-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.project-link-controls,
.linked-project-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.project-link-controls :deep(.ui-select) {
  width: min(340px, 38vw);
}

.linked-project-list {
  display: grid;
  gap: 10px;
}

.linked-project-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 16px;
  background: var(--kk-surface-subtle);
  border: 1px solid var(--kk-border);
  border-radius: 8px;
}

.linked-project-identity {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 12px;
}

.linked-project-icon {
  display: grid;
  width: 38px;
  height: 38px;
  flex: 0 0 auto;
  place-items: center;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 8px;
}

.linked-project-identity > div {
  display: grid;
  min-width: 0;
  gap: 2px;
}

.linked-project-identity code {
  color: var(--kk-accent);
  font-size: .76rem;
  font-weight: 700;
}

.linked-project-identity strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.linked-project-identity small {
  color: var(--kk-text-muted);
}

@media (max-width: 900px) {
  .suite-split-layout { grid-template-columns: 1fr; }
  .two-column { grid-template-columns: 1fr; }
  .project-link-controls,
  .linked-project-card,
  .linked-project-actions { align-items: stretch; flex-direction: column; }
  .project-link-controls :deep(.ui-select) { width: 100%; }
}
</style>
