import type { AiAccountResponse, AuthenticatedUserResponse } from '../../api/contracts'

export interface AiAccountActions {
  canSetApiEnabled: boolean
  canHandleTokenPlaintext: boolean
  canRevokeToken: boolean
  canTransferOwner: boolean
}

export function aiAccountActions(
  account: AiAccountResponse,
  user: AuthenticatedUserResponse,
): AiAccountActions {
  const isOwner = account.ownerAccountId === user.id
  const isAdministrator = user.systemRoles.includes('System Admin')
  return {
    canSetApiEnabled: isOwner || isAdministrator,
    canHandleTokenPlaintext: isOwner,
    canRevokeToken: isOwner || isAdministrator,
    canTransferOwner: isAdministrator,
  }
}
