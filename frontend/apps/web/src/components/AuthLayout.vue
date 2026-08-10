<template>
  <div class="auth-page">
    <aside class="auth-brand">
      <div>
        <div class="auth-brand__mark" aria-hidden="true">K</div>
        <p class="auth-brand__name">KhaiKang</p>
      </div>
      <span class="auth-brand__foot">Self-hosted test workspace · v{{ appVersion }}</span>
    </aside>
    <main class="auth-main">
      <section class="auth-panel">
        <slot />
      </section>
    </main>
  </div>
</template>

<script setup lang="ts">
import { appVersion } from '../version'
</script>
