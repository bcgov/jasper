<template>
  <v-theme-provider :theme="theme">
    <v-app>
      <ProfileOffCanvas v-model="profile" />
      <AppBar @open-profile="profile = true" />
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
  import DarsAccessModal from './components/dashboard/DarsAccessModal.vue';
  import AppBar from './components/shared/AppBar.vue';
  import ProfileOffCanvas from './components/shared/ProfileOffCanvas.vue';
  import Snackbar from './components/shared/Snackbar.vue';
  import { useDarsStore } from './stores/DarsStore';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const darsStore = useDarsStore();
  const theme = ref(themeStore.state);
  const profile = ref(false);
</script>

<style>
  .v-tabs {
    flex: 10;
  }

  .v-toolbar-title {
    flex: none;
  }
</style>
