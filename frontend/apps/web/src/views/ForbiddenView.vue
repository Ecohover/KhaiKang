<template>
  <section class="forbidden-state">
    <p>403</p>
    <h2>沒有權限開啟這個頁面</h2>
    <span>目前登入帳號未具備這項系統功能所需的角色。</span>
    <RouterLink :to="{ name: 'home' }">返回總覽</RouterLink>
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
