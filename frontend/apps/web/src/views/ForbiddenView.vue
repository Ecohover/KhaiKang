<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <section class="forbidden-state">
    <p>403</p>
    <h2>{{ t('system.pages.forbiddenTitle') }}</h2>
    <span>{{ t('system.pages.forbiddenDescription') }}</span>
    <RouterLink :to="{ name: 'home' }">{{ t('system.pages.backToOverview') }}</RouterLink>
  </section>
</template>

<style scoped>
.forbidden-state {
  padding: clamp(52px, 10vw, 108px) 24px;
  text-align: center;
}

.forbidden-state p {
  margin: 0 0 8px;
  color: var(--kk-danger);
  font-weight: 750;
}

.forbidden-state h2 {
  margin: 0 0 10px;
}

.forbidden-state span {
  display: block;
  margin-bottom: 22px;
  color: var(--kk-text-muted);
}

.forbidden-state a {
  color: var(--kk-accent);
  font-weight: 650;
}
</style>
