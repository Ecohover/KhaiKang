<script setup lang="ts">
import { ArrowLeft } from '@lucide/vue'
import { UiFormActionBar } from '@khaikang/ui'
import type { RouteLocationRaw } from 'vue-router'
import { useRouter } from 'vue-router'

const props = withDefaults(defineProps<{
  backTo: RouteLocationRaw
  backLabel: string
  meta?: string
  title: string
  description: string
  loading?: boolean
  loadingLabel?: string
  error?: string
  showErrorSlot?: boolean
  showActions?: boolean
}>(), {
  meta: '',
  loading: false,
  loadingLabel: '',
  error: '',
  showErrorSlot: false,
  showActions: true,
})

const router = useRouter()
</script>

<template>
  <section class="resource-form-layout">
    <button class="resource-form-layout__back" type="button" @click="router.push(props.backTo)">
      <ArrowLeft :size="16" aria-hidden="true" />{{ backLabel }}
    </button>

    <header class="resource-form-layout__heading">
      <p v-if="meta" class="resource-form-layout__meta">{{ meta }}</p>
      <h2>{{ title }}</h2>
      <p>{{ description }}</p>
    </header>

    <p v-if="loading" class="resource-form-layout__state">{{ loadingLabel }}</p>
    <div v-if="error || showErrorSlot" class="resource-form-layout__error" role="alert">
      <slot name="error"><p>{{ error }}</p></slot>
    </div>
    <div v-if="!loading" class="resource-form-layout__content"><slot /></div>
    <UiFormActionBar v-if="!loading && showActions && $slots.actions" mode="floating">
      <slot name="actions" />
    </UiFormActionBar>
  </section>
</template>

<style scoped>
.resource-form-layout { --resource-form-gap: 18px; display: grid; width: min(100%, 960px); gap: var(--resource-form-gap); margin: 0 auto; padding-bottom: 40px; }
.resource-form-layout__back { display: flex; width: fit-content; align-items: center; gap: 6px; padding: 0; color: var(--kk-text-muted); background: transparent; border: 0; cursor: pointer; }
.resource-form-layout__heading h2 { margin: 3px 0 7px; font-size: clamp(1.65rem, 3vw, 2.2rem); }
.resource-form-layout__heading > p:not(.resource-form-layout__meta) { margin: 0; color: var(--kk-text-muted); }
.resource-form-layout__meta { margin: 0; color: var(--kk-accent); font-size: .75rem; font-weight: 750; letter-spacing: .08em; text-transform: uppercase; }
.resource-form-layout__content { display: grid; gap: 18px; }
.resource-form-layout__content > :deep(form) { display: grid; gap: var(--resource-form-gap); }
.resource-form-layout__state { margin: 0; padding: 42px 24px; text-align: center; background: var(--kk-surface); border: 1px dashed var(--kk-border-strong); border-radius: var(--kk-radius); }
.resource-form-layout__error { color: var(--kk-danger); }
.resource-form-layout__error :deep(p) { margin: 0; }
</style>
