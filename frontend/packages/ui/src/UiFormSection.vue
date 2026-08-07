<template>
  <section class="ui-form-section">
    <header v-if="$slots.header" class="ui-form-section__header"><slot name="header" /></header>
    <div class="ui-form-section__content"><slot /></div>
  </section>
</template>

<style scoped>
.ui-form-section {
  display: grid;
  gap: 18px;
  padding: 22px;
  background: var(--kk-surface);
  border: 1px solid var(--kk-border);
  border-radius: var(--kk-radius);
}

.ui-form-section__header { display: flex; align-items: flex-start; gap: 10px; padding-bottom: 14px; border-bottom: 1px solid var(--kk-border); }
.ui-form-section__header :deep(svg) { flex: 0 0 auto; margin-top: 2px; }
.ui-form-section__header :deep(h3), .ui-form-section__header :deep(p) { margin: 0; }
.ui-form-section__header :deep(p) { margin-top: 4px; color: var(--kk-text-muted); font-size: .84rem; line-height: 1.5; }
.ui-form-section__content { display: grid; gap: 14px; }
</style>
