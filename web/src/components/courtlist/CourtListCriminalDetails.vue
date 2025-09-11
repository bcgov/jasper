<template>
  <v-skeleton-loader class="p-3" type="card" :loading="loading">
    <v-row v-if="details.appearanceMethods?.length">
      <v-col cols="6">
        <v-card title="Appearance Methods" variant="flat">
          <AppearanceMethods :appearanceMethod="details.appearanceMethods" />
        </v-card>
      </v-col>
    </v-row>
    <v-row>
      <v-col>
        <v-card title="Key Documents" variant="flat">
          <v-data-table-virtual
            :items="details.keyDocuments"
            :headers
            :sortBy
            density="compact"
          >
            <template v-slot:item.docmClassification="{ item }">
              {{ formatCategory(item) }}
            </template>
            <template v-slot:item.docmFormDsc="{ item }">
                <a v-if="item.imageId" href="javascript:void(0)" @click="openDocument(item)">
                  {{ formatType(item) }}
                </a>
              <span v-else>
                  {{ formatType(item) }}
              </span>
            </template>
          </v-data-table-virtual>
        </v-card>
      </v-col>
    </v-row>
    <v-row>
      <v-col>
        <v-card title="Charges" variant="flat">
          <v-data-table-virtual
            :items="details.charges"
            :headers="chargeHeaders"
            style="background-color: rgba(248, 211, 119, 0.52)"
            density="compact"
          >
          </v-data-table-virtual>
        </v-card>
      </v-col>
    </v-row>
  </v-skeleton-loader>
</template>

<script setup lang="ts">
  import AppearanceMethods from '@/components/case-details/civil/appearances/AppearanceMethods.vue';
  import { FilesService } from '@/services/FilesService';
  import {
    CriminalAppearanceDetails,
    documentType,
  } from '@/types/criminal/jsonTypes';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { beautifyDate } from '@/filters';
  import { inject, onMounted, ref } from 'vue';
  import shared from '@/components/shared';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { useCommonStore } from '@/stores';

  const props = defineProps<{
    fileId: string;
    appearanceId: string;
    partId: string;
    courtClass: string;
  }>();

  const filesService = inject<FilesService>('filesService');
  const commonStore = useCommonStore();
  const details = ref<CriminalAppearanceDetails>(
    {} as CriminalAppearanceDetails
  );
  const loading = ref(false);
  const sortBy = ref([{ key: 'docmClassification', order: 'desc' }] as const);
  if (!filesService) {
    throw new Error('Files service is undefined.');
  }
  const chargeHeaders = ref([
    { title: 'COUNT', key: 'printSeqNo' },
    { title: 'CRIMINAL CODE', key: 'statuteSectionDsc' },
    { title: 'DESCRIPTION', key: 'statuteDsc' },
    { title: 'LAST RESULTS', key: 'appearanceResultDesc' },
    { title: 'PLEA', key: '' }, // Awaiting more info on clAppearanceCount
    { title: 'FINDINGS', key: 'findingDsc' },
  ]);

  const headers = ref([
    {
      title: 'DATE FILED/ISSUED',
      key: 'issueDate',
      value: (item) => formatDateToDDMMMYYYY(item.issueDate),
    },
    { title: 'DOCUMENT TYPE', key: 'docmFormDsc' },
    {
      title: 'CATEGORY',
      key: 'docmClassification',
      sortRaw: (a: documentType, b: documentType) => {
        const order = ['Initiating', 'BAIL', 'ROP'];
        const getOrder = (cat: string) => {
          const formatted = cat === 'rop' ? 'ROP' : cat;
          const idx = order.indexOf(formatted);
          return idx === -1 ? order.length : idx;
        };
        return getOrder(b.docmClassification) - getOrder(a.docmClassification);
      },
    },
    { title: 'PAGES', key: 'documentPageCount' },
  ]);

  onMounted(async () => {
    loading.value = true;
    details.value = await filesService.criminalAppearanceDetails(
      props.fileId,
      props.appearanceId,
      props.partId
    );
    loading.value = false;
  });
  const formatCategory = (item: documentType) =>
    item.category === 'rop' ? 'ROP' : item.category;

  const formatType = (item: documentType) =>
    item.category === 'rop'
      ? 'Record of Proceedings'
      : item.documentTypeDescription;

  
  const openDocument = (document: documentType) => {
    const isRop = document.category?.toLowerCase() === 'rop';
    const locationName = commonStore.courtRoomsAndLocations.filter(
      (location) => location.agencyIdentifierCd == details.value.agencyId)[0]?.name;
    const documentData: DocumentData = {
        courtClass: props.courtClass,
        courtLevel:
          details.value.courtLevelCd.toString(),
        dateFiled: beautifyDate(document.issueDate),
        documentId: document.imageId,
        documentDescription:
          isRop
            ? 'Record of Proceedings'
            : document.documentTypeDescription,
        fileId: props.fileId,
        fileNumberText:
          details.value.fileNumberTxt,
        partId: document.partId,
        profSeqNo: details.value.profSeqNo,
        location: locationName,
        isCriminal: true,
      };
    shared.openDocumentsPdf(isRop ? CourtDocumentType.ROP : CourtDocumentType.Criminal, documentData);
  };
</script>

<style scoped>
  .v-skeleton-loader {
    display: block;
  }
</style>
