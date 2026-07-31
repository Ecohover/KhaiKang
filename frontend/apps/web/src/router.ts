import { createRouter, createWebHistory } from 'vue-router'
import AppShell from './components/AppShell.vue'
import {
  ACCOUNT_READ_PERMISSION,
  type ApplicationModuleId,
  hasRequiredSystemPermission,
} from './navigation'
import { useAuthStore } from './stores/auth'
import ChangePasswordView from './views/ChangePasswordView.vue'
import FeaturePlaceholderView from './views/FeaturePlaceholderView.vue'
import ForbiddenView from './views/ForbiddenView.vue'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import ProjectDetailView from './views/ProjectDetailView.vue'
import ProjectIssuesView from './views/ProjectIssuesView.vue'
import ProjectIssueFormView from './views/ProjectIssueFormView.vue'
import ProjectListView from './views/ProjectListView.vue'
import ProjectMembersView from './views/ProjectMembersView.vue'
import ProjectSettingsView from './views/ProjectSettingsView.vue'
import SetupView from './views/SetupView.vue'
import UnavailableView from './views/UnavailableView.vue'
import UserManagementView from './views/UserManagementView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    module?: ApplicationModuleId
    requiredSystemPermissions?: readonly string[]
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: AppShell,
      children: [
        {
          path: '',
          redirect: { name: 'projects' },
        },
        {
          path: 'admin',
          name: 'home',
          component: HomeView,
          meta: {
            title: '系統總覽',
            module: 'system',
            requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
          },
        },
        {
          path: 'projects',
          name: 'projects',
          component: ProjectListView,
          meta: { title: '專案列表', module: 'projects' },
        },
        {
          path: 'projects/:projectId',
          name: 'project-detail',
          component: ProjectDetailView,
          meta: { title: '專案首頁', module: 'projects' },
        },
        {
          path: 'projects/:projectId/members',
          name: 'project-members',
          component: ProjectMembersView,
          meta: { title: '成員管理', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues',
          name: 'project-issues',
          component: ProjectIssuesView,
          meta: { title: '任務管理', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues/new',
          name: 'project-issue-new',
          component: ProjectIssueFormView,
          meta: { title: '新增任務', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues/:issueId',
          name: 'project-issue-edit',
          component: ProjectIssueFormView,
          meta: { title: '編輯任務', module: 'projects' },
        },
        {
          path: 'projects/:projectId/settings',
          name: 'project-settings',
          component: ProjectSettingsView,
          meta: { title: '專案設定', module: 'projects' },
        },
        {
          path: 'test-cases',
          name: 'test-cases',
          component: FeaturePlaceholderView,
          props: {
            eyebrow: 'Test cases',
            title: '測試案例功能尚未建立',
            description: '目前先保留導覽位置，後續會接上測試工作區與案例管理。',
          },
          meta: { title: '測試案例', module: 'tests' },
        },
        {
          path: 'admin/users',
          name: 'admin-users',
          component: UserManagementView,
          meta: {
            title: '使用者管理',
            module: 'system',
            requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
          },
        },
        {
          path: 'forbidden',
          name: 'forbidden',
          component: ForbiddenView,
          meta: { title: '沒有權限' },
        },
      ],
    },
    { path: '/setup', name: 'setup', component: SetupView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/change-password', name: 'change-password', component: ChangePasswordView },
    { path: '/unavailable', name: 'unavailable', component: UnavailableView },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.ready) {
    try {
      await auth.bootstrap()
    } catch (reason) {
      auth.setStartupError(reason instanceof Error ? reason.message : '無法連線到 KhaiKang API。')
      return { name: 'unavailable' }
    }
  }

  if (auth.startupError) {
    return to.name === 'unavailable' ? true : { name: 'unavailable' }
  }

  if (auth.requiresInitialization && to.name !== 'setup') {
    return { name: 'setup' }
  }

  if (!auth.isAuthenticated && !auth.requiresInitialization && to.name !== 'login') {
    return { name: 'login' }
  }

  if (auth.user?.mustChangePassword && to.name !== 'change-password') {
    return { name: 'change-password' }
  }

  if (auth.isAuthenticated && !auth.user?.mustChangePassword && ['login', 'setup'].includes(String(to.name))) {
    return hasRequiredSystemPermission(
      auth.user?.systemPermissions ?? [],
      [ACCOUNT_READ_PERMISSION],
    )
      ? { name: 'home' }
      : { name: 'projects' }
  }

  if (
    to.meta.requiredSystemPermissions?.length &&
    !hasRequiredSystemPermission(
      auth.user?.systemPermissions ?? [],
      to.meta.requiredSystemPermissions,
    )
  ) {
    return { name: 'forbidden' }
  }

  return true
})
