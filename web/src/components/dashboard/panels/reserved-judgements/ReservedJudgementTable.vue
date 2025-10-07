<template>
  <v-data-table-virtual
    :headers="headers"
    :items="data"
    height="400"
    fixed-header
    :sort-by
  >
    <template #item.courtClass="{ item }">
      {{ getCourtClassLabel(item.courtClass) }}
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import { ReservedJudgement } from '@/types/ReservedJudgement';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassLabel } from '@/utils/utils';
  import { ref } from 'vue';

  defineProps<{
    data: ReservedJudgement[];
  }>();
  const sortBy = ref([{ key: 'cc', order: 'desc' }] as const);

  const headers = ref([
    {
      title: 'FILE #',
      key: 'courtFileNumber',
    },
    {
      title: 'ACCUSED / PARTIES',
      key: 'styleOfCause',
    },
    {
      title: 'DIVISION',
      key: 'courtClass',
    },
    {
      title: 'DECISION DATE',
      key: 'dueDate',
      value: (item: ReservedJudgement) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate)),
    },
    {
      title: 'REASON',
      key: 'reason',
    },
    {
      title: 'LAST APPEARANCE',
      key: 'appearanceDate',
      value: (item: ReservedJudgement) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.appearanceDate)),
    },
    {
      title: 'DUE DATE',
      key: 'dueDate',
      value: (item: ReservedJudgement) =>
        formatDateInstanceToDDMMMYYYY(new Date(item.dueDate)),
    },
    {
      title: 'CASE AGE (days)',
      key: 'ageInDays',
    },
  ]);
</script>
