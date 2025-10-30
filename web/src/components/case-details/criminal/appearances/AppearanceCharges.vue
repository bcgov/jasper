<template>
  <v-data-table-virtual class="charges-table" :headers :items="charges">
    <template v-slot:item.lastResults="{ value, item }">
      <v-tooltip :text="item.appearanceResultDesc" location="top">
        <template v-slot:activator="{ props }">
          <span v-bind="props" class="has-tooltip">{{ item.appearanceResultCd }}</span>
        </template>
      </v-tooltip>
    </template>
    <template v-slot:item.pleaCode="{ value, item }">
      <v-row>
        <v-col>
          {{ value }}
        </v-col>
      </v-row>
      <v-row v-if="item.pleaDate" no-gutters>
        <v-col>
          {{ formatDateInstanceToDDMMMYYYY(new Date(item.pleaDate)) }}
        </v-col>
      </v-row>
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import { CriminalCharges } from '@/types/criminal/jsonTypes/index';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';

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
      key: 'lastResults',
    },
    { title: 'PLEA', key: 'pleaCode' },
    {
      title: 'FINDINGS',
      key: 'findingDsc',
    },
  ];
</script>

<style scoped>
  .charges-table {
    background-color: var(--bg-gray-200) !important;
    padding-bottom: 2rem !important;
  }
</style>
