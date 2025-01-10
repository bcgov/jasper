<template>
  <v-snackbar
    v-model="snackbarStore.isVisible"
    :timeout="-1"
    :color="snackbarStore.color"
    location="bottom right"
  >
    <h3>{{ snackbarStore.title }}</h3>
    {{ snackbarStore.message }}
    <template v-slot:actions>
      <v-btn variant="text" @click="close"> Close </v-btn>
    </template>
  </v-snackbar>
</template>

<script setup>
  import { useSnackbarStore } from '@/stores/SnackBarStore';

  const snackbarStore = useSnackbarStore();

  const close = () => {
    snackbarStore.isVisible = false;
  };
</script>
