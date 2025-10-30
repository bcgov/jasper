<template>
  <v-row>
    <v-col cols="6" />
    <v-col cols="3" class="ml-auto" v-if="documentCategories.length > 1">
      <v-select
        v-model="selectedCategory"
        label="Documents"
        placeholder="All documents"
        hide-details
        :items="documentCategories"
      >
        <template v-slot:item="{ props: itemProps, item }">
          <v-list-item
            v-bind="itemProps"
            :title="item.raw + ' (' + categoryCount(item.raw) + ')'"
          ></v-list-item>
        </template>
      </v-select>
    </v-col>
    <v-col cols="3" class="ml-auto" v-if="participants.length > 1">
      <name-filter v-model="selectedAccused" :people="participants" />
    </v-col>
  </v-row>
  <div
    v-for="(documents, type) in {
      keyDocuments: keyDocuments,
      documents: documents,
    }"
    :key="type"
  >
    <v-card
      class="my-6"
      color="var(--bg-gray-500)"
      elevation="0"
      v-if="unfilteredDocuments?.length > 0"
    >
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="6">
            {{ type === 'keyDocuments' ? 'Key Documents' : 'All Documents' }}
            ({{ documents.length }})
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      v-if="documents?.length"
      v-model="selectedItems"
      :headers="type === 'documents' ? headers : keyDocumentHeaders"
      :items="documents"
      :sort-by="type === 'documents' ? sortBy : keyDocumentsSortBy"
      :group-by
      show-select
      return-object
      class="my-3"
      style="max-height: 50vh; overflow: auto"
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
              {{ formatFromFullname(item.value) }}
            </v-banner>
          </td>
        </tr>
      </template>
      <template v-slot:item.category="{ item }">
        {{ formatDocumentCategory(item) }}
      </template>
      <template v-slot:item.documentTypeDescription="{ item }">
        <v-row>
          <v-col>
            <a
              v-if="item.imageId"
              href="javascript:void(0)"
              @click="openIndividualDocument(item)"
            >
              {{ formatDocumentType(item) }}
            </a>
            <span v-else>
              {{ formatDocumentType(item) }}
            </span>
          </v-col>
        </v-row>
        <v-row
          v-if="
            type === 'keyDocuments' && item.category?.toLowerCase() === 'bail'
          "
          no-gutters
        >
          <v-col>
            {{ item.docmDispositionDsc }} <span class="pl-2" />
            {{ formatDateToDDMMMYYYY(item.issueDate) }}
          </v-col>
        </v-row>
      </template>
    </v-data-table-virtual>
  </div>
  <ActionBar v-if="showActionbar" :selected="selectedItems">
    <v-btn
      size="large"
      class="mx-2"
      :prepend-icon="mdiFileDocumentMultipleOutline"
      style="letter-spacing: 0.001rem"
      @click="openMergedDocuments()"
    >
      View together
    </v-btn>
  </ActionBar>
</template>
<script setup lang="ts">
  import {
    getCriminalDocumentType,
    prepareCriminalDocumentData,
    formatDocumentCategory,
    formatDocumentType
  } from '@/components/documents/DocumentUtils';
  import shared from '@/components/shared';
  import NameFilter from '@/components/shared/Form/NameFilter.vue';
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import {
    criminalParticipantType,
    documentType,
  } from '@/types/criminal/jsonTypes';
  import {
    CourtDocumentType,
    DocumentData,
    DocumentRequestType,
  } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { formatFromFullname } from '@/utils/utils';
  import { mdiFileDocumentMultipleOutline } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{ participants: criminalParticipantType[] }>();
  const selectedItems = ref<documentType[]>([]);
  const showActionbar = computed<boolean>(
    () => selectedItems.value.filter((item) => item.imageId).length > 1
  );
  const sortBy = ref([{ key: 'issueDate', order: 'desc' }] as const);
  const keyDocumentsSortBy = ref([{ key: 'category', order: 'desc' }] as const);
  const selectedCategory = ref<string>();
  const selectedAccused = ref<string>();

  const filterByCategory = (item: any) =>
    !selectedCategory.value ||
    item.category?.toLowerCase() === selectedCategory.value?.toLowerCase();
  const filterByAccused = (item: any) =>
    !selectedAccused.value ||
    (item.fullName &&
      formatFromFullname(item.fullName) === selectedAccused.value);

  const unfilteredDocuments = computed(
    () =>
      props.participants?.flatMap((participant) =>
        participant.document?.map((doc) => ({
          ...doc,
          fullName: participant.fullName || '',
          profSeqNo: participant.profSeqNo,
          id:
            doc.category +
            participant.fullName +
            doc.partId +
            participant.profSeqNo,
        }))
      ) || []
  );

  const unfilteredKeyDocuments = computed(
    () =>
      props.participants?.flatMap((participant) =>
        (participant.keyDocuments || []).map((doc) => ({
          ...doc,
          fullName: participant.fullName || '',
          profSeqNo: participant.profSeqNo,
          id:
            doc.category +
            participant.fullName +
            doc.partId +
            participant.profSeqNo,
        }))
      ) || []
  );

  const documents = computed(() =>
    unfilteredDocuments.value.filter(filterByCategory).filter(filterByAccused)
  );
  const keyDocuments = computed(() =>
    unfilteredKeyDocuments.value
      .filter(filterByCategory)
      .filter(filterByAccused)
  );

  const categoryCount = (category: string): number => {
    return unfilteredDocuments.value.filter(
      (doc) => formatDocumentCategory(doc) === category
    ).length;
  };

  const documentCategories = ref<string[]>([
    ...new Set(
      unfilteredDocuments.value
        ?.filter((doc) => doc.category)
        .map((doc) => formatDocumentCategory(doc)) || []
    ),
  ]);

  const groupBy = ref([
    {
      key: 'fullName',
      order: 'asc' as const,
    },
  ]);
  const headers = [
    { key: 'data-table-group' },
    {
      title: 'DATE FILED/ISSUES',
      key: 'issueDate',
      value: (item) => formatDateToDDMMMYYYY(item.issueDate),
      sortRaw: (a: documentType, b: documentType) =>
        new Date(a.issueDate).getTime() - new Date(b.issueDate).getTime(),
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

  const keyDocumentHeaders = [
    { key: 'data-table-group' },
    {
      title: 'DATE FILED/ISSUED',
      key: 'issueDate',
      value: (item) => formatDateToDDMMMYYYY(item.issueDate),
      sortRaw: (a: documentType, b: documentType) =>
        new Date(a.issueDate).getTime() - new Date(b.issueDate).getTime(),
    },
    {
      title: 'DOCUMENT TYPE',
      key: 'documentTypeDescription',
    },
    {
      title: 'CATEGORY',
      key: 'category',
      sortRaw: (a: documentType, b: documentType) => {
        const order = ['Initiating', 'rop', 'Bail', 'PSR'];
        const getOrder = (cat: string) => {
          const idx = order.indexOf(cat);
          return idx === -1 ? order.length : idx;
        };
        return getOrder(b.category) - getOrder(a.category);
      },
    },
    {
      title: 'PAGES',
      key: 'documentPageCount',
    },
  ];

  const openIndividualDocument = (data: documentType) => {
    shared.openDocumentsPdf(
      getCriminalDocumentType(data),
      prepareCriminalDocumentData(data)
    );
  };

  const openMergedDocuments = () => {
    const documents: {
      documentType: DocumentRequestType;
      documentData: DocumentData;
      groupKeyOne: string;
      groupKeyTwo: string;
      documentName: string;
    }[] = [];
    selectedItems.value
      .filter((item) => item.imageId)
      .forEach((item) => {
        const documentType =
          getCriminalDocumentType(item) === CourtDocumentType.Criminal
            ? DocumentRequestType.File
            : DocumentRequestType.ROP;
        const documentData = prepareCriminalDocumentData(item);
        documents.push({
          documentType,
          documentData,
          groupKeyOne: documentData.fileNumberText || '',
          groupKeyTwo: item.fullName || '',
          documentName: documentData.documentDescription || '',
        });
      });

    shared.openMergedDocuments(documents);
  };
</script>
