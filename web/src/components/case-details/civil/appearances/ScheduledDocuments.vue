<template>
  <v-data-table-virtual
    class="documents-table"
    :headers
    :items="documents"
    item-value="appearanceId"
  >
    <template v-slot:item.activity="{ item }">
      <div v-for="info in item.documentSupport" :key="info.actCd">
        {{ info.actCd }}
      </div>
    </template>
    <template v-slot:item.issue="{ item }">
      <LabelWithTooltip
        v-if="item.issue?.length > 0"
        :values="item.issue.map((issue) => issue.issueTypeDesc)"
        :location="Anchor.Top"
      />
    </template>
  </v-data-table-virtual>
</template>

<script setup lang="ts">
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { civilDocumentType } from '@/types/civil/jsonTypes/index';
  import { Anchor } from '@/types/common';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';

  defineProps<{
    documents: civilDocumentType[];
  }>();

  const headers = [
    { title: 'SEQ', key: 'fileSeqNo' },
    { title: 'DOCUMENT TYPE', key: 'category' },
    { title: 'ACT', key: 'activity' },
    {
      title: 'DATE FILED',
      key: 'filedDt',
      value: (item) => formatDateToDDMMMYYYY(item.filedDt),
      sortRaw: (a: civilDocumentType, b: civilDocumentType) =>
        new Date(a.filedDt).getTime() - new Date(b.filedDt).getTime(),
    },
    { title: 'FILED BY', key: 'filedByName' },
    { title: 'RESULTS', key: 'runtime' },
    { title: 'ISSUES', key: 'issue' },
  ];
</script>

<style scoped>
  .documents-table {
    background-color: var(--bg-gray-200) !important;
    padding-bottom: 2rem !important;
  }
</style>
