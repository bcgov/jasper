<template>
  <v-navigation-drawer location="right" temporary>
    <v-list-item subtitle="JSmith" color="primary" rounded="shaped">
      <template v-slot:prepend>
        <v-icon :icon="mdiAccountCircle" size="45" />
      </template>
      <v-list-item-title>John Smith</v-list-item-title>
      <template v-slot:append>
        <v-btn :icon="mdiCloseCircle" @click="$emit('close')" />
      </template>
    </v-list-item>

    <v-divider></v-divider>

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
</template>

<script setup lang="ts">
  import { useThemeStore } from '@/stores/ThemeStore';
  import { mdiAccountCircle, mdiCloseCircle, mdiWeatherNight } from '@mdi/js';
  import { ref } from 'vue';

  const themeStore = useThemeStore();
  const theme = ref(themeStore.state);
  const isDark = ref(theme.value === 'dark');

  function toggleDark() {
    themeStore.changeState(theme.value === 'dark' ? 'light' : 'dark');
  }
</script>

<style>
  div.v-list-item__spacer {
    width: 15px !important;
  }
</style>
