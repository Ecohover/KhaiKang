<script setup lang="ts">
import { UiSaveToast, UiSaveToastStack } from '@khaikang/ui'
import { useSaveNotice } from '../composables/useSaveNotice'
import { useI18n } from 'vue-i18n'

const { saveNotices, clearSaveNotice } = useSaveNotice()
const { t } = useI18n()
</script>

<template>
  <UiSaveToastStack :label="t('common.feedback.resultNotifications')">
    <UiSaveToast
      v-for="notice in saveNotices"
      :key="notice.id"
      inline
      :mode="notice.mode"
      :record-label="notice.recordLabel"
      :record-key="notice.recordKey"
      :auto-close="true"
      :auto-close-delay="5000"
      :created-label="t('common.feedback.created')"
      :updated-label="t('common.feedback.updated')"
      :close-label="t('common.feedback.closeSuccess')"
      @close="clearSaveNotice(notice.id)"
    />
  </UiSaveToastStack>
</template>
