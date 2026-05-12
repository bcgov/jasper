<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    fixed-header
  >
    <template #[`item.packageId`]="{ item }">
      <a
        v-if="viewOrderDetails"
        href="#"
        @click.prevent="viewOrderDetails(item)"
      >
        {{ item.packageId }}
      </a>
      <span v-else>{{ item.packageId }}</span>
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
    <template #[`item.priorityType`]="{ item }">
      <LabelWithTooltip
        v-if="item.priorityTypeDescription"
        :values="[item.priorityType, item.priorityTypeDescription || '']"
        :append-count="false"
        :location="Anchor.Top"
      />
      <span v-else>
        {{ item.priorityType }}
      </span>
    </template>
    <template #[`item.courtListType`]="{ item }">
      <span>{{ item.courtListType }}</span>
    </template>
  </v-data-table-virtual>
</template>
<script setup lang="ts">
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { Order } from '@/types';
  import { Anchor } from '@/types/common';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';
  import { computed, ref } from 'vue';

  type ColumnKey =
    | 'packageId'
    | 'priorityType'
    | 'courtListType'
    | 'receivedDate'
    | 'processedDate'
    | 'division'
    | 'fileNumber'
    | 'styleOfCause';

  const props = defineProps<{
    data: Order[];
    viewOrderDetails?: (item: Order) => void;
    viewCaseDetails: (item: Order) => void;
    columns?: ColumnKey[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
  }>();

  const sortBy = ref<{ key: string; order: 'asc' | 'desc' }[]>(
    props.sortBy ?? [{ key: 'receivedDate', order: 'asc' }]
  );

  const allColumns: Record<ColumnKey, DataTableHeader> = {
    packageId: {
      title: 'PACKAGE #',
      key: 'packageId',
    },
    priorityType: {
      title: 'PRIORITY',
      key: 'priorityType',
    },
    courtListType: {
      title: 'TYPE',
      key: 'courtListType',
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
      'packageId',
      'priorityType',
      'courtListType',
      'receivedDate',
      'division',
      'fileNumber',
      'styleOfCause',
    ];
    return columnKeys.map((key) => allColumns[key]);
  });
</script>
