<template>
  <v-theme-provider :theme="theme">
    <div
      v-if="commonStore.sessionState === SESSION_STATES.INITIALIZING"
      class="d-flex align-center justify-center ga-3 ma-3"
      role="status"
    >
      <v-progress-circular indeterminate color="primary" />
      <span>Verifying your JASPER access...</span>
    </div>
    <v-app v-else>
      <ProfileOffCanvas v-if="isAuthorized" v-model="profile" />
      <AppBar v-if="isAuthorized" @open-profile="profile = true" />
      <v-main>
        <AuthenticationFailed
          v-if="commonStore.sessionState === SESSION_STATES.FAILED"
        />
        <router-view
          v-else-if="
            $route?.name === 'RequestAccess'
              ? commonStore.sessionState === SESSION_STATES.ACCESS_REQUIRED
              : isAuthorized
          "
        />
      </v-main>
      <DarsAccessModal v-if="isAuthorized" v-model="darsStore.isModalVisible" />
      <Snackbar />
    </v-app>
  </v-theme-provider>
</template>

<script setup lang="ts">
  import { SESSION_STATES } from '@/constants/sessionStates';
  import { computed, ref } from 'vue';
  import AuthenticationFailed from './components/dashboard/AuthenticationFailed.vue';
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
  const isAuthorized = computed(
    () => commonStore.sessionState === SESSION_STATES.AUTHORIZED
  );
  const theme = themeStore.state;
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
