<script setup lang="ts">
import { ref, watch } from 'vue'
import type { Component } from 'vue'
import type { RouteLocationRaw } from 'vue-router'
import { RouterLink, useRoute } from 'vue-router'
import { LogOut, Menu, X } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import { useI18n } from 'vue-i18n'
import SaveNoticeHost from './SaveNoticeHost.vue'
import { appVersion } from '../version'

export interface ShellModuleItem { id: string; label: string; to: RouteLocationRaw; icon: Component }
export interface ShellNavigationItem { label: string; to: RouteLocationRaw; icon: Component }

const props = defineProps<{
  activeModuleId: string
  activeModuleLabel: string
  pageTitle: string
  navigationLabel: string
  navigationSection: string
  modules: ShellModuleItem[]
  navigation: ShellNavigationItem[]
  username?: string
  userRoles: string
  loggingOut: boolean
  labels: { openNavigation: string; closeNavigation: string; logout: string; moduleSwitcher: string }
}>()
const emit = defineEmits<{ logout: [] }>()
const route = useRoute()
const { locale, t } = useI18n()
const mobileNavigationOpen = ref(false)

watch(() => route.fullPath, () => { mobileNavigationOpen.value = false })
</script>

<template>
  <div class="application-shell">
    <button v-if="mobileNavigationOpen" class="navigation-backdrop" type="button" :aria-label="labels.closeNavigation" @click="mobileNavigationOpen = false" />
    <aside class="application-sidebar" :class="{ 'is-open': mobileNavigationOpen }">
      <div class="application-sidebar__brand">
        <span aria-hidden="true">K</span><div><strong>KhaiKang</strong><small>{{ activeModuleLabel }} · v{{ appVersion }}</small></div>
        <button class="application-sidebar__close" type="button" :aria-label="labels.closeNavigation" @click="mobileNavigationOpen = false"><X :size="20" /></button>
      </div>
      <nav class="application-navigation" :aria-label="navigationLabel">
        <p>{{ navigationSection }}</p>
        <RouterLink v-for="item in navigation" :key="item.label" :to="item.to" class="application-navigation__link">
          <component :is="item.icon" :size="18" aria-hidden="true" />{{ item.label }}
        </RouterLink>
      </nav>
    </aside>
    <div class="application-main">
      <header class="application-header">
        <button class="application-header__menu" type="button" :aria-label="labels.openNavigation" :aria-expanded="mobileNavigationOpen" @click="mobileNavigationOpen = true"><Menu :size="21" /></button>
        <h1>{{ pageTitle }}</h1>
        <nav class="module-switcher" :aria-label="labels.moduleSwitcher">
          <RouterLink v-for="item in modules" :key="item.id" :to="item.to" :class="{ 'is-active': item.id === activeModuleId }" :aria-current="item.id === activeModuleId ? 'page' : undefined">
            <component :is="item.icon" :size="16" aria-hidden="true" /><span>{{ item.label }}</span>
          </RouterLink>
        </nav>
        <div class="application-header__user">
          <select v-model="locale" class="locale-select" :aria-label="t('shell.language')">
            <option value="zh-TW">{{ t('shell.locales.zh-TW') }}</option>
            <option value="en">{{ t('shell.locales.en') }}</option>
          </select>
          <span class="user-avatar" aria-hidden="true">{{ username?.slice(0, 1).toUpperCase() || 'U' }}</span>
          <div><strong>{{ username }}</strong><small>{{ userRoles }}</small></div>
          <UiButton variant="ghost" :loading="loggingOut" :aria-label="labels.logout" :title="labels.logout" @click="emit('logout')"><LogOut :size="17" /></UiButton>
        </div>
      </header>
      <main class="application-content"><slot /></main>
      <SaveNoticeHost />
    </div>
  </div>
</template>

<style scoped>
.application-shell{display:grid;min-height:100vh;grid-template-columns:232px minmax(0,1fr);background:var(--kk-surface-subtle)}
.application-sidebar{position:sticky;top:0;display:flex;height:100vh;min-width:0;flex-direction:column;color:#e9f2ec;background:#25332b}
.application-sidebar__brand{display:flex;min-height:68px;align-items:center;gap:11px;padding:0 18px;border-bottom:1px solid rgb(255 255 255/10%)}
.application-sidebar__brand>span,.user-avatar{display:grid;place-items:center;font-weight:800}.application-sidebar__brand>span{width:32px;height:32px;color:#25332b;background:#9ed3b6;border-radius:6px}.application-sidebar__brand>div{display:grid;gap:1px}.application-sidebar__brand small{color:#9fb0a4;font-size:.7rem}.application-sidebar__close{display:none;margin-left:auto}
.application-navigation{display:grid;gap:5px;padding:24px 12px}.application-navigation>p{margin:0 10px 7px;color:#9fb0a4;font-size:.72rem;font-weight:700;letter-spacing:.08em;text-transform:uppercase}.application-navigation__link{display:flex;align-items:center;gap:11px;padding:10px 12px;color:#d8e2db;border-radius:6px;text-decoration:none}.application-navigation__link:hover{color:white;background:rgb(255 255 255/7%)}.application-navigation__link.router-link-exact-active{color:white;background:rgb(158 211 182/17%)}
.application-main{min-width:0}.application-header{position:sticky;top:0;z-index:10;display:flex;min-height:68px;align-items:center;gap:14px;padding:0 clamp(20px,3vw,40px);background:rgb(255 255 255/94%);border-bottom:1px solid var(--kk-border);backdrop-filter:blur(12px)}.application-header h1{margin:0;font-size:1.05rem;white-space:nowrap}.application-header__menu{display:none}
.module-switcher{display:flex;gap:3px;margin-left:auto;padding:3px;background:var(--kk-surface-subtle);border:1px solid var(--kk-border);border-radius:8px}.module-switcher a{display:flex;min-height:34px;align-items:center;gap:6px;padding:6px 10px;color:var(--kk-text-muted);border-radius:6px;font-size:.78rem;font-weight:650;text-decoration:none}.module-switcher a:hover,.module-switcher a.is-active{color:var(--kk-text);background:var(--kk-surface);box-shadow:0 1px 3px rgb(27 46 35/9%)}
.application-header__user{display:flex;align-items:center;gap:9px;padding-left:14px;border-left:1px solid var(--kk-border)}.application-header__user>div{display:grid;min-width:82px}.application-header__user strong{font-size:.82rem}.application-header__user small{overflow:hidden;max-width:130px;color:var(--kk-text-muted);font-size:.68rem;text-overflow:ellipsis;white-space:nowrap}.application-header__user :deep(.ui-button){min-height:36px;padding:7px 9px}.user-avatar{width:32px;height:32px;color:var(--kk-accent);background:var(--kk-accent-soft);border-radius:50%;font-size:.78rem}
.locale-select{min-height:34px;padding:5px 7px;color:var(--kk-text-muted);background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:6px;font:inherit;font-size:.75rem}
.application-content{width:calc(100% - 48px);max-width:1720px;margin:0 auto;padding:28px 0 48px}.navigation-backdrop{display:none}
@media(max-width:980px){.module-switcher a span,.application-header__user>div{display:none}.module-switcher a{padding-inline:9px}}
@media(max-width:820px){.application-shell{grid-template-columns:1fr}.application-sidebar{position:fixed;z-index:30;width:min(290px,calc(100vw - 54px));transform:translateX(-100%);transition:transform 180ms ease}.application-sidebar.is-open{transform:translateX(0)}.application-sidebar__close,.application-header__menu{display:grid;padding:7px;place-items:center;color:inherit;background:transparent;border:0;border-radius:5px}.application-header__menu{color:var(--kk-text)}.navigation-backdrop{position:fixed;z-index:20;display:block;width:100%;height:100%;padding:0;background:rgb(17 24 20/48%);border:0}.application-content{width:min(100% - 32px,720px);padding-top:28px}}
@media(max-width:520px){.application-header{gap:8px;padding-inline:12px}.application-header h1{overflow:hidden;max-width:120px;text-overflow:ellipsis}.application-header__user{padding-left:7px}.user-avatar{display:none}}
</style>
