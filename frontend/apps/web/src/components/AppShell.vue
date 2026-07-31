<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { Component } from 'vue'
import type { RouteLocationRaw } from 'vue-router'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import {
  ArrowLeft,
  ClipboardCheck,
  FolderKanban,
  LayoutDashboard,
  ListTodo,
  LogOut,
  Menu,
  Settings2,
  UserRoundCog,
  Users,
  X,
} from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import {
  type ApplicationModuleId,
  visibleApplicationModules,
} from '../navigation'
import { useAuthStore } from '../stores/auth'

interface ContextNavigationItem {
  label: string
  to: RouteLocationRaw
  icon: Component
}

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const mobileNavigationOpen = ref(false)
const loggingOut = ref(false)

const moduleIcons = {
  system: Settings2,
  projects: FolderKanban,
  tests: ClipboardCheck,
}

const allowedModules = computed(() =>
  visibleApplicationModules(auth.user?.systemPermissions ?? []),
)
const activeModule = computed<ApplicationModuleId>(() => route.meta.module ?? 'projects')
const activeModuleLabel = computed(
  () => allowedModules.value.find((item) => item.id === activeModule.value)?.label ?? '專案管理',
)
const projectId = computed(() => String(route.params.projectId ?? ''))
const pageTitle = computed(() => String(route.meta.title ?? 'KhaiKang'))
const userInitial = computed(() => auth.user?.username.slice(0, 1).toUpperCase() ?? 'U')

const contextNavigation = computed<ContextNavigationItem[]>(() => {
  if (activeModule.value === 'system') {
    return [
      { label: '系統總覽', to: { name: 'home' }, icon: LayoutDashboard },
      { label: '使用者管理', to: { name: 'admin-users' }, icon: Users },
    ]
  }

  if (activeModule.value === 'tests') {
    return [{ label: '測試案例', to: { name: 'test-cases' }, icon: ClipboardCheck }]
  }

  if (projectId.value) {
    return [
      { label: '返回專案列表', to: { name: 'projects' }, icon: ArrowLeft },
      {
        label: '首頁',
        to: { name: 'project-detail', params: { projectId: projectId.value } },
        icon: LayoutDashboard,
      },
      {
        label: '成員管理',
        to: { name: 'project-members', params: { projectId: projectId.value } },
        icon: UserRoundCog,
      },
      {
        label: '任務管理',
        to: { name: 'project-issues', params: { projectId: projectId.value } },
        icon: ListTodo,
      },
      {
        label: '專案設定',
        to: { name: 'project-settings', params: { projectId: projectId.value } },
        icon: Settings2,
      },
    ]
  }

  return [{ label: '專案列表', to: { name: 'projects' }, icon: FolderKanban }]
})

watch(
  () => route.fullPath,
  () => {
    mobileNavigationOpen.value = false
  },
)

async function logout(): Promise<void> {
  loggingOut.value = true
  try {
    await auth.logout()
    await router.push({ name: 'login' })
  } finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <div class="application-shell">
    <button
      v-if="mobileNavigationOpen"
      class="navigation-backdrop"
      type="button"
      aria-label="關閉導覽選單"
      @click="mobileNavigationOpen = false"
    />

    <aside class="application-sidebar" :class="{ 'is-open': mobileNavigationOpen }">
      <div class="application-sidebar__brand">
        <span aria-hidden="true">K</span>
        <div>
          <strong>KhaiKang</strong>
          <small>{{ activeModuleLabel }}</small>
        </div>
        <button
          class="application-sidebar__close"
          type="button"
          aria-label="關閉導覽選單"
          @click="mobileNavigationOpen = false"
        >
          <X :size="20" aria-hidden="true" />
        </button>
      </div>

      <nav class="application-navigation" :aria-label="`${activeModuleLabel}功能導覽`">
        <p>{{ projectId ? '專案功能' : activeModuleLabel }}</p>
        <RouterLink
          v-for="item in contextNavigation"
          :key="item.label"
          :to="item.to"
          class="application-navigation__link"
        >
          <component :is="item.icon" :size="18" aria-hidden="true" />
          {{ item.label }}
        </RouterLink>
      </nav>
    </aside>

    <div class="application-main">
      <header class="application-header">
        <button
          class="application-header__menu"
          type="button"
          aria-label="開啟導覽選單"
          :aria-expanded="mobileNavigationOpen"
          @click="mobileNavigationOpen = true"
        >
          <Menu :size="21" aria-hidden="true" />
        </button>
        <h1>{{ pageTitle }}</h1>

        <nav class="module-switcher" aria-label="功能模組切換">
          <RouterLink
            v-for="moduleItem in allowedModules"
            :key="moduleItem.id"
            :to="{ name: moduleItem.routeName }"
            :class="{ 'is-active': moduleItem.id === activeModule }"
            :aria-current="moduleItem.id === activeModule ? 'page' : undefined"
          >
            <component :is="moduleIcons[moduleItem.icon]" :size="16" aria-hidden="true" />
            <span>{{ moduleItem.label }}</span>
          </RouterLink>
        </nav>

        <div class="application-header__user">
          <span class="user-avatar" aria-hidden="true">{{ userInitial }}</span>
          <div>
            <strong>{{ auth.user?.username }}</strong>
            <small>{{ auth.user?.systemRoles.join(' · ') || '已登入' }}</small>
          </div>
          <UiButton
            variant="ghost"
            :loading="loggingOut"
            aria-label="登出"
            title="登出"
            @click="logout"
          >
            <LogOut :size="17" aria-hidden="true" />
          </UiButton>
        </div>
      </header>

      <main class="application-content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.application-shell {
  display: grid;
  min-height: 100vh;
  grid-template-columns: 232px minmax(0, 1fr);
  background: var(--kk-surface-subtle);
}

.application-sidebar {
  position: sticky;
  top: 0;
  display: flex;
  height: 100vh;
  min-width: 0;
  flex-direction: column;
  color: #e9f2ec;
  background: #25332b;
}

.application-sidebar__brand {
  display: flex;
  min-height: 68px;
  align-items: center;
  gap: 11px;
  padding: 0 18px;
  border-bottom: 1px solid rgb(255 255 255 / 10%);
}

.application-sidebar__brand > span,
.user-avatar {
  display: grid;
  place-items: center;
  font-weight: 800;
}

.application-sidebar__brand > span {
  width: 32px;
  height: 32px;
  color: #25332b;
  background: #9ed3b6;
  border-radius: 6px;
}

.application-sidebar__brand > div {
  display: grid;
  gap: 1px;
}

.application-sidebar__brand strong {
  font-size: 1rem;
}

.application-sidebar__brand small {
  color: #9fb0a4;
  font-size: 0.7rem;
}

.application-sidebar__close {
  display: none;
  margin-left: auto;
}

.application-navigation {
  display: grid;
  gap: 5px;
  padding: 24px 12px;
}

.application-navigation > p {
  margin: 0 10px 7px;
  color: #9fb0a4;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.application-navigation__link {
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 10px 12px;
  color: #d8e2db;
  border-radius: 6px;
  text-decoration: none;
}

.application-navigation__link:hover {
  color: white;
  background: rgb(255 255 255 / 7%);
}

.application-navigation__link.router-link-exact-active {
  color: white;
  background: rgb(158 211 182 / 17%);
}

.application-main {
  min-width: 0;
}

.application-header {
  position: sticky;
  top: 0;
  z-index: 10;
  display: flex;
  min-height: 68px;
  align-items: center;
  gap: 14px;
  padding: 0 clamp(20px, 3vw, 40px);
  background: rgb(255 255 255 / 94%);
  border-bottom: 1px solid var(--kk-border);
  backdrop-filter: blur(12px);
}

.application-header h1 {
  margin: 0;
  font-size: 1.05rem;
  white-space: nowrap;
}

.application-header__menu {
  display: none;
}

.module-switcher {
  display: flex;
  gap: 3px;
  margin-left: auto;
  padding: 3px;
  background: var(--kk-surface-subtle);
  border: 1px solid var(--kk-border);
  border-radius: 8px;
}

.module-switcher a {
  display: flex;
  min-height: 34px;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  color: var(--kk-text-muted);
  border-radius: 6px;
  font-size: 0.78rem;
  font-weight: 650;
  text-decoration: none;
}

.module-switcher a:hover,
.module-switcher a.is-active {
  color: var(--kk-text);
  background: var(--kk-surface);
  box-shadow: 0 1px 3px rgb(27 46 35 / 9%);
}

.application-header__user {
  display: flex;
  align-items: center;
  gap: 9px;
  padding-left: 14px;
  border-left: 1px solid var(--kk-border);
}

.application-header__user > div {
  display: grid;
  min-width: 82px;
}

.application-header__user strong {
  font-size: 0.82rem;
}

.application-header__user small {
  overflow: hidden;
  max-width: 130px;
  color: var(--kk-text-muted);
  font-size: 0.68rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.application-header__user :deep(.ui-button) {
  min-height: 36px;
  padding: 7px 9px;
}

.user-avatar {
  width: 32px;
  height: 32px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 50%;
  font-size: 0.78rem;
}

.application-content {
  width: min(1180px, calc(100% - 48px));
  margin: 0 auto;
  padding: 38px 0 64px;
}

.navigation-backdrop {
  display: none;
}

@media (max-width: 980px) {
  .module-switcher a span,
  .application-header__user > div {
    display: none;
  }

  .module-switcher a {
    padding-inline: 9px;
  }
}

@media (max-width: 820px) {
  .application-shell {
    grid-template-columns: 1fr;
  }

  .application-sidebar {
    position: fixed;
    z-index: 30;
    width: min(290px, calc(100vw - 54px));
    transform: translateX(-100%);
    transition: transform 180ms ease;
  }

  .application-sidebar.is-open {
    transform: translateX(0);
  }

  .application-sidebar__close,
  .application-header__menu {
    display: grid;
    padding: 7px;
    place-items: center;
    color: inherit;
    background: transparent;
    border: 0;
    border-radius: 5px;
  }

  .application-header__menu {
    color: var(--kk-text);
  }

  .navigation-backdrop {
    position: fixed;
    z-index: 20;
    display: block;
    width: 100%;
    height: 100%;
    padding: 0;
    background: rgb(17 24 20 / 48%);
    border: 0;
  }

  .application-content {
    width: min(100% - 32px, 720px);
    padding-top: 28px;
  }
}

@media (max-width: 520px) {
  .application-header {
    gap: 8px;
    padding-inline: 12px;
  }

  .application-header h1 {
    overflow: hidden;
    max-width: 120px;
    text-overflow: ellipsis;
  }

  .application-header__user {
    padding-left: 7px;
  }

  .user-avatar {
    display: none;
  }
}
</style>
