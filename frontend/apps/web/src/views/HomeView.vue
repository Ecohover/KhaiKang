<script setup lang="ts">
import { ClipboardCheck, FolderKanban, ShieldCheck } from '@lucide/vue'
import { useAuthStore } from '../stores/auth'
import { useI18n } from 'vue-i18n'

const auth = useAuthStore()
const { t } = useI18n()
</script>

<template>
  <div class="dashboard-page">
    <section class="dashboard-hero">
      <div>
        <p>Workspace</p>
        <h2>{{ t('system.home.welcome', { username: auth.user?.username }) }}</h2>
        <span>{{ t('system.home.description') }}</span>
      </div>
      <div class="dashboard-status">
        <ShieldCheck :size="19" aria-hidden="true" />
        <div>
          <strong>{{ t('system.home.signedIn') }}</strong>
          <span>{{ auth.user?.systemRoles.join(' · ') }}</span>
        </div>
      </div>
    </section>

    <section class="dashboard-grid" :aria-label="t('system.home.featureStatus')">
      <article>
        <FolderKanban :size="21" aria-hidden="true" />
        <h3>{{ t('system.home.projects') }}</h3>
        <p>{{ t('system.home.projectsDescription') }}</p>
        <RouterLink :to="{ name: 'projects' }">{{ t('system.home.open') }}</RouterLink>
      </article>
      <article>
        <ClipboardCheck :size="21" aria-hidden="true" />
        <h3>{{ t('system.home.tests') }}</h3>
        <p>{{ t('system.home.testsDescription') }}</p>
        <RouterLink :to="{ name: 'test-workspaces' }">{{ t('system.home.open') }}</RouterLink>
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
