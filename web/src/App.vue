<template>
  <v-theme-provider :theme="theme">
    <v-app>
      <profile-off-canvas v-model="profile" />
      <v-app-bar app>
        <v-app-bar-title>
          <router-link to="/"> JASPER </router-link>
        </v-app-bar-title>
        <v-tabs align-tabs="start" v-model="selectedTab">
          <v-tab value="dashboard" to="/dashboard">Dashboard</v-tab>
          <v-tab value="court-list" to="/court-list">Court list</v-tab>
          <v-tab value="court-file-search" to="/court-file-search"
            >Court file search</v-tab
          >
          <v-spacer></v-spacer>
          <div class="d-flex align-center">
            <JudgeSelector
              v-if="selectedTab === 'dashboard' || selectedTab === 'court-list'"
            />
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
  import { ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import JudgeSelector from './components/shared/JudgeSelector.vue';
  import ProfileOffCanvas from './components/shared/ProfileOffCanvas.vue';
  import Snackbar from './components/shared/Snackbar.vue';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const theme = ref(themeStore.state);
  const profile = ref(false);

  const route = useRoute();
  const selectedTab = ref('/dashboard');

  watch(
    () => route.path,
    (newPath) => {
      if (
        newPath.startsWith('/civil-file') ||
        newPath.startsWith('/criminal-file')
      ) {
        selectedTab.value = 'court-file-search';
      } else {
        selectedTab.value = newPath;
      }
    }
  );
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
