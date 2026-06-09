<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    :sort-by="sortBy"
    :row-props="getRowProps"
    :style="maxHeight ? { maxHeight: `${maxHeight}px` } : undefined"
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
    <template #[`item.priorityTypeDesc`]="{ item }">
      <span>{{ item.priorityTypeDesc }}</span>
    </template>
    <template #[`item.courtListType`]="{ item }">
      <span>{{ item.courtListTypeDescription }}</span>
    </template>
  </v-data-table-virtual>
</template>
<script setup lang="ts">
  import { Order } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';
  import { DateTime } from 'luxon';
  import { computed, ref } from 'vue';

  type ColumnKey =
    | 'packageId'
    | 'priorityTypeDesc'
    | 'courtListType'
    | 'receivedDate'
    | 'processedDate'
    | 'division'
    | 'fileNumber'
    | 'styleOfCause'
    | 'referralNotes';

  const props = defineProps<{
    data: Order[];
    viewOrderDetails?: (item: Order) => void;
    viewCaseDetails: (item: Order) => void;
    columns?: ColumnKey[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
    highlightAgedOrders?: boolean;
    agedOrdersThresholdDays?: number;
    maxHeight?: number;
  }>();

  const sortBy = ref<{ key: string; order: 'asc' | 'desc' }[]>(
    props.sortBy ?? [{ key: 'receivedDate', order: 'asc' }]
  );

  const isAgedOrder = (order: Order): boolean => {
    if (!props.highlightAgedOrders) {
      return false;
    }
    const thresholdDays = props.agedOrdersThresholdDays ?? 5;
    const received = DateTime.fromFormat(
      order.receivedDate,
      'dd-MMM-yyyy'
    )?.startOf('day');
    if (!received || !received.isValid) {
      return false;
    }
    const today = DateTime.now().startOf('day');
    return today.diff(received, 'days').days > thresholdDays;
  };

  const getRowProps = ({ item }: { item: Order }) => ({
    class: isAgedOrder(item) ? 'aged-row' : undefined,
  });

  const allColumns: Record<ColumnKey, DataTableHeader> = {
    packageId: {
      title: 'PACKAGE #',
      key: 'packageId',
    },
    priorityTypeDesc: {
      title: 'PRIORITY',
      key: 'priorityTypeDesc',
    },
    courtListType: {
      title: 'TYPE',
      key: 'courtListTypeDescription',
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
    referralNotes: {
      title: 'NOTES',
      key: 'referralNotes',
    },
  };

  const headers = computed<DataTableHeader[]>(() => {
    const columnKeys = props.columns || [
      'packageId',
      'priorityTypeDesc',
      'courtListType',
      'receivedDate',
      'division',
      'fileNumber',
      'styleOfCause',
      'referralNotes',
    ];
    return columnKeys.map((key) => allColumns[key]);
  });
</script>

<style scoped>
  :deep(tr.aged-row),
  :deep(tr.aged-row > td) {
    background-color: var(--bg-yellow-500);
  }
  :deep(tr.aged-row:hover),
  :deep(tr.aged-row:hover > td) {
    background-color: var(--bg-yellow-500);
  }
</style>
