<template>
  <v-snackbar
    v-model="snackbarStore.isVisible"
    :timeout="snackbarStore.timeout"
    :color="snackbarStore.color"
    location="bottom right"
  >
    <h3>{{ snackbarStore.title }}</h3>
    <span>{{ snackbarStore.message }}</span>
    <template v-slot:actions>
      <v-btn
        v-if="snackbarStore.actionLabel && snackbarStore.actionHandler"
        variant="text"
        size="small"
        class="mx-1"
        @click="runAction"
      >
        {{ snackbarStore.actionLabel }}
      </v-btn>
      <v-icon class="mx-2" :icon="mdiCloseCircle" @click="close" />
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { mdiCloseCircle } from '@mdi/js';
  const snackbarStore = useSnackbarStore();
  const runAction = () => {
    snackbarStore.actionHandler?.();
    snackbarStore.hideSnackbar();
  };
  const close = () => {
    snackbarStore.hideSnackbar();
  };
</script>
