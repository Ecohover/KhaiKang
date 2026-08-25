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

export type ApplicationModuleId = 'system' | 'aiAccounts' | 'projects' | 'tests'

export interface ApplicationModule {
  id: ApplicationModuleId
  routeName: string
  icon: 'system' | 'aiAccounts' | 'projects' | 'tests'
  requiredSystemPermissions?: readonly string[]
  requiredAccountType?: 'human'
}

export const applicationModules: readonly ApplicationModule[] = [
  {
    id: 'aiAccounts',
    routeName: 'ai-accounts',
    icon: 'aiAccounts',
    requiredAccountType: 'human',
  },
  {
    id: 'system',
    routeName: 'home',
    icon: 'system',
    requiredSystemPermissions: [ACCOUNT_READ_PERMISSION],
  },
  {
    id: 'projects',
    routeName: 'projects',
    icon: 'projects',
  },
  {
    id: 'tests',
    routeName: 'test-workspaces',
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
  accountType: 'human' | 'ai_agent' = 'human',
): readonly ApplicationModule[] {
  return applicationModules.filter((item) =>
    (!item.requiredAccountType || item.requiredAccountType === accountType) &&
    hasRequiredSystemPermission(systemPermissions, item.requiredSystemPermissions),
  )
}
