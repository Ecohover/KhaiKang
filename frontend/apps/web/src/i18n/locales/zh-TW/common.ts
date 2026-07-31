export const common = {
  actions: {
    cancel: '取消',
    close: '關閉',
    create: '建立',
    createAndContinue: '建立並繼續',
    reload: '重新載入',
    save: '儲存',
  },
  feedback: {
    created: '新增成功',
    updated: '修改成功',
    closeSuccess: '關閉成功提示',
    resultNotifications: '操作結果通知',
  },
  viewMode: {
    label: '顯示方式',
    list: '清單',
    grid: '卡片',
  },
  search: {
    clear: '清除搜尋',
  },
  fields: {
    status: '狀態',
  },
  pagination: {
    navigation: '分頁導覽',
    summary: '共 {count} 筆',
    pageSize: '每頁',
    previous: '上一頁',
    next: '下一頁',
    page: '第 {page} / {total} 頁',
  },
  time: {
    hoursAgo: '{count} 小時前',
  },
  errors: {
    apiUnavailable: '無法連線到 KhaiKang API。',
  },
} as const
