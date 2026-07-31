export const ACCOUNT_READ_PERMISSION = 'account.read'
export const ACCOUNT_CREATE_PERMISSION = 'account.create'
export const ACCOUNT_UPDATE_PERMISSION = 'account.update'
export const ACCOUNT_SUSPEND_PERMISSION = 'account.suspend'
export const PROJECT_CREATE_PERMISSION = 'project.create'
export const PROJECT_DEACTIVATE_PERMISSION = 'project.deactivate'
export const PROJECT_UPDATE_PERMISSION = 'project.update'
export const PROJECT_MEMBER_ADD_PERMISSION = 'project.member.add'
export const PROJECT_MEMBER_REMOVE_PERMISSION = 'project.member.remove'
export const PROJECT_ROLE_ASSIGN_PERMISSION = 'project.role.assign'

export type ApplicationModuleId = 'system' | 'projects' | 'tests'

export interface ApplicationModule {
  id: ApplicationModuleId
  label: string
  routeName: string
  icon: 'system' | 'projects' | 'tests'
  requiredSystemPermissions?: readonly string[]
}

export const applicationModules: readonly ApplicationModule[] = [
  {
    id: 'system',
    label: '系統管理',
    routeName: 'home',
    icon: 'system',
    requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
  },
  {
    id: 'projects',
    label: '專案管理',
    routeName: 'projects',
    icon: 'projects',
  },
  {
    id: 'tests',
    label: '測試管理',
    routeName: 'test-cases',
    icon: 'tests',
  },
]

export function hasRequiredSystemPermission(
  systemPermissions: readonly string[],
  requiredSystemPermissions?: readonly string[],
): boolean {
  return !requiredSystemPermissions?.length || requiredSystemPermissions.every((permission) =>
    systemPermissions.includes(permission),
  )
}

export function visibleApplicationModules(
  systemPermissions: readonly string[],
): readonly ApplicationModule[] {
  return applicationModules.filter((item) =>
    hasRequiredSystemPermission(systemPermissions, item.requiredSystemPermissions),
  )
}
