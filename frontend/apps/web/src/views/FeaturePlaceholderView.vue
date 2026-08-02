<script setup lang="ts">
defineProps<{
  eyebrow: string
  title: string
  description: string
}>()
</script>

<template>
  <section class="feature-placeholder">
    <p>{{ eyebrow }}</p>
    <h2>{{ title }}</h2>
    <span>{{ description }}</span>
  </section>
</template>

<style scoped>
.feature-placeholder {
  padding: clamp(52px, 10vw, 108px) 24px;
  text-align: center;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.feature-placeholder p {
  margin: 0 0 8px;
  color: var(--kk-accent);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.feature-placeholder h2 {
  margin: 0 0 10px;
  font-size: 1.35rem;
}

.feature-placeholder span {
  color: var(--kk-text-muted);
  line-height: 1.6;
}
</style>
