<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Component } from 'vue'
import type { RouteLocationRaw } from 'vue-router'
import { RouterView, useRoute, useRouter } from 'vue-router'
import { ArrowLeft, ClipboardCheck, FolderKanban, FolderTree, LayoutDashboard, ListTodo, Settings2, UserRoundCog, Users } from '@lucide/vue'
import { useI18n } from 'vue-i18n'
import ModuleShell from './ModuleShell.vue'
import { type ApplicationModuleId, visibleApplicationModules } from '../navigation'
import { useAuthStore } from '../stores/auth'

interface NavigationItem { label: string; to: RouteLocationRaw; icon: Component }

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const loggingOut = ref(false)
const moduleIcons = { system: Settings2, projects: FolderKanban, tests: ClipboardCheck }
const activeModule = computed<ApplicationModuleId>(() => route.meta.module ?? 'projects')
const projectId = computed(() => String(route.params.projectId ?? ''))
const workspaceId = computed(() => String(route.params.workspaceId ?? ''))
const allowedModules = computed(() => visibleApplicationModules(auth.user?.systemPermissions ?? []).map((item) => ({
  id: item.id, label: t(`shell.modules.${item.id}`), to: { name: item.routeName }, icon: moduleIcons[item.icon],
})))
const activeModuleLabel = computed(() => t(`shell.modules.${activeModule.value}`))
const pageTitle = computed(() => route.meta.titleKey ? t(route.meta.titleKey) : 'KhaiKang')

const navigation = computed<NavigationItem[]>(() => {
  if (activeModule.value === 'system') return [
    { label: t('shell.navigation.systemOverview'), to: { name: 'home' }, icon: LayoutDashboard },
    { label: t('shell.navigation.users'), to: { name: 'admin-users' }, icon: Users },
  ]
  if (activeModule.value === 'tests') {
    if (!workspaceId.value) return [{ label: t('shell.navigation.workspaceList'), to: { name: 'test-workspaces' }, icon: ClipboardCheck }]
    return [
      { label: t('shell.navigation.backToWorkspaces'), to: { name: 'test-workspaces' }, icon: ArrowLeft },
      { label: t('shell.navigation.home'), to: { name: 'test-home', params: { workspaceId: workspaceId.value } }, icon: LayoutDashboard },
      { label: t('routes.testSuites'), to: { name: 'test-suites', params: { workspaceId: workspaceId.value } }, icon: FolderTree },
      { label: t('shell.navigation.members'), to: { name: 'test-members', params: { workspaceId: workspaceId.value } }, icon: UserRoundCog },
      { label: t('shell.navigation.workspaceSettings'), to: { name: 'test-settings', params: { workspaceId: workspaceId.value } }, icon: Settings2 },
    ]
  }
  if (!projectId.value) return [{ label: t('shell.navigation.projectList'), to: { name: 'projects' }, icon: FolderKanban }]
  return [
    { label: t('shell.navigation.backToProjects'), to: { name: 'projects' }, icon: ArrowLeft },
    { label: t('shell.navigation.home'), to: { name: 'project-detail', params: { projectId: projectId.value } }, icon: LayoutDashboard },
    { label: t('shell.navigation.members'), to: { name: 'project-members', params: { projectId: projectId.value } }, icon: UserRoundCog },
    { label: t('shell.navigation.issues'), to: { name: 'project-issues', params: { projectId: projectId.value } }, icon: ListTodo },
    { label: t('shell.navigation.projectSettings'), to: { name: 'project-settings', params: { projectId: projectId.value } }, icon: Settings2 },
  ]
})
const navigationSection = computed(() => projectId.value ? t('shell.sections.project') : workspaceId.value ? t('shell.sections.workspace') : activeModuleLabel.value)

async function logout(): Promise<void> {
  loggingOut.value = true
  try { await auth.logout(); await router.push({ name: 'login' }) } finally { loggingOut.value = false }
}
</script>

<template>
  <ModuleShell
    :active-module-id="activeModule" :active-module-label="activeModuleLabel"
    :page-title="pageTitle"
    :navigation-label="`${activeModuleLabel}${t('shell.navigationSuffix')}`"
    :navigation-section="navigationSection" :modules="allowedModules" :navigation="navigation"
    :username="auth.user?.username ?? ''" :user-roles="auth.user?.systemRoles.join(' · ') || t('shell.signedIn')"
    :logging-out="loggingOut"
    :labels="{ openNavigation: t('shell.actions.openNavigation'), closeNavigation: t('shell.actions.closeNavigation'), logout: t('shell.actions.logout'), moduleSwitcher: t('shell.moduleSwitcher') }"
    @logout="logout"
  >
    <RouterView />
  </ModuleShell>
</template>
