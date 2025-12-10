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
      <v-list-item-title>My Timebank</v-list-item-title>
    </v-list-item>

    <v-list density="compact" v-model:opened="openedGroups">
      <QuickLinksMenu />
    </v-list>

    <template v-slot:append>
      <v-list-item color="primary" rounded="shaped">
        <template v-slot:prepend>
          <v-icon :icon="mdiWeatherNight"></v-icon>
        </template>
        <v-list-item-title>Dark mode</v-list-item-title>
        <template v-slot:append>
          <v-switch v-model="appliedThemes" value="dark" hide-details />
        </template>
      </v-list-item>
    </template>
  </v-navigation-drawer>

  <TimebankModal
    v-if="showTimebankModal && userJudgeId"
    v-model="showTimebankModal"
    :judge-id="userJudgeId"
  />
</template>

<script setup lang="ts">
  import QuickLinksMenu from '@/components/shared/QuickLinksMenu.vue';
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
  const appliedThemes = ref(['']);
  const openedGroups = ref(['quick-links']); // Expand Quick links by default
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

  watch(appliedThemes, (newValue) => {
    // Currently we only support light and dark themes
    if (newValue.includes('dark')) {
      themeStore.changeState('dark');
    } else {
      themeStore.changeState('light');
    }
  });
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
