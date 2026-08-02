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
    name: 'Name',
    description: 'Description',
    username: 'Username',
    role: 'Role',
    joinedAt: 'Joined',
  },
  status: {
    active: 'Active',
    inactive: 'Inactive',
  },
  navigation: {
    breadcrumb: 'Breadcrumb navigation',
    backToList: 'Back to list',
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
    connectionFailed: 'A network connection error occurred. Please try again later.',
  },
  members: {
    title: 'Members', description: 'Manage member access and roles for this resource.', count: '{count} member | {count} members',
    searchPlaceholder: 'Search members or roles…', add: 'Add member', cancelAdd: 'Cancel adding', addDetails: 'Add member details', confirmAdd: 'Add member',
    loading: 'Loading members…', empty: 'No members yet.', emptySearch: 'No matching members found.', remove: 'Remove',
    removeConfirm: 'Remove {username} from this resource?', addPlaceholder: 'Enter a username', loadFailed: 'Unable to load members.',
    addFailed: 'Unable to add the member.', updateFailed: 'Unable to update the member role.', removeFailed: 'Unable to remove the member.',
    projectRecord: 'Project member', workspaceRecord: 'Workspace member',
  },
  settings: {
    defaultDescription: 'Update basic information and status.', defaultCodeLabel: 'Resource code', version: 'Version {version}', loading: 'Loading settings…',
    nameRequired: 'Name *', namePlaceholder: 'Enter a name', codeImmutable: 'This code or prefix cannot be changed after creation.',
    descriptionLabel: 'Description', descriptionPlaceholder: 'Enter a description…', statusPermission: 'You do not have permission to change status.',
    saved: 'Settings saved.', readOnly: 'Read-only. You do not have permission to edit.', saving: 'Saving…', save: 'Save settings',
  },
} as const
