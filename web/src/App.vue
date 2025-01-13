<template>
  <v-theme-provider :theme="theme">
    <v-app>
      <profile-off-canvas v-model="profile" @close="profile = false" />
      <v-app-bar app>
        <v-app-bar-title>
          <router-link to="/"> JASPER </router-link>
        </v-app-bar-title>
        <v-tabs align-tabs="start">
          <v-tab to="/dashboard">Dashboard</v-tab>
          <v-tab to="/court-list">Court list</v-tab>
          <v-tab to="/court-file-search">Court file search</v-tab>
          <v-spacer></v-spacer>
          <div class="d-flex align-center">
            <v-btn
              class="ma-2"
              @click.stop="profile = true"
              :icon="mdiAccountCircle"
              size="x-large"
              style="font-size: 1.5rem"
            />
          </div>
        </v-tabs>
      </v-app-bar>

      <v-main>
        <router-view />
      </v-main>
      <snackbar />
    </v-app>
  </v-theme-provider>
</template>

<script setup lang="ts">
  import { mdiAccountCircle } from '@mdi/js';
  import { ref } from 'vue';
  import ProfileOffCanvas from './components/shared/ProfileOffCanvas.vue';
  import Snackbar from './components/shared/Snackbar.vue';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const theme = ref(themeStore.state);
  const profile = ref(false);
</script>

<style>
  .v-tabs {
    flex: 10;
  }
  .v-app-bar-title a {
    text-decoration: none;
    color: inherit;
  }
</style>
