export const common = {
  views: {
    list: '列表',
  },
  actions: {
    confirm: '確認',
    actions: '操作',
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
    name: '名稱',
    description: '說明',
    username: '使用者名稱',
    role: '角色權限',
    joinedAt: '加入時間',
  },
  status: {
    active: '使用中',
    inactive: '已停用',
  },
  navigation: {
    breadcrumb: '麵包屑導覽',
    backToList: '返回列表',
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
    connectionFailed: '網路連線發生問題，請稍後再試。',
  },
  members: {
    title: '成員管理', description: '管理此資源的成員存取權與角色。', count: '共 {count} 位成員',
    searchPlaceholder: '搜尋成員名稱或角色…', add: '新增成員', cancelAdd: '取消新增', addDetails: '新增成員資料', confirmAdd: '確定新增',
    loading: '載入成員列表中…', empty: '目前尚無成員。', emptySearch: '找不到符合條件的成員。', remove: '移除',
    removeConfirm: '確定要移除成員「{username}」嗎？', addPlaceholder: '請輸入使用者名稱', loadFailed: '載入成員列表失敗',
    addFailed: '新增成員失敗', updateFailed: '更新成員角色失敗', removeFailed: '移除成員失敗',
    projectRecord: '專案成員', workspaceRecord: '工作區成員',
  },
  settings: {
    defaultDescription: '更新資源基本資料與狀態', defaultCodeLabel: '資源代碼', version: '版本 v{version}', loading: '載入設定資料中…',
    nameRequired: '名稱 *', namePlaceholder: '請輸入名稱', codeImmutable: '建立後此代碼／前綴無法修改',
    descriptionLabel: '說明／簡介', descriptionPlaceholder: '填寫簡介說明…', statusPermission: '缺乏變更狀態權限',
    saved: '設定已成功儲存！', readOnly: '僅供檢視，無編輯權限', saving: '儲存中…', save: '儲存設定',
  },
} as const
