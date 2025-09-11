<template>
  <v-data-table-virtual
    class="documents-table"
    :headers
    :items="documents"
    item-value="appearanceId"
  >
    <template v-slot:item.documentTypeDescription="{ item }">
      <a v-if="item.imageId" href="javascript:void(0)" @click="openDocument(item)">
        {{ item.documentTypeDescription }}
      </a>
      <span v-else>
        {{ item.documentTypeDescription }}
      </span>
    </template>
    <template v-slot:item.activity="{ item }">
      <div v-for="info in item.documentSupport" :key="info.actCd">
        {{ info.actCd }}
      </div>
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
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import shared from '@/components/shared';
  import { civilDocumentType } from '@/types/civil/jsonTypes/index';
  import { beautifyDate } from '@/filters';
  import { Anchor } from '@/types/common';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { useCommonStore } from '@/stores';

  const props = defineProps<{
    documents: civilDocumentType[];
    fileId: string;
    fileNumberTxt: string;
    courtLevel: string;
    agencyId: string;
  }>();

  const headers = [
    { title: 'SEQ', key: 'fileSeqNo' },
    { title: 'DOCUMENT TYPE', key: 'documentTypeDescription' },
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
  const commonStore = useCommonStore();

  const openDocument = (document: civilDocumentType) => {
    const locationName = commonStore.courtRoomsAndLocations.filter(
        (location) => location.agencyIdentifierCd == props.agencyId)[0]?.name;
      const isCsr = document.category?.toLowerCase() === 'csr';
      const documentData: DocumentData = {
        dateFiled: beautifyDate(document.DateGranted),
        documentId: document.civilDocumentId,
        documentDescription: document.documentTypeDescription,
        fileId: props.fileId,
        fileNumberText: props.fileNumberTxt,
        courtLevel: props.courtLevel,
        partId: document.partId,
        profSeqNo: document.fileSeqNo,
        location: locationName,
        isCriminal: false,
      };
    shared.openDocumentsPdf(isCsr ? CourtDocumentType.CSR : CourtDocumentType.Civil, documentData);
  };
</script>

<style scoped>
  .documents-table {
    background-color: var(--bg-gray-200) !important;
    padding-bottom: 2rem !important;
  }
</style>
