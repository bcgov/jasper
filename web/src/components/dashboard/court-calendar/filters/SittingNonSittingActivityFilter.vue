<template>
  <div class="mx-3">
    <div class="mb-1">Activities</div>
    <!-- Option 1 -->
    <v-btn-toggle v-model="selected1" mandatory class="w-100" density="compact">
      <v-btn value="all" border class="flex-grow-1">All</v-btn>
      <v-btn value="red" border class="flex-grow-1 p-0">Sitting</v-btn>
      <v-btn value="blue" border class="flex-grow-1 p-1">Non-sitting</v-btn>
    </v-btn-toggle>
    <!-- Option 2 -->
    <div>Activities</div>
    <v-btn-toggle v-model="selected2" mandatory class="w-100" density="compact">
      <v-btn value="all" border class="flex-grow-1">All</v-btn>
      <v-btn value="red" border class="flex-grow-1">
        <v-tooltip activator="parent" location="top">Sitting</v-tooltip>
        <v-icon :icon="mdiGavel" size="x-large"></v-icon>
      </v-btn>
      <v-btn value="blue" border class="flex-grow-1">
        <v-tooltip activator="parent" location="top">Non-sitting</v-tooltip>
        <v-icon :icon="mdiCalendarRemove" size="x-large"></v-icon>
      </v-btn>
    </v-btn-toggle>
    <!-- Option 3 -->
    <div>Activities</div>
    <v-select
      v-model="selected3"
      :items="activityItems"
      item-title="label"
      item-value="value"
      density="compact"
      variant="outlined"
      hide-details
    ></v-select>
  </div>
</template>
<script setup lang="ts">
  import { mdiCalendarRemove, mdiGavel } from '@mdi/js';
import { ref } from 'vue';

  const selected1 = ref('all');
  const selected2 = ref('all');
  const selected3 = ref('all');

  const activityItems = [
    { label: 'All activities', value: 'all' },
    { label: 'Sitting', value: 'sitting' },
    { label: 'Non-sitting', value: 'non-sitting' },
  ];
</script>
<style scoped></style>
