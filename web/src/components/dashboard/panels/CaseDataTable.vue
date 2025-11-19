<template>
  <v-data-table-virtual :headers="headers" :items="data" fixed-header :sort-by>
    <template #item.courtClass="{ item }">
      <span :class="getCourtClassStyle(item.courtClass)">
        {{ getCourtClassLabel(item.courtClass) }}
      </span>
    </template>
    <template #item.styleOfCause="{ item }">
      <a href="#" @click.prevent="viewCaseDetails(item)">
        {{ item.styleOfCause }}
      </a>
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import { Case } from '@/types';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassLabel, getCourtClassStyle } from '@/utils/utils';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    data: Case[];
    viewCaseDetails: (item: Case) => void;
    columns?: (
      | 'fileNumber'
      | 'styleOfCause'
      | 'division'
      | 'decisionDate'
      | 'nextAppearance'
      | 'reason'
      | 'lastAppearance'
      | 'dueDate'
      | 'caseAge'
    )[];
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
  }>();

  const sortBy = ref(props.sortBy || []);

  const allColumns: Record<string, DataTableHeader> = {
    fileNumber: {
      title: 'FILE #',
      key: 'courtFileNumber',
    },
    styleOfCause: {
      title: 'ACCUSED / PARTIES',
      key: 'styleOfCause',
    },
    division: {
      title: 'DIVISION',
      key: 'courtClass',
    },
    decisionDate: {
      title: 'DECISION DATE',
      key: 'decisionDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate)),
    },
    nextAppearance: {
      title: 'NEXT APPEARANCE',
      key: 'dueDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate)),
    },
    reason: {
      title: 'REASON',
      key: 'reason',
    },
    lastAppearance: {
      title: 'LAST APPEARANCE',
      key: 'appearanceDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.appearanceDate)),
    },
    dueDate: {
      title: 'DUE DATE',
      key: 'dueDate',
      value: (item: Case) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate)),
    },
    caseAge: {
      title: 'CASE AGE (days)',
      key: 'ageInDays',
      value: (item: Case) =>
        item.ageInDays === 0 ? '' : item.ageInDays.toString(),
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
