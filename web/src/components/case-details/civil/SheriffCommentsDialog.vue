<template>
  <v-dialog v-model="show">
    <v-card class="pa-3">
      <v-card-title
        ><div class="d-flex justify-space-between align-center w-100">
          <span>Sheriff Comments</span>
          <v-btn icon @click="show = false">
            <v-icon :icon="mdiClose" size="32" />
          </v-btn>
        </div>
      </v-card-title>
      <v-card-text class="pl-3">{{ comments }}</v-card-text>
      <v-card-actions class="justify-start">
        <v-btn-secondary text="Close" @click="show = false" />
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
<script setup lang="ts">
  import { mdiClose } from '@mdi/js';

  defineProps<{ comments: string }>();

  const show = defineModel<boolean>({ type: Boolean, required: true });
</script>
<style scoped>
  .v-dialog {
    max-width: 500px;
  }

  .v-dialog .v-card {
    border-radius: 1rem;
  }
</style>
