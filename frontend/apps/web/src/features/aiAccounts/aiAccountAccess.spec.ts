import { describe, expect, it } from 'vitest'
import type { AiAccountResponse, AuthenticatedUserResponse } from '../../api/contracts'
import { aiAccountActions } from './aiAccountAccess'

const account = { ownerAccountId: 'owner-id' } as AiAccountResponse
const user = (id: string, roles: string[] = []): AuthenticatedUserResponse => ({
  id,
  username: id,
  accountType: 'human',
  fixedRole: null,
  ownerAccountId: null,
  systemRoles: roles,
  systemPermissions: [],
  mustChangePassword: false,
})

describe('AI account management access', () => {
  it('keeps plaintext commands owner-only while administrators retain non-plaintext controls', () => {
    expect(aiAccountActions(account, user('owner-id'))).toEqual({
      canSetApiEnabled: true,
      canHandleTokenPlaintext: true,
      canRevokeToken: true,
      canTransferOwner: false,
    })
    expect(aiAccountActions(account, user('admin-id', ['System Admin']))).toEqual({
      canSetApiEnabled: true,
      canHandleTokenPlaintext: false,
      canRevokeToken: true,
      canTransferOwner: true,
    })
  })
})
