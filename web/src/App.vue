<template>
  <v-theme-provider :theme="theme">
    <v-app>
      <ProfileOffCanvas v-model="profile" />
      <AppBar @open-profile="profile = true" />
            <v-btn
              class="mr-2"
              size="small"
              variant="outlined"
              :loading="isSendingNotification"
              @click="sendDemoNotification"
            >
              Test Notification
            </v-btn>
      <v-main>
        <router-view />
      </v-main>
      <DarsAccessModal v-model="darsStore.isModalVisible" />
      <Snackbar />
    </v-app>
  </v-theme-provider>
</template>

<script setup lang="ts">
  import { ref } from 'vue';
  import type { HttpService } from '@/services/HttpService';
  import { useCommonStore, useSnackbarStore } from '@/stores';
  import {
    type NotificationDto,
    type NotificationsService,
  } from '@/signalr/notifications';
  import DarsAccessModal from './components/dashboard/DarsAccessModal.vue';
  import AppBar from './components/shared/AppBar.vue';
  import ProfileOffCanvas from './components/shared/ProfileOffCanvas.vue';
  import Snackbar from './components/shared/Snackbar.vue';
  import { useDarsStore } from './stores/DarsStore';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const snackbarStore = useSnackbarStore();
  const darsStore = useDarsStore();
  const theme = ref(themeStore.state);
  const profile = ref(false);
  const notificationsService = inject<NotificationsService>(
    'notificationsService'
  );
  const httpService = inject<HttpService>('httpService');
  const isSendingNotification = ref(false);

  const sendDemoNotification = async () => {
    if (isSendingNotification.value) {
      return;
    }

    isSendingNotification.value = true;
    try {
      await httpService.post('api/notifications/demo', {});
    } finally {
      isSendingNotification.value = false;
    }
  };
</script>

<style>
  .v-tabs {
    flex: 10;
  }

  .v-toolbar-title {
    flex: none;
  }
</style>
