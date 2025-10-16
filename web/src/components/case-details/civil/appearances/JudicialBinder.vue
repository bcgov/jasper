<!-- Judicial Binder specifically for Appearances -->
<template>
  <v-data-table-virtual :items="documents" :headers="headers" height="200">
    <template v-slot:item.documentTypeDescription="{ item }">
      <a
        v-if="item.imageId"
        href="javascript:void(0)"
        @click="openIndividualDocument(item)"
      >
        {{ item.documentTypeDescription }}
      </a>
      <span v-else>
        {{ item.documentTypeDescription }}
      </span>
    </template>
    <template v-slot:item.activity="{ item }">
      <v-chip-group>
        <div v-for="info in item.documentSupport" :key="info.actCd">
          <v-chip rounded="lg">{{ info.actCd }}</v-chip>
        </div>
      </v-chip-group>
    </template>
    <template v-slot:item.filedBy="{ item }">
      <LabelWithTooltip
        v-if="item.filedBy?.length > 0"
        :values="item.filedBy.map((p) => p.filedByName)"
        :location="Anchor.Top"
      />
    </template>
    <template v-slot:item.issue="{ item }">
      <LabelWithTooltip
        v-if="item.issue?.length > 0"
        :values="item.issue.map((issue) => issue.issueDsc)"
        :location="Anchor.Top"
      />
    </template>
  </v-data-table-virtual>
</template>
<script setup lang="ts">
  import {
    getCivilDocumentType,
    prepareCivilDocumentData,
  } from '@/components/documents/DocumentUtils';
  import shared from '@/components/shared';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { Anchor } from '@/types/common';
  import { DataTableHeader } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';

  const props = defineProps<{
    documents: civilDocumentType[];
  }>();

  const headers: DataTableHeader[] = [
    {
      title: 'SEQ',
      key: 'fileSeqNo',
    },
    {
      title: 'DOCUMENT TYPE',
      key: 'documentTypeDescription',
    },
    {
      title: 'ACT',
      key: 'activity',
    },
    {
      title: 'DATE FILED',
      key: 'filedDt',
      value: (item: civilDocumentType) => formatDateToDDMMMYYYY(item.filedDt),
      sortRaw: (a: civilDocumentType, b: civilDocumentType) =>
        new Date(a.filedDt).getTime() - new Date(b.filedDt).getTime(),
    },
    {
      title: 'FILED BY',
      key: 'filedBy',
    },
    {
      title: 'ISSUES',
      key: 'issue',
    },
  ];

  const openIndividualDocument = (data: civilDocumentType) =>
    shared.openDocumentsPdf(
      getCivilDocumentType(data),
      prepareCivilDocumentData(data)
    );
</script>
