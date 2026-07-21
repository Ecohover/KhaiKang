<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { LogOut, ShieldCheck } from '@lucide/vue'
import { UiButton } from '@khaikang/ui'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const loggingOut = ref(false)

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
  <div class="app-shell">
    <header class="app-header">
      <div class="app-header__brand"><span>K</span>KhaiKang</div>
      <div class="app-header__account">
        <span>{{ auth.user?.username }}</span>
        <UiButton variant="ghost" :loading="loggingOut" @click="logout">
          <LogOut :size="17" aria-hidden="true" />
          登出
        </UiButton>
      </div>
    </header>

    <main class="workspace">
      <div class="workspace__heading">
        <div>
          <p>Workspace</p>
          <h1>開始使用 KhaiKang</h1>
        </div>
        <span class="status-chip"><ShieldCheck :size="16" />已安全登入</span>
      </div>

      <section class="empty-state">
        <h2>尚未建立測試工作區</h2>
        <p>專案與測試案例功能會在下一個垂直切片加入。</p>
      </section>
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  background: var(--kk-surface-subtle);
}

.app-header {
  display: flex;
  min-height: 62px;
  align-items: center;
  justify-content: space-between;
  padding: 0 clamp(18px, 4vw, 46px);
  background: var(--kk-surface);
  border-bottom: 1px solid var(--kk-border);
}

.app-header__brand,
.app-header__account {
  display: flex;
  align-items: center;
  gap: 12px;
}

.app-header__brand {
  font-weight: 750;
}

.app-header__brand > span {
  display: grid;
  width: 30px;
  height: 30px;
  place-items: center;
  color: white;
  background: #25332b;
  border-radius: 5px;
}

.app-header__account > span {
  color: var(--kk-text-muted);
  font-size: 0.875rem;
}

.workspace {
  width: min(1180px, calc(100% - 36px));
  margin: 0 auto;
  padding: 44px 0;
}

.workspace__heading {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: 20px;
  padding-bottom: 24px;
  border-bottom: 1px solid var(--kk-border);
}

.workspace__heading p {
  margin: 0 0 6px;
  color: var(--kk-text-muted);
  font-size: 0.8125rem;
  text-transform: uppercase;
}

.workspace__heading h1 {
  margin: 0;
  font-size: 1.75rem;
  letter-spacing: 0;
}

.status-chip {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 7px 9px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: 5px;
  font-size: 0.8125rem;
  font-weight: 650;
}

.empty-state {
  padding: 72px 0;
  text-align: center;
}

.empty-state h2 {
  margin: 0 0 8px;
  font-size: 1.2rem;
}

.empty-state p {
  margin: 0;
  color: var(--kk-text-muted);
}

@media (max-width: 620px) {
  .app-header__account > span {
    display: none;
  }

  .workspace__heading {
    align-items: start;
    flex-direction: column;
  }
}
</style>
