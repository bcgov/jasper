<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    fixed-header
  >
  <template #[`item.packageNumber`]="{ item }">
      <a href="#" @click.prevent="viewOrderDetails(item)">
        {{ item.packageNumber }}
      </a>
    </template>
    <template #[`item.courtClass`]="{ item }">
      <span :class="[getCourtClassStyle(item.courtClass)]">
        {{ getCourtClassLabel(item.courtClass) }}
      </span>
    </template>
    <template #[`item.styleOfCause`]="{ item }">
      <a href="#" @click.prevent="viewCaseDetails(item)">
        {{ item.styleOfCause }}
      </a>
    </template>
  </v-data-table-virtual>
</template>
<script setup lang="ts">
  import { Order } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { computed, ref } from 'vue';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';

  const props = defineProps<{
    data: Order[];
    viewOrderDetails: (item: Order) => void;
    viewCaseDetails: (item: Order) => void;
    columns?: (
      | 'packageNumber'
      | 'receivedDate'
      | 'processedDate'
      | 'division'
      | 'fileNumber'
      | 'styleOfCause'
    )[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
  }>();

  const sortBy = ref(props.sortBy || [{ key: 'receivedDate', order: 'asc' }]);

  const allColumns: Record<string, DataTableHeader> = {
    packageNumber: {
      title: 'PACKAGE #',
      key: 'packageNumber',
    },
    receivedDate: {
      title: 'DATE RECEIVED',
      key: 'receivedDate',
      value: (item: Order) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.receivedDate)),
      sort: (a: string, b: string) =>
        new Date(a).getTime() - new Date(b).getTime(),
    },
    processedDate: {
      title: 'DATE PROCESSED',
      key: 'processedDate',
      value: (item: Order) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.processedDate)),
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
      'packageNumber',
      'receivedDate',
      'division',
      'fileNumber',
      'styleOfCause',
    ];
    return columnKeys.map((key) => allColumns[key]);
  });
</script>
