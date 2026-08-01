<script setup lang="ts">
import { computed, ref } from 'vue'
import { ChevronDown, ChevronRight, FileCheck2, Folder, FolderOpen } from '@lucide/vue'
import type { TestCaseResponse, TestSuiteResponse } from '../api/contracts'

const props = defineProps<{
  suites: TestSuiteResponse[]
  cases: TestCaseResponse[]
  selectedCaseIds: string[]
  workspacePrefix: string
}>()

const emit = defineEmits<{
  toggleSuite: [suiteId: string]
  toggleCase: [caseId: string]
  moveCase: [caseId: string, offset: number]
}>()

const collapsedSuiteIds = ref<Set<string>>(new Set())
const activeSuites = computed(() => props.suites.filter((suite) => suite.status === 'active'))
const activeCases = computed(() => props.cases.filter((testCase) => testCase.status === 'active'))
const casesBySuite = computed(() => {
  const values = new Map<string, TestCaseResponse[]>()
  for (const testCase of activeCases.value) {
    values.set(testCase.suiteId, [...(values.get(testCase.suiteId) ?? []), testCase])
  }
  for (const cases of values.values()) {
    cases.sort((left, right) => left.sortOrder - right.sortOrder || left.title.localeCompare(right.title))
  }
  return values
})

const visibleNodes = computed(() => {
  const nodes: Array<{ type: 'suite'; suite: TestSuiteResponse } | { type: 'case'; testCase: TestCaseResponse; depth: number; index: number }> = []
  const append = (parentId: string | null): void => {
    const children = activeSuites.value
      .filter((suite) => suite.parentId === parentId)
      .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name))

    for (const suite of children) {
      nodes.push({ type: 'suite', suite })
      if (collapsedSuiteIds.value.has(suite.id)) continue
      append(suite.id)
      ;(casesBySuite.value.get(suite.id) ?? []).forEach((testCase, index) => {
        nodes.push({ type: 'case', testCase, depth: suite.depth + 1, index: index + 1 })
      })
    }
  }
  append(null)
  return nodes
})

function hasChildren(suiteId: string): boolean {
  return activeSuites.value.some((suite) => suite.parentId === suiteId)
    || (casesBySuite.value.get(suiteId)?.length ?? 0) > 0
}

function isCollapsed(suiteId: string): boolean {
  return collapsedSuiteIds.value.has(suiteId)
}

function toggleCollapsed(suiteId: string): void {
  const next = new Set(collapsedSuiteIds.value)
  next.has(suiteId) ? next.delete(suiteId) : next.add(suiteId)
  collapsedSuiteIds.value = next
}

function caseIdsForSuite(suiteId: string): string[] {
  const suiteIds = new Set([suiteId])
  let foundChild = true
  while (foundChild) {
    foundChild = false
    for (const suite of activeSuites.value) {
      if (suite.parentId && suiteIds.has(suite.parentId) && !suiteIds.has(suite.id)) {
        suiteIds.add(suite.id)
        foundChild = true
      }
    }
  }
  return activeCases.value.filter((testCase) => suiteIds.has(testCase.suiteId)).map((testCase) => testCase.id)
}

function isSuiteSelected(suiteId: string): boolean {
  const caseIds = caseIdsForSuite(suiteId)
  return caseIds.length > 0 && caseIds.every((caseId) => props.selectedCaseIds.includes(caseId))
}
</script>

<template>
  <div class="test-plan-case-tree">
    <template v-for="node in visibleNodes" :key="node.type === 'suite' ? node.suite.id : node.testCase.id">
      <div
        v-if="node.type === 'suite'"
        class="tree-item suite-tree-item"
        :class="{ selected: isSuiteSelected(node.suite.id) }"
        :style="{ paddingLeft: `${(node.suite.depth - 1) * 16 + 8}px` }"
      >
        <button
          v-if="hasChildren(node.suite.id)"
          type="button"
          class="chevron-button"
          :aria-expanded="!isCollapsed(node.suite.id)"
          :aria-label="node.suite.name"
          @click="toggleCollapsed(node.suite.id)"
        >
          <ChevronDown v-if="!isCollapsed(node.suite.id)" :size="14" />
          <ChevronRight v-else :size="14" />
        </button>
        <span v-else class="chevron-spacer" />
        <input
          type="checkbox"
          :checked="isSuiteSelected(node.suite.id)"
          :disabled="!caseIdsForSuite(node.suite.id).length"
          @change="emit('toggleSuite', node.suite.id)"
        />
        <FolderOpen v-if="!isCollapsed(node.suite.id)" :size="16" class="tree-icon" />
        <Folder v-else :size="16" class="tree-icon" />
        <span class="tree-label">{{ node.suite.name }}</span>
        <span class="badge">{{ casesBySuite.get(node.suite.id)?.length ?? 0 }}</span>
      </div>
      <div
        v-else
        class="tree-item case-tree-item"
        :class="{ selected: selectedCaseIds.includes(node.testCase.id) }"
        :style="{ paddingLeft: `${node.depth * 16 + 2}px` }"
      >
        <input
          type="checkbox"
          :checked="selectedCaseIds.includes(node.testCase.id)"
          @change="emit('toggleCase', node.testCase.id)"
        />
        <FileCheck2 :size="15" class="tree-icon case-icon" />
        <span class="case-code">{{ workspacePrefix }}-{{ node.index }}</span>
        <span class="tree-label">{{ node.testCase.title }}</span>
        <span v-if="selectedCaseIds.includes(node.testCase.id)" class="order-actions">
          <button type="button" @click="emit('moveCase', node.testCase.id, -1)">&uarr;</button>
          <button type="button" @click="emit('moveCase', node.testCase.id, 1)">&darr;</button>
        </span>
      </div>
    </template>
  </div>
</template>

<style scoped>
.test-plan-case-tree{display:flex;flex-direction:column;gap:2px}.tree-item{display:flex;align-items:center;gap:6px;min-height:34px;padding:6px 8px;border-radius:6px;color:var(--kk-text);font-size:.85rem}.suite-tree-item{background:var(--kk-accent-soft)}.suite-tree-item.selected{color:#1b5e37;font-weight:650}.case-tree-item{font-size:.82rem;color:#4a5550}.case-tree-item.selected{background:#f5faf7}.tree-item input{width:14px;height:14px;margin:0;padding:0}.chevron-button{display:grid;place-items:center;width:18px;height:18px;padding:0;border:0;border-radius:4px;background:transparent;color:var(--kk-text-muted);cursor:pointer}.chevron-button:hover{background:rgba(0,0,0,.08)}.chevron-spacer{width:18px}.tree-icon{flex-shrink:0;color:var(--kk-accent)}.case-icon{color:#4b8b65}.tree-label{flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.badge{padding:2px 7px;border-radius:10px;background:rgba(0,0,0,.05);color:var(--kk-text-muted);font-size:.72rem;font-weight:600}.suite-tree-item.selected .badge{background:#d4ebd9;color:#1b5e37}.case-code{flex-shrink:0;padding:1px 5px;border-radius:4px;background:rgba(0,0,0,.05);color:var(--kk-text-muted);font-family:monospace;font-size:.75rem}.order-actions{display:flex;gap:4px}.order-actions button{border:1px solid var(--kk-border);border-radius:4px;background:white;cursor:pointer}
</style>
