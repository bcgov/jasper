<template>
  <div
    v-for="(documents, type) in {
      keyDocuments: keyDocuments,
      documents: documents,
    }"
    :key="type"
  >
    <v-card
      class="my-6"
      color="var(--bg-gray)"
      elevation="0"
      v-if="documents?.length > 0"
    >
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="6">
            {{ type === 'keyDocuments' ? 'Key documents' : 'Documents' }}
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      v-if="documents?.length"
      v-model="selectedItems"
      :headers="headers"
      :items="documents"
      :sort-by="sortBy"
      :group-by
      show-select
      class="my-3"
      height="400"
    >
      <template
        v-slot:group-header="{ item, columns, isGroupOpen, toggleGroup }"
      >
        <tr>
          <td class="pa-0" style="height: 1rem" :colspan="columns.length">
            <v-banner
              class="table-banner"
              :ref="
                () => {
                  if (!isGroupOpen(item)) toggleGroup(item);
                }
              "
            >
              {{ item.value }}
            </v-banner>
          </td>
        </tr>
      </template>
      <template v-slot:item.category="{ item }">
        {{ formatCategory(item) }}
      </template>
      <template v-slot:item.documentTypeDescription="{ item }">
        <a
          v-if="item.imageId"
          href="javascript:void(0)"
          @click="cellClick({ item })"
        >
          {{ formatType(item) }}
        </a>
      </template>
    </v-data-table-virtual>
  </div>
</template>
<script setup lang="ts">
  import shared from '@/components/shared';
import { beautifyDate } from '@/filters';
import { useCriminalFileStore } from '@/stores';
import {
  criminalParticipantType,
  documentType,
} from '@/types/criminal/jsonTypes';
import { CourtDocumentType, DocumentData } from '@/types/shared';
import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
import { ref } from 'vue';

  const props = defineProps<{ participants: criminalParticipantType[] }>();
  const keyDocuments = [];
  const criminalFileStore = useCriminalFileStore();
  const sortBy = ref([{ key: 'appearanceDt', order: 'asc' }] as const);
  const selectedItems = defineModel<criminalParticipantType[]>();
  const documents = props.participants?.flatMap(
    (participant) =>
      participant.document?.map((doc) => ({
        ...doc,
        name: participant.fullName,
        profSeqNo: participant.profSeqNo,
        id: crypto.randomUUID(),
      })) || []
  );
  const groupBy = ref([
    {
      key: 'name',
      order: 'asc' as const,
    },
  ]);
  const headers = [
    { key: 'data-table-group' },
    {
      title: 'DATE SWORN/FILED',
      key: 'issueDate',
      value: (item) => formatDateToDDMMMYYYY(item.issueDate),
    },
    {
      title: 'DOCUMENT TYPE',
      key: 'documentTypeDescription',
    },
    {
      title: 'CATEGORY',
      key: 'category',
    },
    {
      title: 'PAGES',
      key: 'documentPageCount',
    },
  ];

  const formatCategory = (item: documentType) =>
    item.category === 'rop' ? 'ROP' : item.category;
  const formatType = (item: documentType) =>
    item.category === 'rop'
      ? 'Record of Proceedings'
      : item.documentTypeDescription;

  // This is code ported over from 'CriminalDocumentsView.vue' to keep file viewing capability
  // This will eventually be deprecated in favor of Nutrient PDF viewing functionality
  const cellClick = (data) => {
    console.log(data.item?.documentType);
    const ropDescription = 'Record of Proceedings';
    const documentType =
      data.item?.category?.toLowerCase() === 'rop'
        ? CourtDocumentType.ROP
        : CourtDocumentType.Criminal;
    const documentData: DocumentData = {
      courtClass:
        criminalFileStore.criminalFileInformation.detailsData.courtClassCd,
      courtLevel:
        criminalFileStore.criminalFileInformation.detailsData.courtLevelCd,
      dateFiled: beautifyDate(data.item.date),
      documentId: data.item?.imageId,
      documentDescription:
        data.item?.category?.toLowerCase() === 'rop'
          ? ropDescription
          : data.item?.documentTypeDescription,
      fileId: criminalFileStore.criminalFileInformation.fileNumber,
      fileNumberText:
        criminalFileStore.criminalFileInformation.detailsData.fileNumberTxt,
      partId: data.item?.partId,
      profSeqNo: data.item?.profSeqNo,
      location:
        criminalFileStore.criminalFileInformation.detailsData
          .homeLocationAgencyName,
    };

    shared.openDocumentsPdf(documentType, documentData);
  };
</script>
