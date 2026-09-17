<template>
  <v-card
    class="my-3"
    color="var(--bg-gray-500)"
    elevation="0"
    data-testid="all-documents-container"
    v-if="documents?.length > 0 || props.hasActiveFilters"
  >
    <v-card-text>
      <v-row align="center" no-gutters>
        <v-col class="text-headline-small" cols="6">
          {{ props.sectionTitle || 'All Documents' }}
          ({{ documents.length }})
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
    :headers="headers"
    :items="documents"
    v-model:sort-by="activeSort"
    :must-sort="!!pinToBottom"
    return-object
    item-value="civilDocumentId"
    show-select
    class="my-3"
    height="800"
  >
    <template v-slot:[`item.documentTypeDescription`]="{ item }">
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
    <template v-slot:[`item.activity`]="{ item }">
      <v-chip-group>
        <div v-for="info in item.documentSupport" :key="info.actCd">
          <v-chip rounded="lg">{{ info.actCd }}</v-chip>
        </div>
      </v-chip-group>
    </template>
    <template v-slot:[`item.filedBy`]="{ item }">
      <LabelWithTooltip
        v-if="item.filedBy?.length > 0"
        :values="item.filedBy.map((p) => p.filedByName)"
        :location="Anchor.Top"
      />
    </template>
    <template v-slot:[`item.issue`]="{ item }">
      <LabelWithTooltip
        v-if="item.issue?.length > 0"
        :values="item.issue.map((issue) => issue.issueDsc)"
        :location="Anchor.Top"
      />
    </template>
    <template v-slot:[`item.binderMenu`]="{ item }">
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
  import { computed, ref, watch } from 'vue';

  const props = defineProps<{
    selectedItems: civilDocumentType[];
    documents: civilDocumentType[];
    courtClassCdStyle: string;
    rolesLoading: boolean;
    roles: LookupCode[];
    baseHeaders: DataTableHeader[];
    binderDocumentIds: string[];
    addDocumentToBinder: (document: civilDocumentType) => void;
    hasActiveFilters?: boolean;
    sectionTitle?: string;
    sortBy?: { key: string; order: 'asc' | 'desc' }[];
    pinToBottom?: (document: civilDocumentType) => boolean;
    openIndividualDocument: (data: civilDocumentType) => void;
  }>();
  const emit =
    defineEmits<
      (e: 'update:selectedItems', value: civilDocumentType[]) => void
    >();

  const handleSelectedItemsChange = (newItems) => {
    emit('update:selectedItems', [...newItems]);
  };

  const activeSort = ref([...(props.sortBy ?? [])]);

  watch(
    () => props.sortBy,
    (sortBy) => {
      activeSort.value = [...(sortBy ?? [])];
    },
    { deep: true }
  );

  const compareValues = (valueA: unknown, valueB: unknown): number =>
    String(valueA ?? '').localeCompare(String(valueB ?? ''), undefined, {
      numeric: true,
      sensitivity: 'base',
    });

  const headers = computed<DataTableHeader[]>(() =>
    props.baseHeaders.map((header) => {
      if (header.sortable === false || !props.pinToBottom) {
        return header;
      }

      const baseComparator = header.sortRaw;
      return {
        ...header,
        sortRaw: (
          documentA: civilDocumentType,
          documentB: civilDocumentType
        ) => {
          const isPinnedA = props.pinToBottom?.(documentA) ?? false;
          const isPinnedB = props.pinToBottom?.(documentB) ?? false;

          if (isPinnedA !== isPinnedB) {
            const displayedOrder = isPinnedA ? 1 : -1;
            const sortOrder = activeSort.value.find(
              (sort) => sort.key === header.key
            )?.order;
            return sortOrder === 'desc' ? -displayedOrder : displayedOrder;
          }

          return baseComparator
            ? baseComparator(documentA, documentB)
            : compareValues(documentA[header.key], documentB[header.key]);
        },
      };
    })
  );

  const getAllDocumentsMenuItems = (item: civilDocumentType) => {
    return [
      {
        title: 'Add to binder',
        action: () => props.addDocumentToBinder(item),
        enable: !props.binderDocumentIds.find(
          (id) => id === item.civilDocumentId
        ),
      },
    ];
  };
</script>
