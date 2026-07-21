// Keep these wire types aligned with contract/openapi/khaikang.v1.yaml.
// They are maintained source code so project-specific behavior can evolve without
// a generator overwriting it.

export interface SetupStatusResponse {
  requiresInitialization: boolean
}

export interface InitializeAdminResponse {
  username: string
  initialPassword: string
}

export interface CsrfTokenResponse {
  token: string
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface AuthenticatedUserResponse {
  id: string
  username: string
  systemRoles: string[]
  mustChangePassword: boolean
}

export interface ApiProblem {
  type?: string | null
  title?: string | null
  status?: number | null
  detail?: string | null
  instance?: string | null
  code?: string | null
  errors?: Record<string, string[]>
}
