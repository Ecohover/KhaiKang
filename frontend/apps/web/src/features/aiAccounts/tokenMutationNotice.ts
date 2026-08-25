import { useSaveNotice } from '../../composables/useSaveNotice'

type Translate = (key: string) => string

export function showTokenCreatedNotice(accountName: string, translate: Translate): void {
  useSaveNotice().showCreated(translate('system.aiAccounts.record'), accountName)
}

export function showTokenRecreatedNotice(accountName: string, translate: Translate): void {
  useSaveNotice().showUpdated(translate('system.aiAccounts.record'), accountName)
}
