<template>
  <v-navigation-drawer v-model="model" location="right" temporary>
    <v-list-item color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiAccountCircle" size="35" />
      </template>
      <v-list-item-title>{{ userName }}</v-list-item-title>
      <template v-slot:append>
        <v-btn size="25" :icon="mdiCloseCircle" @click="model = false" />
      </template>
    </v-list-item>

    <v-divider></v-divider>

    <v-list-item
      v-if="canViewCourtCalendar"
      color="primary"
      rounded="shaped"
      @click="openTimebankModal"
    >
      <template v-slot:prepend>
        <v-icon :icon="mdiCalendarClock"></v-icon>
      </template>
      <v-list-item-title style="font-size: 0.85rem"
        >My Timebank</v-list-item-title
      >
    </v-list-item>

    <v-list-item color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiWeatherNight"></v-icon>
      </template>
      <v-list-item-title style="font-size: 0.85rem"
        >Dark mode</v-list-item-title
      >
      <template v-slot:append>
        <v-switch v-model="isDark" hide-details @click="toggleDark" />
      </template>
    </v-list-item>
  </v-navigation-drawer>

  <TimebankModal
    v-if="showTimebankModal && userJudgeId"
    v-model="showTimebankModal"
    :judge-id="userJudgeId"
  />
</template>

<script setup lang="ts">
  import { useCommonStore } from '@/stores/CommonStore';
  import { useThemeStore } from '@/stores/ThemeStore';
  import { UserInfo } from '@/types/common';
  import {
    mdiAccountCircle,
    mdiCalendarClock,
    mdiCloseCircle,
    mdiWeatherNight,
  } from '@mdi/js';
  import { computed, ref, watch } from 'vue';
  import TimebankModal from './TimebankModal.vue';

  const model = defineModel<boolean>();
  const themeStore = useThemeStore();
  const commonStore = useCommonStore();
  const theme = ref(themeStore.state);
  const isDark = ref(theme.value === 'dark');
  const showTimebankModal = ref(false);

  const canViewCourtCalendar = computed(() => {
    // Check if user has permission to view court calendar
    // For now, we check if userInfo exists (logged in user)
    return commonStore.userInfo !== null;
  });

  const userJudgeId = computed(() => {
    return commonStore.userInfo?.judgeId;
  });
  const userName = ref<string>(commonStore.userInfo?.userTitle || '');

  function toggleDark() {
    themeStore.changeState(theme.value === 'dark' ? 'light' : 'dark');
  }

  function openTimebankModal() {
    showTimebankModal.value = true;
  }

  watch(
    () => commonStore.userInfo,
    (newUserInfo: UserInfo | null) => {
      if (!newUserInfo) {
        return;
      }
      userName.value = newUserInfo.userTitle || '';
    }
  );
</script>

<style>
  div.v-list-item__spacer {
    width: 15px !important;
  }
  .v-list-item-title {
    white-space: normal;
    word-break: break-word;
  }
</style>
