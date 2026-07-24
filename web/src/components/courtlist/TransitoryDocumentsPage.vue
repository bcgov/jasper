<template>
  <TransitoryDocumentsTable
    :model-value="true"
    :location-id="locationId"
    :location="location"
    :room-cd="roomCd"
    :date="date"
    standalone
    @update:model-value="close"
  />
</template>

<script setup lang="ts">
  import TransitoryDocumentsTable from './TransitoryDocumentsTable.vue';

  defineProps<{
    locationId: string;
    roomCd: string;
    date: string;
    location: string;
  }>();

  const close = (): void => {
    window.close();
  };
</script>
