import type { RouteLocationRaw } from 'vue-router'
import { createRouter, createWebHistory } from 'vue-router'
import AppShell from './components/AppShell.vue'
import { type ApplicationModuleId, ACCOUNT_CREATE_PERMISSION, ACCOUNT_READ_PERMISSION, ACCOUNT_SUSPEND_PERMISSION, ACCOUNT_UPDATE_PERMISSION, PROJECT_CREATE_PERMISSION, hasRequiredSystemPermission } from './navigation'
import { useAuthStore } from './stores/auth'
import { i18n } from './i18n'
import ChangePasswordView from './views/ChangePasswordView.vue'
import ForbiddenView from './views/ForbiddenView.vue'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import ProjectCreateView from './views/ProjectCreateView.vue'
import ProjectDetailView from './views/ProjectDetailView.vue'
import ProjectIssueFormView from './views/ProjectIssueFormView.vue'
import ProjectIssuesView from './views/ProjectIssuesView.vue'
import ProjectListView from './views/ProjectListView.vue'
import ProjectMembersView from './views/ProjectMembersView.vue'
import ProjectSettingsView from './views/ProjectSettingsView.vue'
import SetupView from './views/SetupView.vue'
import TestCaseCreateView from './views/TestCaseCreateView.vue'
import TestSuiteCreateView from './views/TestSuiteCreateView.vue'
import TestWorkspaceCreateView from './views/TestWorkspaceCreateView.vue'
import TestWorkspaceListView from './views/TestWorkspaceListView.vue'
import TestWorkspaceView from './views/TestWorkspaceView.vue'
import UnavailableView from './views/UnavailableView.vue'
import UserManagementView from './views/UserManagementView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    titleKey?: string
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
            titleKey: 'routes.systemOverview',
            module: 'system',
            requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
          },
        },
        {
          path: 'projects',
          name: 'projects',
          component: ProjectListView,
          meta: { titleKey: 'routes.projectList', module: 'projects' },
        },
        {
          path: 'projects/new',
          name: 'project-new',
          component: ProjectCreateView,
          meta: {
            titleKey: 'routes.projectCreate',
            module: 'projects',
            requiredSystemPermissions: [PROJECT_CREATE_PERMISSION],
          },
        },
        {
          path: 'projects/:projectId',
          name: 'project-detail',
          component: ProjectDetailView,
          meta: { titleKey: 'routes.projectHome', module: 'projects' },
        },
        {
          path: 'projects/:projectId/members',
          name: 'project-members',
          component: ProjectMembersView,
          meta: { titleKey: 'routes.projectMembers', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues',
          name: 'project-issues',
          component: ProjectIssuesView,
          meta: { titleKey: 'routes.projectIssues', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues/new',
          name: 'project-issue-new',
          component: ProjectIssueFormView,
          meta: { titleKey: 'routes.issueCreate', module: 'projects' },
        },
        {
          path: 'projects/:projectId/issues/:issueId',
          name: 'project-issue-edit',
          component: ProjectIssueFormView,
          meta: { titleKey: 'routes.issueEdit', module: 'projects' },
        },
        {
          path: 'projects/:projectId/settings',
          name: 'project-settings',
          component: ProjectSettingsView,
          meta: { titleKey: 'routes.projectSettings', module: 'projects' },
        },
        {
          path: 'test-workspaces',
          name: 'test-workspaces',
          component: TestWorkspaceListView,
          meta: { titleKey: 'routes.testWorkspaceList', module: 'tests' },
        },
        {
          path: 'test-workspaces/new',
          name: 'test-workspace-new',
          component: TestWorkspaceCreateView,
          meta: { titleKey: 'routes.testWorkspaceCreate', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId',
          name: 'test-workspace',
          redirect: (to) => ({ name: 'test-home', params: to.params }),
        },
        {
          path: 'test-workspaces/:workspaceId/home',
          name: 'test-home',
          component: TestWorkspaceView,
          meta: { titleKey: 'routes.testHome', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/suites',
          name: 'test-suites',
          component: TestWorkspaceView,
          meta: { titleKey: 'routes.testSuites', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/suites/new',
          name: 'test-suite-new',
          component: TestSuiteCreateView,
          meta: { titleKey: 'routes.testSuiteCreate', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/cases/new',
          name: 'test-case-new',
          component: TestCaseCreateView,
          meta: { titleKey: 'routes.testCaseCreate', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/cases/:caseId/edit',
          name: 'test-case-edit',
          component: () => import('./views/TestCaseEditView.vue'),
          meta: { titleKey: 'routes.testCaseEdit', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/plans',
          name: 'test-plans',
          component: () => import('./views/TestPlanView.vue'),
          meta: { titleKey: 'routes.testPlans', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/runs',
          name: 'test-runs',
          component: () => import('./views/TestRunView.vue'),
          meta: { titleKey: 'routes.testRuns', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/runs/:runId',
          name: 'test-run-detail',
          component: () => import('./views/TestRunExecutionView.vue'),
          meta: { titleKey: 'routes.testRunExecution', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/members',
          name: 'test-members',
          component: TestWorkspaceView,
          meta: { titleKey: 'routes.testMembers', module: 'tests' },
        },
        {
          path: 'test-workspaces/:workspaceId/settings',
          name: 'test-settings',
          component: TestWorkspaceView,
          meta: { titleKey: 'routes.testSettings', module: 'tests' },
        },
        {
          path: 'admin/users',
          name: 'admin-users',
          component: UserManagementView,
          meta: {
            titleKey: 'routes.users',
            module: 'system',
            requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
          },
        },
        {
          path: 'forbidden',
          name: 'forbidden',
          component: ForbiddenView,
          meta: { titleKey: 'routes.forbidden' },
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
      auth.setStartupError(
        reason instanceof Error ? reason.message : i18n.global.t('common.errors.apiUnavailable'),
      )
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
    to.meta.requiredSystemPermissions &&
    !hasRequiredSystemPermission(
      auth.user?.systemPermissions ?? [],
      to.meta.requiredSystemPermissions,
    )
  ) {
    return { name: 'forbidden' }
  }

  return true
})
