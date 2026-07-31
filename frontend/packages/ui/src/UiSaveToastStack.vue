<template>
  <Teleport to="body">
    <TransitionGroup
      name="ui-save-toast-stack"
      tag="section"
      class="ui-save-toast-stack"
      aria-label="操作結果通知"
    >
      <slot />
    </TransitionGroup>
  </Teleport>
</template>

<style>
.ui-save-toast-stack {
  position: fixed;
  right: max(20px, env(safe-area-inset-right));
  bottom: max(20px, env(safe-area-inset-bottom));
  z-index: 1000;
  display: flex;
  width: min(390px, calc(100vw - 32px));
  max-height: calc(100vh - 40px);
  flex-direction: column;
  gap: 12px;
  overflow: visible;
  pointer-events: none;
}

.ui-save-toast-stack .ui-save-toast {
  pointer-events: auto;
}

.ui-save-toast-stack-enter-active,
.ui-save-toast-stack-leave-active,
.ui-save-toast-stack-move {
  transition:
    opacity 300ms ease,
    transform 380ms cubic-bezier(.22, 1, .36, 1);
}

.ui-save-toast-stack-enter-from {
  opacity: 0;
  transform: translateY(-24px) scale(.985);
}

.ui-save-toast-stack-leave-to {
  opacity: 0;
  transform: translateY(18px) scale(.985);
}

.ui-save-toast-stack-leave-active {
  position: absolute;
  width: 100%;
}

@media (prefers-reduced-motion: reduce) {
  .ui-save-toast-stack-enter-active,
  .ui-save-toast-stack-leave-active,
  .ui-save-toast-stack-move {
    transition-duration: 1ms;
  }
}

@media (max-width: 520px) {
  .ui-save-toast-stack {
    right: 16px;
    bottom: 16px;
    left: 16px;
    width: auto;
    max-height: calc(100vh - 32px);
  }
}
</style>
