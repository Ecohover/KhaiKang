<template>
  <section class="ui-table-container">
    <header v-if="$slots.header" class="ui-table-container__header">
      <slot name="header" />
    </header>
    <div class="ui-table-container__scroll">
      <slot />
    </div>
    <footer v-if="$slots.footer" class="ui-table-container__footer">
      <slot name="footer" />
    </footer>
  </section>
</template>

<style scoped>
.ui-table-container{display:grid;overflow:hidden;background:var(--kk-surface);border:1px solid var(--kk-border);border-radius:8px}
.ui-table-container__header{display:flex;align-items:center;justify-content:space-between;padding:14px 18px;border-bottom:1px solid var(--kk-border)}
.ui-table-container__scroll{overflow:auto}
.ui-table-container__footer{padding:0 16px}
.ui-table-container__header :deep(span){color:var(--kk-text-muted);font-size:.82rem}
</style>
