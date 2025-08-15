<template>
  <v-card
    class="my-3"
    color="var(--bg-gray-500)"
    elevation="0"
    data-testid="all-documents-container"
    v-if="documents?.length > 0"
  >
    <v-card-text>
      <v-row align="center" no-gutters>
        <v-col class="text-h5" cols="6">
          All Documents ({{ documents.length }})
        </v-col>
      </v-row>
    </v-card-text>
  </v-card>
  <v-alert
    v-if="binderDocumentIds.length === 0"
    :class="['ml-3', courtClassCdStyle]"
    border="start"
    text="To create a judicial binder, click the ellipsis icon on the document you want to include, then select “Add to Binder”."
  >
    <template #prepend>
      <v-icon :icon="mdiNotebookOutline" />
    </template>
  </v-alert>
  <v-data-table-virtual
    v-if="documents?.length"
    :model-value="selectedItems"
    @update:model-value="handleSelectedItemsChange"
    :headers="baseHeaders"
    :items="documents"
    :sort-by="sortBy"
    return-object
    item-value="civilDocumentId"
    show-select
    class="my-3"
    height="400"
  >
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
        :values="item.issue.map((issue) => issue.issueTypeDesc)"
        :location="Anchor.Top"
      />
    </template>
    <template v-slot:item.binderMenu="{ item }">
      <EllipsesMenu :menuItems="getAllDocumentsMenuItems(item)" />
    </template>
  </v-data-table-virtual>
</template>
<script setup lang="ts">
  import EllipsesMenu from '@/components/shared/EllipsesMenu.vue';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { Anchor, LookupCode } from '@/types/common';
  import { DataTableHeader } from '@/types/shared';
  import { mdiNotebookOutline } from '@mdi/js';
  import { ref } from 'vue';

  const sortBy = ref([{ key: 'fileSeqNo', order: 'desc' }] as const);

  const props = defineProps<{
    selectedItems: civilDocumentType[];
    documents: civilDocumentType[];
    courtClassCdStyle: string;
    rolesLoading: boolean;
    roles: LookupCode[];
    baseHeaders: DataTableHeader[];
    binderDocumentIds: string[];
    addDocumentToBinder: (documentId: string) => void;
    openIndividualDocument: (data: civilDocumentType) => void;
  }>();
  const emit =
    defineEmits<
      (e: 'update:selectedItems', value: civilDocumentType[]) => void
    >();

  const handleSelectedItemsChange = (newItems) => {
    emit('update:selectedItems', [...newItems]);
  };

  const getAllDocumentsMenuItems = (item: civilDocumentType) => {
    return [
      {
        title: 'Add to binder',
        action: () => props.addDocumentToBinder(item.civilDocumentId),
        enable: !props.binderDocumentIds.find(
          (id) => id === item.civilDocumentId
        ),
      },
    ];
  };
</script>
