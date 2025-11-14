<template>
  <v-navigation-drawer v-model="model" location="right" temporary>
    <v-list-item color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiAccountCircle" size="35" />
      </template>
      <v-list-item-title style="white-space: normal; word-break: break-word;">{{ judgeName }}</v-list-item-title>
      <template v-slot:append>
        <v-btn size="25" :icon="mdiCloseCircle" @click="model = false" />
      </template>
    </v-list-item>

    <v-divider></v-divider>

    <v-list-item color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiWeatherNight"></v-icon>
      </template>
      <v-list-item-title style="white-space: normal; word-break: break-word; font-size: .85rem;">Dark mode</v-list-item-title>
      <template v-slot:append>
        <v-switch v-model="isDark" hide-details @click="toggleDark" />
      </template>
    </v-list-item>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
  import { useThemeStore } from '@/stores/ThemeStore';
  import { mdiAccountCircle, mdiCloseCircle, mdiWeatherNight } from '@mdi/js';
  import { useCommonStore } from '@/stores';
  import { UserInfo } from '@/types/common';
  import { ref, watch } from 'vue';

  const model = defineModel<boolean>();
  const themeStore = useThemeStore();
  const theme = ref(themeStore.state);
  const isDark = ref(theme.value === 'dark');
  
  const commonStore = useCommonStore();
  const judgeName = ref<string>(
    commonStore.userInfo?.judgeName || ''
  );

  function toggleDark() {
    themeStore.changeState(theme.value === 'dark' ? 'light' : 'dark');
  }

  watch(
    () => commonStore.userInfo,
    (newUserInfo: UserInfo | null) => {
      if (!newUserInfo) {
        return;
      }
      judgeName.value = newUserInfo.judgeName || '';
    }
  );
</script>

<style>
  div.v-list-item__spacer {
    width: 15px !important;
  }
</style>
