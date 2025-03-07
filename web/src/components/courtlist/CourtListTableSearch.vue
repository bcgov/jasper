<template>
  <v-row class="pb-3">
    <v-col cols="4">
      <v-text-field
        id="search"
        v-model="search"
        title="Search"
        label="Search"
        variant="outlined"
        clearable
        hide-details
        single-line
        :prepend-inner-icon="mdiMagnify"
      />
    </v-col>
    <v-col cols="2">
      <v-select
        v-model="selectedFiles"
        label="Files"
        placeholder="All files"
        :items="['To be called', 'Complete', 'Cancelled']"
      >
      </v-select>
    </v-col>
    <v-col cols="2">
      <v-select
        v-model="selectedAMPM"
        label="Time"
        placeholder="AM and PM"
        :items="['AM', 'PM']"
      >
      </v-select>
    </v-col>
    <v-col cols="2">
      <action-buttons :showSearch="false" size="large" @reset="reset" />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
  import { mdiMagnify } from '@mdi/js';
  import ActionButtons from '../shared/Form/ActionButtons.vue';

  const selectedFiles = defineModel<string>('filesFilter');
  const selectedAMPM = defineModel<string>('AMPMFilter');
  const search = defineModel<string>('search');

  const reset = () => {
    selectedFiles.value = undefined;
    selectedAMPM.value = undefined;
    search.value = undefined;
  };
</script>
