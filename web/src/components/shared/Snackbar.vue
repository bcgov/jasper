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
      <v-icon class="mx-2" :icon="mdiCloseCircle" @click="close" />
    </template>
  </v-snackbar>
</template>

<script setup>
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { mdiCloseCircle } from '@mdi/js';
  const snackbarStore = useSnackbarStore();
  const close = () => {
    snackbarStore.isVisible = false;
  };
</script>
