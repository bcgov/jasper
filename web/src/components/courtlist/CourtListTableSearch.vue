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
        hide-details
      >
      </v-select>
    </v-col>
    <v-col cols="2">
      <v-select
        v-model="selectedAMPM"
        label="Time"
        placeholder="AM and PM"
        :items="['AM', 'PM']"
        hide-details
      >
      </v-select>
    </v-col>
    <v-col cols="2" class="d-flex align-center">
      <action-buttons :showSearch="false" size="large" @reset="reset" />
    </v-col>
    <v-col class="d-flex align-center justify-end">
      <v-menu location="start">
        <template v-slot:activator="{ props }">
          <v-icon v-bind="props" hover :icon="mdiMenu" size="32" />
        </template>
        <v-list>
          <v-list-item
            v-for="item in menuItems"
            :key="item.title"
            @click="onMenuClicked(item.value)"
            hover
          >
            {{ item.title }}
          </v-list-item>
        </v-list>
      </v-menu>
    </v-col>
  </v-row>
</template>
<script setup lang="ts">
  import { mdiMagnify, mdiMenu } from '@mdi/js';
  import { inject } from 'vue';
  import ActionButtons from '../shared/Form/ActionButtons.vue';
  const selectedFiles = defineModel<string>('filesFilter');
  const selectedAMPM = defineModel<string>('AMPMFilter');
  const search = defineModel<string>('search');

  const reset = () => {
    selectedFiles.value = undefined;
    selectedAMPM.value = undefined;
    search.value = undefined;
  };

  const onMenuClicked = inject<(value: string) => void>('menuClicked')!;

  const menuItems = [{ title: 'Print Court List', value: 'print' }];
</script>
