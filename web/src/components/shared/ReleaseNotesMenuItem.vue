<template>
  <v-list-item
    v-if="releaseNotesUrl"
    color="primary"
    rounded="shaped"
    @click="openJasperReleaseNotes"
  >
    <template #prepend>
      <v-icon
        :icon="mdiUpdate"
        :class="hasUnviewedReleaseNotes ? 'release-notes-icon-emphasis' : ''"
      ></v-icon>
    </template>
    <v-list-item-title
      :class="
        hasUnviewedReleaseNotes ? 'font-weight-bold release-notes-emphasis' : ''
      "
    >
      What's new in JASPER?
    </v-list-item-title>
  </v-list-item>
</template>

<script setup lang="ts">
  import { UserService } from '@/services/UserService';
  import { useCommonStore } from '@/stores/CommonStore';
  import { mdiUpdate } from '@mdi/js';
  import { computed, inject } from 'vue';

  const commonStore = useCommonStore();
  const userService = inject<UserService>('userService');

  const appVersion = computed(() => commonStore.currentAppVersion);
  const releaseNotesUrl = computed(() => commonStore.releaseNotesUrl);
  const hasUnviewedReleaseNotes = computed(
    () => commonStore.hasUnviewedReleaseNotes
  );

  const refreshCurrentUserInfo = async (): Promise<void> => {
    if (!userService) {
      return;
    }

    try {
      const user = await userService.getMyUser();
      commonStore.setUserInfo(user ?? null);
    } catch (error) {
      console.error(
        'Failed to load release notes state for current user.',
        error
      );
    }
  };

  const openJasperReleaseNotes = async () => {
    if (!releaseNotesUrl.value) {
      return;
    }

    const currentVersion = appVersion.value;
    window.open(releaseNotesUrl.value, '_blank', 'noopener,noreferrer');

    if (!userService || !currentVersion) {
      return;
    }

    try {
      await userService.markReleaseNotesViewed(currentVersion);
      await refreshCurrentUserInfo();
    } catch (error) {
      console.error('Failed to update release notes viewed state.', error);
    }
  };
</script>

<style scoped>
  .release-notes-emphasis {
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  .release-notes-icon-emphasis {
    color: rgb(var(--v-theme-on-surface)) !important;
    opacity: 1 !important;
  }
</style>
