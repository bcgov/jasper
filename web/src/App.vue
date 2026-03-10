<template>
  <v-theme-provider :theme="theme">
    <div
      v-if="!commonStore.isInitialized"
      class="d-flex align-center justify-center ma-3"
    >
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-app v-else>
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
  import { useCommonStore } from './stores/CommonStore';
  import { useDarsStore } from './stores/DarsStore';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const darsStore = useDarsStore();
  const commonStore = useCommonStore();
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

  @keyframes badge-pop {
    0% {
      transform: scale(1);
    }
    40% {
      transform: scale(1.2);
    }
    100% {
      transform: scale(1);
    }
  }
</style>
