<template>
  <v-data-table-virtual class="charges-table" :headers :items="charges">
    <template v-slot:item.appearanceResultCd="{ value }">
      <v-chip v-if="value" color="black" rounded="lg" variant="flat">
        {{ value }}
      </v-chip>
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import { CriminalCharges } from '@/types/criminal/jsonTypes/index';

  defineProps<{ charges: CriminalCharges[] | undefined }>();

  const headers = [
    {
      title: 'COUNT',
      key: 'printSeqNo',
    },
    {
      title: 'CRIMINAL CODE',
      key: 'statuteSectionDsc',
    },
    {
      title: 'DESCRIPTION',
      key: 'statuteDsc',
    },
    {
      title: 'LAST RESULTS',
      key: 'appearanceResultCd',
    },
    {
      title: 'FINDINGS',
      key: 'findingCd',
    },
  ];
</script>

<style scoped>
  .charges-table {
    background-color: var(--bg-very-light-gray) !important;
    padding-bottom: 2rem !important;
  }
</style>
