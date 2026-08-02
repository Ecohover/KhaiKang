export const common = {
  views: {
    list: 'List',
  },
  actions: {
    confirm: 'Confirm',
    actions: 'Actions',
    cancel: 'Cancel',
    close: 'Close',
    create: 'Create',
    createAndContinue: 'Create and continue',
    reload: 'Reload',
    save: 'Save',
  },
  feedback: {
    created: 'Created',
    updated: 'Updated',
    closeSuccess: 'Close success message',
    resultNotifications: 'Operation result notifications',
  },
  viewMode: {
    label: 'View mode',
    list: 'List',
    grid: 'Cards',
  },
  search: {
    clear: 'Clear search',
  },
  fields: {
    status: 'Status',
  },
  pagination: {
    navigation: 'Pagination',
    summary: '{count} records',
    pageSize: 'Per page',
    previous: 'Previous',
    next: 'Next',
    page: 'Page {page} / {total}',
  },
  time: {
    hoursAgo: '{count} hours ago',
  },
  errors: {
    apiUnavailable: 'Unable to connect to KhaiKang API.',
  },
} as const
