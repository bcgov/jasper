<template>
    <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    fixed-header
  >
    <template #[`item.styleOfCause`]="{ item }">
      <a href="#" @click.prevent="viewOrderDetails(item)">
        {{ item.styleOfCause }}
      </a>
    </template>
</template>
<script setup lang="ts">
  import { Order } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    data: Order[];
    viewOrderDetails: (item: Order) => void;
    columns?: (
      | 'packageNumber'
      | 'dateReceived'
      | 'dateProcessed'
      | 'division'
      | 'fileNumber'
      | 'styleOfCause'
    )[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
  }>();

  const sortBy = ref(props.sortBy || [{ key: 'dateReceived', order: 'asc' }]);

  const allColumns: Record<string, DataTableHeader> = {
    packageNumber: {
      title: 'PACKAGE #',
      key: 'packageNumber',
    },
    dateReceived: {
      title: 'DATE RECEIVED',
      key: 'dateReceived',
      value: (item: Order) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dateReceived)),
      sort: (a: string, b: string) =>
        new Date(a).getTime() - new Date(b).getTime(),
    },
    dateProcessed: {
      title: 'DATE PROCESSED',
      key: 'dateProcessed',
      value: (item: Order) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dateProcessed)),
      sort: (a: string, b: string) =>
        new Date(a).getTime() - new Date(b).getTime(),
    },
    division: {
      title: 'DIVISION',
      key: 'courtClass',
    },
    fileNumber: {
      title: 'FILE #',
      key: 'courtFileNumber',
    },
    styleOfCause: {
      title: 'ACCUSED / PARTIES',
      key: 'styleOfCause',
    },
    
  };

  const headers = computed<DataTableHeader[]>(() => {
    const columnKeys = props.columns || [
      'fileNumber',
      'styleOfCause',
      'division',
      'nextAppearance',
      'reason',
      'lastAppearance',
      'caseAge',
    ];
    return columnKeys.map((key) => allColumns[key]);
  });
</script>