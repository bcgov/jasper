<!-- Judicial Binder specifically for Appearances -->
<template>
  <v-data-table-virtual :items="documents" :headers="headers" height="200">
    <template v-slot:item.documentTypeDescription="{ item }">
      <a
        v-if="item.imageId"
        href="javascript:void(0)"
        @click="openDocument(item)"
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
  import shared from '@/components/shared';
  import { useCommonStore } from '@/stores';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { Anchor } from '@/types/common';

  const props = defineProps<{
    documents: civilDocumentType[];
    fileId: string;
    fileNumberTxt: string;
    courtLevel: string;
    agencyId: string;
  }>();

  const headers = shared.getBaseCivilDocumentTableHeaders();
  const commonStore = useCommonStore();

  const openDocument = (document: civilDocumentType) => {
    shared.openCivilDocument(
      document,
      props.fileId,
      props.fileNumberTxt,
      props.courtLevel,
      props.agencyId,
      commonStore.courtRoomsAndLocations
    );
  };
</script>
