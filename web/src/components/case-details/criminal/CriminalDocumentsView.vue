<template>
  <v-row>
    <v-col cols="6" />
    <v-col cols="3" class="ml-auto" v-if="participants.length > 1">
      <name-filter v-model="selectedAccused" :people="participants" />
    </v-col>
  </v-row>
  <div
    v-for="(documentList, type) in {
      keyDocuments: keyDocuments,
      documents: documents,
    }"
    :key="type"
  >
    <v-row v-if="type === 'documents'">
      <v-col cols="6" />
      <v-col cols="3" class="ml-auto" v-if="documentCategories.length > 1">
        <ChipMultiSelect
          v-model="selectedCategories"
          :items="documentCategories"
          :select-all-count="unfilteredDocuments.length"
        />
      </v-col>
    </v-row>
    <v-card
      class="my-6"
      color="var(--bg-gray-500)"
      elevation="0"
      v-if="unfilteredDocuments?.length > 0"
    >
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="6">
            {{
              type === 'keyDocuments' ? 'Key Documents' : documentsSectionTitle
            }}
            ({{ documentList.length }})
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      v-if="documentList?.length"
      v-model="selectedItems"
      :headers="type === 'documents' ? headers : keyDocumentHeaders"
      :items="documentList"
      :sort-by="type === 'documents' ? sortBy : keyDocumentsSortBy"
      :group-by
      show-select
      return-object
      class="my-3"
      style="max-height: 50vh; overflow: auto"
    >
      <template
        v-slot:[`group-header`]="{ item, columns, isGroupOpen, toggleGroup }"
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
      <template v-slot:[`item.category`]="{ item }: { item: documentType }">
        {{ item.category }}
      </template>
      <template
        v-slot:[`item.documentTypeDescription`]="{
          item,
        }: {
          item: documentType;
        }"
      >
        <v-row>
          <v-col>
            <a
              v-if="item.imageId"
              href="#"
              @click.prevent="openIndividualDocument(item)"
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
            type === 'keyDocuments' &&
            item.category?.toLowerCase() === 'bail' &&
            item.docmDispositionDsc?.toLowerCase() === 'perfected'
          "
          no-gutters
        >
          <v-col>
            {{ item.docmDispositionDsc }}
            <span class="pl-2" />
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
    formatDocumentType,
    getCriminalDocumentType,
    prepareCriminalDocumentData,
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
  import {
    DEFAULT_OTHER_LABEL,
    getActiveSelections,
    getSectionTitle,
    getUncategorizedCount,
    isAllOptionsSelected,
    matchesCategorySelection,
  } from '../common/categoryFilterUtils';
  import ChipMultiSelect from '../common/ChipMultiSelect.vue';

  const props = defineProps<{ participants: criminalParticipantType[] }>();

  const selectedItems = ref<documentType[]>([]);
  const showActionbar = computed<boolean>(
    () => selectedItems.value.filter((item) => item.imageId).length > 1
  );
  const sortBy = ref([{ key: 'issueDate', order: 'desc' }] as const);
  const keyDocumentsSortBy = ref([{ key: 'category', order: 'asc' }] as const);
  const selectedCategories = ref<string[]>([]);
  const selectedAccused = ref<string>();
  const OTHER_CATEGORY = DEFAULT_OTHER_LABEL;
  type CriminalViewDocument = documentType & {
    fullName?: string;
    fullNameLastFirst?: string;
    profSeqNo?: string | number;
    id: string;
  };

  const isAllSelected = computed<boolean>(() =>
    isAllOptionsSelected(
      selectedCategories.value,
      documentCategories.value.length
    )
  );

  const activeCategories = computed<string[]>(() =>
    getActiveSelections(selectedCategories.value, isAllSelected.value)
  );

  const filterByCategory = (item: CriminalViewDocument) => {
    return matchesCategorySelection(
      item,
      activeCategories.value,
      (doc) => doc.category,
      {
        otherLabel: OTHER_CATEGORY,
      }
    );
  };

  const filterByAccused = (item: CriminalViewDocument) =>
    !selectedAccused.value ||
    (!!item.fullName &&
      formatFromFullname(item.fullName) === selectedAccused.value);

  const unfilteredDocuments = computed(
    () =>
      props.participants?.flatMap((participant) =>
        (participant.document ?? []).map((doc) => ({
          ...doc,
          fullName: participant.fullName || '',
          profSeqNo: participant.profSeqNo,
          id:
            doc.category +
            doc.issueDate +
            participant.fullName +
            doc.partId +
            participant.profSeqNo +
            (doc.docmId || doc.imageId || ''),
        }))
      ) || []
  );

  const unfilteredKeyDocuments = computed(
    () =>
      props.participants?.flatMap((participant) =>
        (participant.keyDocuments || []).map((doc) => ({
          ...doc,
          fullName: participant.fullName || '',
          fullNameLastFirst: participant.fullNameLastFirst || '',
          profSeqNo: participant.profSeqNo,
          id:
            doc.category +
            doc.issueDate +
            participant.fullName +
            doc.partId +
            participant.profSeqNo +
            (doc.docmId || doc.imageId || ''),
        }))
      ) || []
  );
  const documents = computed(() =>
    unfilteredDocuments.value.filter(filterByCategory).filter(filterByAccused)
  );

  const keyDocuments = computed(() =>
    unfilteredKeyDocuments.value.filter(filterByAccused)
  );

  const getUncategorizedDocumentCount = (): number =>
    getUncategorizedCount(unfilteredDocuments.value, (doc) => doc.category);

  const categoryCount = (category: string): number => {
    if (category.toLowerCase() === OTHER_CATEGORY.toLowerCase()) {
      return getUncategorizedDocumentCount();
    }

    return unfilteredDocuments.value.filter(
      (doc) => doc.category?.trim().toLowerCase() === category.toLowerCase()
    ).length;
  };

  const documentsSectionTitle = computed<string>(() =>
    getSectionTitle(activeCategories.value)
  );

  const documentCategories = computed<
    { title: string; value: string; count: number }[]
  >(() => {
    const uncategorizedDocumentCount = getUncategorizedDocumentCount();

    return [
      ...new Set(
        unfilteredDocuments.value
          ?.filter((doc) => doc.category)
          .map((doc) => doc.category) || []
      ),
    ]
      .map((category) => ({
        title: category,
        value: category,
        count: categoryCount(category),
      }))
      .concat(
        uncategorizedDocumentCount > 0
          ? [
              {
                title: OTHER_CATEGORY,
                value: OTHER_CATEGORY,
                count: uncategorizedDocumentCount,
              },
            ]
          : []
      );
  });

  const groupBy = ref([
    {
      key: 'fullName',
      order: 'asc' as const,
    },
  ]);
  const headers = [
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
        const order = ['Initiating', 'ROP', 'Bail', 'Report'];
        const getOrder = (cat: string) => {
          const idx = order.indexOf(cat);
          return idx === -1 ? order.length : idx;
        };
        return getOrder(a.category) - getOrder(b.category);
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
      physicalFileId: string;
    }[] = [];

    selectedItems.value
      .filter((item) => item.imageId)
      .forEach((item: documentType) => {
        const criminalDocType = getCriminalDocumentType(item);
        let documentType: DocumentRequestType;

        if (criminalDocType === CourtDocumentType.Criminal) {
          documentType = DocumentRequestType.File;
        } else if (criminalDocType === CourtDocumentType.Transcript) {
          documentType = DocumentRequestType.Transcript;
        } else {
          documentType = DocumentRequestType.ROP;
        }

        const documentData = prepareCriminalDocumentData(item);
        documents.push({
          documentType,
          documentData,
          groupKeyOne: documentData.aslCourtFileNumber || '',
          groupKeyTwo: documentData.partyName || '',
          documentName: documentData.documentDescription || '',
          physicalFileId: documentData.fileId || '',
        });
      });
    shared.openMergedDocuments(documents);
  };
</script>
