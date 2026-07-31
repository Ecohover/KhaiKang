<script setup lang="ts">
import { ClipboardCheck, FolderKanban, ShieldCheck } from '@lucide/vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
</script>

<template>
  <div class="dashboard-page">
    <section class="dashboard-hero">
      <div>
        <p>Workspace</p>
        <h2>歡迎回來，{{ auth.user?.username }}</h2>
        <span>主框架已就緒，可以從下一個垂直切片開始加入實際功能。</span>
      </div>
      <div class="dashboard-status">
        <ShieldCheck :size="19" aria-hidden="true" />
        <div>
          <strong>已安全登入</strong>
          <span>{{ auth.user?.systemRoles.join(' · ') }}</span>
        </div>
      </div>
    </section>

    <section class="dashboard-grid" aria-label="功能狀態">
      <article>
        <FolderKanban :size="21" aria-hidden="true" />
        <h3>專案</h3>
        <p>尚未建立專案，後續會從專案與成員權限開始。</p>
        <RouterLink :to="{ name: 'projects' }">查看預留頁</RouterLink>
      </article>
      <article>
        <ClipboardCheck :size="21" aria-hidden="true" />
        <h3>測試案例</h3>
        <p>測試工作區與案例管理會在專案基礎完成後加入。</p>
        <RouterLink :to="{ name: 'test-cases' }">查看預留頁</RouterLink>
      </article>
    </section>
  </div>
</template>

<style scoped>
.dashboard-page {
  display: grid;
  gap: 24px;
}

.dashboard-hero {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 28px;
  padding: clamp(24px, 5vw, 42px);
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.dashboard-hero p {
  margin: 0 0 7px;
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-hero h2 {
  margin: 0 0 9px;
  font-size: clamp(1.45rem, 4vw, 2rem);
}

.dashboard-hero > div > span,
.dashboard-status span,
.dashboard-grid p {
  color: var(--kk-text-muted);
}

.dashboard-status {
  display: flex;
  min-width: 210px;
  align-items: center;
  gap: 11px;
  padding: 14px 16px;
  color: var(--kk-accent);
  background: var(--kk-accent-soft);
  border-radius: var(--kk-radius);
}

.dashboard-status > div {
  display: grid;
  gap: 3px;
}

.dashboard-status span {
  font-size: 0.78rem;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
}

.dashboard-grid article {
  padding: 24px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.dashboard-grid article > svg {
  color: var(--kk-accent);
}

.dashboard-grid h3 {
  margin: 16px 0 7px;
}

.dashboard-grid p {
  min-height: 48px;
  margin: 0 0 18px;
  line-height: 1.55;
}

.dashboard-grid a {
  color: var(--kk-accent);
  font-size: 0.875rem;
  font-weight: 650;
}

@media (max-width: 680px) {
  .dashboard-hero {
    align-items: stretch;
    flex-direction: column;
  }

  .dashboard-status {
    min-width: 0;
  }

  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .dashboard-grid p {
    min-height: 0;
  }
}
</style>
