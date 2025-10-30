<template>
  <v-skeleton-loader class="p-3" type="card" :loading="loading">
    <v-row v-if="details.appearanceMethods?.length">
      <v-col>
        <v-card title="Appearance Methods" variant="flat">
          <v-card-text>
            <CriminalAppearanceMethods :appearanceMethods="details.appearanceMethods" />
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
    <v-card title="Key Documents" variant="flat">
      <v-data-table-virtual
        :items="details.keyDocuments"
        :headers
        :sortBy
        density="compact"
      >
        <template v-slot:item.docmClassification="{ item }">
          {{ formatDocumentCategory(item) }}
        </template>
        <template v-slot:item.docmFormDsc="{ item }">
          <a
            v-if="item.imageId"
            href="javascript:void(0)"
            @click="openDocument(item)"
          >
            {{ formatDocumentType(item) }}
          </a>
          <span v-else>
            {{ formatDocumentType(item) }}
          </span>
          <div v-if="item.category?.toLowerCase() === 'bail'">
            {{ item.docmDispositionDsc }}<span class="pl-2" />
            {{ formatDateToDDMMMYYYY(item.issueDate) }}
          </div>
        </template>
      </v-data-table-virtual>
    </v-card>
    <v-row>
      <v-col>
        <v-card title="Charges" variant="flat">
          <v-data-table-virtual
            :items="details.charges"
            :headers="chargeHeaders"
            style="background-color: rgba(248, 211, 119, 0.52)"
            density="compact"
          >
              <template v-slot:item.lastResults="{ value, item }">
                <v-tooltip :text="item.appearanceResultDesc" location="top">
                  <template v-slot:activator="{ props }">
                    <span v-bind="props" class="has-tooltip">{{ item.appearanceResultCd }}</span>
                  </template>
                </v-tooltip>
              </template>
                <template v-slot:item.pleaCode="{ value, item }">
                  <v-row>
                    <v-col>
                      {{ value }}
                    </v-col>
                  </v-row>
                  <v-row v-if="item.pleaDate" no-gutters>
                    <v-col>
                      {{ formatDateInstanceToDDMMMYYYY(new Date(item.pleaDate)) }}
                    </v-col>
                  </v-row>
              </template>
          </v-data-table-virtual>
        </v-card>
      </v-col>
    </v-row>
  </v-skeleton-loader>
</template>

<script setup lang="ts">
  import CriminalAppearanceMethods from '@/components/case-details/criminal/appearances/CriminalAppearanceMethods.vue';
  import shared from '@/components/shared';
  import { beautifyDate } from '@/filters';
  import { formatDateInstanceToDDMMMYYYY, formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { FilesService } from '@/services/FilesService';
  import { useCommonStore } from '@/stores';
  import {
    CriminalAppearanceDetails,
    documentType,
  } from '@/types/criminal/jsonTypes';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { formatDocumentCategory, formatDocumentType } from '@/components/documents/DocumentUtils';
  import { inject, onMounted, ref } from 'vue';

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
    { title: 'LAST RESULTS', key: 'lastResults' },
    { title: 'PLEA', key: 'pleaCode' },
    { title: 'FINDINGS', key: 'findingDsc' }
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
        const order = ['Initiating', 'rop', 'Bail', 'PSR'];
        const getOrder = (cat: string) => {
          const idx = order.indexOf(cat);
          return idx === -1 ? order.length : idx;
        };
        return getOrder(b.category ?? b.docmClassification) - getOrder(a.category ?? a.docmClassification);
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

  const openDocument = (document: documentType) => {
    const isRop = document.category?.toLowerCase() === 'rop';
    const locationName = commonStore.courtRoomsAndLocations.filter(
      (location) => location.agencyIdentifierCd == details.value.agencyId
    )[0]?.name;
    const documentData: DocumentData = {
      courtClass: props.courtClass,
      courtLevel: details.value.courtLevelCd.toString(),
      dateFiled: beautifyDate(document.issueDate),
      documentId: document.imageId,
      documentDescription: isRop
        ? 'Record of Proceedings'
        : document.documentTypeDescription,
      fileId: props.fileId,
      fileNumberText: details.value.fileNumberTxt,
      partId: document.partId,
      profSeqNo: details.value.profSeqNo,
      location: locationName,
      isCriminal: true,
      partyName: details.value.accused.fullName,
    };
    shared.openDocumentsPdf(
      isRop ? CourtDocumentType.ROP : CourtDocumentType.Criminal,
      documentData
    );
  };
</script>

<style scoped>
  .v-skeleton-loader {
    display: block;
  }
</style>
