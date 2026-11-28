<template>
  <v-container fluid>
    <v-row justify="center">
      <v-col cols="12" md="8" lg="4">
        <v-card class="text-center pa-4" elevation="2">
          <img :src="logo" alt="JASPER" width="63" />
          <h1 class="text-h5 mt-4">Unable to sign in to JASPER</h1>
          <p class="my-4">
            We were unable to authenticate your account or verify your access.
            Please try signing in again.
          </p>
          <v-btn color="primary" :disabled="isRetrying" @click="retry">
            Try Again
          </v-btn>
          <p class="mt-4 mb-0">
            If the problem persists, please contact your JASPER administrator.
          </p>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import logo from '@/assets/jasper-logo.svg?url';
  import redirectHandlerService from '@/services/RedirectHandlerService';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { ref } from 'vue';

  const snackbarStore = useSnackbarStore();
  const isRetrying = ref(false);

  const retry = () => {
    if (isRetrying.value) return;
    isRetrying.value = true;
    try {
      redirectHandlerService.retrySignIn();
    } catch {
      isRetrying.value = false;
      snackbarStore.showSnackbar(
        'Please try signing in again.',
        'error',
        'Unable to sign in to JASPER'
      );
    }
  };
</script>
