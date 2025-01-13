<template>
  <v-snackbar
    v-model="snackbarStore.isVisible"
    :timeout="-1"
    :color="snackbarStore.color"
    location="bottom right"
  >
    <h3>{{ snackbarStore.title }}</h3>
    <span>{{ snackbarStore.message }}</span>
    <template v-slot:actions>
      <v-icon class="mx-2" :icon="mdiCloseCircle" @click="close" />
    </template>
  </v-snackbar>
</template>

<script setup>
  import { useSnackbarStore } from '@/stores/SnackBarStore';
  import { mdiCloseCircle } from '@mdi/js';

  const snackbarStore = useSnackbarStore();

  const close = () => {
    snackbarStore.isVisible = false;
  };
</script>
