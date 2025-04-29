<template>
  <v-row>
    <v-col cols="6" />
    <v-col cols="3" class="ml-auto" v-if="documentTypes.length > 1">
      <v-select
        v-model="selectedType"
        label="Documents"
        placeholder="All documents"
        hide-details
        :items="documentTypes"
      />
    </v-col>
  </v-row>
  <div
    v-for="(documents, type) in {
      // binder: binder,
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
            All Documents ({{ filteredDocuments.length }})
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      v-if="documents?.length"
      v-model="selectedItems"
      :headers="headers"
      :items="filteredDocuments"
      :sort-by="sortBy"
      item-value="civilDocumentId"
      show-select
      class="my-3"
      height="400"
    >
      <template v-slot:item.documentTypeDescription="{ item }">
        <a
          v-if="item.imageId"
          href="javascript:void(0)"
          @click="cellClick({ item })"
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
        <span v-for="(role, index) in item.filedBy" :key="index">
          <span v-if="role.roleTypeCode">
            <v-skeleton-loader type="text" :loading="rolesLoading">
              {{
                roles ? getLookupShortDescription(role.roleTypeCode, roles) : ''
              }}
            </v-skeleton-loader>
          </span>
        </span>
      </template>
      <template v-slot:item.issue="{ item }">
        <LabelWithTooltip
          v-if="item.issue?.length > 0"
          :values="item.issue.map((issue) => issue.issueTypeDesc)"
          :location="Anchor.Top"
        />
      </template>
      <!-- <template v-slot:item.binderMenu>
        <EllipsesMenu :menuItems="menuItems" />
      </template> -->
    </v-data-table-virtual>
  </div>
</template>

<script setup lang="ts">
  import shared from '@/components/shared';
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { beautifyDate } from '@/filters';
  import { useCivilFileStore } from '@/stores';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { Anchor, LookupCode } from '@/types/common';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { getLookupShortDescription, getRoles } from '@/utils/utils';
  import { computed, onMounted, ref } from 'vue';

  const props = defineProps<{ documents: civilDocumentType[] }>();

  const civilFileStore = useCivilFileStore();
  const selectedItems = defineModel<civilDocumentType[]>();
  const sortBy = ref([{ key: 'fileSeqNo', order: 'desc' }] as const);
  const selectedType = ref<string>();
  const menuItems = [{ title: 'Add to binder' }];
  const rolesLoading = ref(false);
  const roles = ref<LookupCode[]>();
  const headers = [
    { key: 'data-table-group' },
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
      value: (item) => formatDateToDDMMMYYYY(item.filedDt),
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
    // {
    //   title: 'JUDICIAL BINDER',
    //   key: 'binderMenu',
    //   width: '2%',
    // },
  ];
  const documentTypes = ref<any[]>([
    ...new Map(
      props.documents.map((doc) => [
        doc.documentTypeCd,
        { title: doc.documentTypeDescription, value: doc.documentTypeCd },
      ])
    ).values(),
  ]);
  const filterByType = (item: any) =>
    !selectedType.value ||
    item.documentTypeCd?.toLowerCase() === selectedType.value?.toLowerCase();

  const filteredDocuments = computed(() =>
    props.documents.filter(filterByType)
  );

  onMounted(async () => {
    rolesLoading.value = true;
    roles.value = await getRoles();
    rolesLoading.value = false;
  });

  // This is code ported over from 'civil/CivilDocumentsView.vue' to keep file viewing capability
  // This will eventually be deprecated in favor of Nutrient PDF viewing functionality
  const cellClick = (eventData) => {
    const documentType =
      eventData.item.documentTypeCd == 'CSR'
        ? CourtDocumentType.CSR
        : CourtDocumentType.Civil;
    const documentData: DocumentData = {
      appearanceDate: beautifyDate(eventData.item.lastAppearanceDt),
      appearanceId:
        eventData.item.appearanceId ?? eventData.item.civilDocumentId,
      dateFiled: beautifyDate(eventData.item.filedDt),
      documentDescription: eventData.item.documentTypeCd,
      documentId: eventData.item.civilDocumentId,
      fileId: civilFileStore.civilFileInformation.fileNumber,
      fileNumberText: eventData.item.documentTypeDescription,
      courtClass: civilFileStore.civilFileInformation.detailsData.courtClassCd,
      courtLevel: civilFileStore.civilFileInformation.detailsData.courtLevelCd,
      location:
        civilFileStore.civilFileInformation.detailsData.homeLocationAgencyName,
    };
    shared.openDocumentsPdf(documentType, documentData);
  };
</script>

<style scoped>
  .v-chip {
    cursor: default;
  }
</style>
