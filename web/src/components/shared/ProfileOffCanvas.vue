<template>
  <v-navigation-drawer v-model="model" location="right" temporary>
    <v-list-item subtitle="JSmith" color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiAccountCircle" size="45" />
      </template>
      <v-list-item-title>John Smith</v-list-item-title>
      <template v-slot:append>
        <v-btn :icon="mdiCloseCircle" @click="model = false" />
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

    <v-list-item color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiWeatherNight"></v-icon>
      </template>
      <v-list-item-title>Dark mode</v-list-item-title>
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
  import {
    mdiAccountCircle,
    mdiCalendarClock,
    mdiCloseCircle,
    mdiWeatherNight,
  } from '@mdi/js';
  import { computed, ref } from 'vue';
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

  function toggleDark() {
    themeStore.changeState(theme.value === 'dark' ? 'light' : 'dark');
  }

  function openTimebankModal() {
    showTimebankModal.value = true;
  }
</script>

<style>
  div.v-list-item__spacer {
    width: 15px !important;
  }
</style>
