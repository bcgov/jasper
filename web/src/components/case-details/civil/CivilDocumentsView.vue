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
            :title="`${item.title} (${categoryCount(item.raw)})`"
          ></v-list-item>
        </template>
      </v-select>
    </v-col>
  </v-row>

  <JudicialBinder
    v-model="selectedBinderItems"
    :courtClassCdStyle
    :binderDocuments
    :isBinderLoading
    :rolesLoading
    :roles="roles ?? []"
    :openIndividualDocument
    :removeDocumentFromBinder
    :deleteBinder="deleteCurrentBinder"
    :viewBinder="() => openMergedDocuments(binderDocuments)"
    :baseHeaders="headers"
    @update:reordered="(documentData) => handleReordering(documentData)"
  />

  <AllDocuments
    :courtClassCdStyle
    :documents="filteredDocuments"
    :rolesLoading
    :roles="roles ?? []"
    :baseHeaders="headers"
    :addDocumentToBinder
    :openIndividualDocument
    :selectedItems
    :binderDocumentIds="currentBinder?.documents.map((d) => d.documentId) ?? []"
    @update:selectedItems="(val) => (selectedItems = val)"
  />

  <ActionBar
    :selected="selectedBinderItems"
    selectionPrependText="Judicial Binder document/s"
  >
    <v-btn
      size="large"
      class="mx-2"
      :prepend-icon="mdiFileDocumentMultipleOutline"
      style="letter-spacing: 0.001rem"
      @click="openMergedDocuments(selectedBinderItems)"
    >
      View together
    </v-btn>
    <v-btn
      size="large"
      class="mx-2"
      :prepend-icon="mdiNotebookRemoveOutline"
      style="letter-spacing: 0.001rem"
      @click="removeSelectedJudicialDocuments()"
    >
      Remove
    </v-btn>
  </ActionBar>
  <ActionBar
    v-if="showActionbar"
    :selected="selectedItems"
    selectionPrependText="Documents"
  >
    <template #default>
      <v-btn
        size="large"
        class="mx-2"
        :prepend-icon="mdiFileDocumentMultipleOutline"
        style="letter-spacing: 0.001rem"
        @click="openMergedDocuments(selectedItems)"
        :disabled="!enableViewTogether"
      >
        View together
      </v-btn>
      <v-btn
        size="large"
        class="mx-2"
        :prepend-icon="mdiNotebookOutline"
        style="letter-spacing: 0.001rem"
        @click="addSelectedItemsToBinder"
      >
        Add to Binder
      </v-btn>
    </template>
  </ActionBar>
</template>

<script setup lang="ts">
  import {
    getCivilDocumentType,
    prepareCivilDocumentData,
  } from '@/components/documents/DocumentUtils';
  import shared from '@/components/shared';
  import ActionBar from '@/components/shared/table/ActionBar.vue';
  import { BinderService } from '@/services';
  import { useCommonStore } from '@/stores';
  import { Binder, BinderDocument } from '@/types';
  import { ApiResponse } from '@/types/ApiResponse';
  import { civilDocumentType } from '@/types/civil/jsonTypes';
  import { LookupCode } from '@/types/common';
  import {
    CourtDocumentType,
    DataTableHeader,
    DocumentData,
    DocumentRequestType,
  } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassStyle, getRoles } from '@/utils/utils';
  import {
    mdiFileDocumentMultipleOutline,
    mdiNotebookOutline,
    mdiNotebookRemoveOutline,
  } from '@mdi/js';
  import { computed, inject, onMounted, ref } from 'vue';
  import AllDocuments from './documents/AllDocuments.vue';
  import JudicialBinder from './documents/JudicialBinder.vue';

  const props = defineProps<{
    documents: civilDocumentType[];
    courtClassCd: string;
    fileId: string;
  }>();

  const binderService = inject<BinderService>('binderService');
  const commonStore = useCommonStore();

  if (!binderService) {
    throw new Error('Service is undefined.');
  }

  const SCHEDULED_CATEGORY = 'Scheduled';
  const CSR_CATEGORY = 'CSR';
  const CSR_CATEGORY_DESC = 'Court Summary';

  const selectedItems = ref<civilDocumentType[]>([]);
  const selectedBinderItems = ref<civilDocumentType[]>([]);
  const showActionbar = computed<boolean>(() => selectedItems.value.length > 1);
  const enableViewTogether = computed<boolean>(
    () => selectedItems.value.filter((d) => d.imageId).length > 1
  );
  const scheduledDocuments = props.documents.filter(
    (doc) => doc.nextAppearanceDt
  );
  const selectedCategory = ref<string>(
    scheduledDocuments.length > 0 ? SCHEDULED_CATEGORY : ''
  );
  const isBinderLoading = ref(true);
  const rolesLoading = ref(false);
  const roles = ref<LookupCode[]>();
  const currentBinder = ref<Binder>();
  const courtClassCdStyle = getCourtClassStyle(props.courtClassCd);

  const headers: DataTableHeader[] = [
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
      value: (item: civilDocumentType) => formatDateToDDMMMYYYY(item.filedDt),
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
    {
      title: 'ACTIONS',
      key: 'binderMenu',
      align: 'end' as const,
      sortable: false,
    },
  ];
  const labels = {
    ['physicalFileId']: props.fileId,
    ['courtClassCd']: props.courtClassCd,
    ['judgeId']: commonStore.userInfo?.userId,
  };

  const documentCategories = ref<string[]>(
    (scheduledDocuments.length > 0 ? [SCHEDULED_CATEGORY] : []).concat(
      Array.from(
        new Set(
          props.documents
            .filter((d) => d.category)
            .map((doc) =>
              doc.category === CSR_CATEGORY ? CSR_CATEGORY_DESC : doc.category
            )
        )
      )
    )
  );

  const filterByCategory = (item: civilDocumentType) => {
    const category = selectedCategory.value?.toLowerCase();
    if (!category) {
      return true;
    }

    if (category === SCHEDULED_CATEGORY.toLowerCase()) {
      return !!item.nextAppearanceDt;
    }

    if (category === CSR_CATEGORY_DESC.toLowerCase()) {
      return item.category === CSR_CATEGORY;
    }

    return item.category?.toLowerCase() === category;
  };

  const filteredDocuments = computed(() =>
    props.documents.filter(filterByCategory)
  );

  const binderDocuments = computed(() => {
    const binderDocumentIds = currentBinder.value?.documents
      .sort((d) => d.order)
      .map((d) => d.documentId);

    if (!binderDocumentIds || binderDocumentIds.length === 0) {
      return [];
    }

    const documentsMaps = new Map(
      props.documents.map((d) => [d.civilDocumentId, d])
    );
    return binderDocumentIds
      .map((id) => documentsMaps.get(id))
      .filter(
        (item): item is (typeof props.documents)[number] => item !== undefined
      );
  });

  const categoryCount = (category: string): number => {
    if (category.toLowerCase() === SCHEDULED_CATEGORY.toLowerCase()) {
      return props.documents.filter((doc) => doc.nextAppearanceDt).length;
    }

    if (category.toLowerCase() === CSR_CATEGORY_DESC.toLowerCase()) {
      return props.documents.filter((doc) => doc.category === CSR_CATEGORY)
        .length;
    }

    return props.documents.filter(
      (doc) => doc.category?.toLowerCase() === category.toLowerCase()
    ).length;
  };

  onMounted(async () => {
    try {
      rolesLoading.value = true;
      isBinderLoading.value = true;
      const [rolesResp] = await Promise.all([getRoles(), loadBinder()]);
      roles.value = rolesResp;
    } catch (err: unknown) {
      console.error(err);
    } finally {
      rolesLoading.value = false;
      isBinderLoading.value = false;
    }
  });

  const openIndividualDocument = (data: civilDocumentType) =>
    shared.openDocumentsPdf(
      getCivilDocumentType(data),
      prepareCivilDocumentData(data)
    );

  // Todo, parts of these binder operation methods should be moved to a
  // shared binder space, that way the code is not repeated
  const openMergedDocuments = (items: civilDocumentType[] = []) => {
    const documents: {
      documentType: DocumentRequestType;
      documentData: DocumentData;
      groupKeyOne: string;
      groupKeyTwo: string;
      documentName: string;
    }[] = [];
    items
      .filter((item) => item.imageId)
      .forEach((item) => {
        const documentType =
          getCivilDocumentType(item) === CourtDocumentType.CSR
            ? DocumentRequestType.CourtSummary
            : DocumentRequestType.File;
        const documentData = prepareCivilDocumentData(item);
        documents.push({
          documentType,
          documentData,
          groupKeyOne: documentData.fileNumberText!,
          groupKeyTwo: '',
          documentName:
            item.documentTypeDescription +
            ' - ' +
            formatDateToDDMMMYYYY(item.filedDt),
        });
      });
    shared.openDocumentsPdfV2(documents);
  };

  const loadBinder = async () => {
    let getBindersResp: ApiResponse<Binder[]> | null = null;
    try {
      // Get binders associated to the current user. In Phase 1, we are supporting 1 binder per case per user.
      getBindersResp = await binderService.getBinders(labels);
    } catch (error) {
      console.error(`Error occured while retrieving user's binders: ${error}`);
    } finally {
      currentBinder.value =
        getBindersResp &&
        getBindersResp.succeeded &&
        getBindersResp.payload.length > 0
          ? getBindersResp.payload[0]
          : ({ id: null, labels, documents: [] } as Binder);
    }
  };

  const addDocumentToBinder = async (documentId: string) => {
    currentBinder.value?.documents.push({
      documentId,
      order: currentBinder.value?.documents.length,
    } as BinderDocument);

    await saveBinder();
  };

  const removeSelectedJudicialDocuments = async () => {
    if (!currentBinder.value?.documents) {
      return;
    }
    currentBinder.value.documents = currentBinder.value?.documents.filter(
      (d) =>
        !selectedBinderItems.value.find(
          (item) => item.civilDocumentId === d.documentId
        )
    );
    selectedBinderItems.value = [];
    await saveBinder();
  };

  const removeDocumentFromBinder = async (documentId: string) => {
    if (!currentBinder.value?.documents) {
      return;
    }
    currentBinder.value.documents = currentBinder.value?.documents.filter(
      (d) => d.documentId !== documentId
    );

    await saveBinder();
  };

  const deleteCurrentBinder = async () => {
    if (!currentBinder.value?.id) {
      return;
    }
    isBinderLoading.value = true;
    await binderService.deleteBinder(currentBinder.value.id);
    currentBinder.value = { id: null, labels, documents: [] } as Binder;
    isBinderLoading.value = false;
  };

  const saveBinder = async () => {
    let savedBinderResult: ApiResponse<Binder> | null = null;
    try {
      isBinderLoading.value = true;
      if (currentBinder.value?.id) {
        savedBinderResult = await binderService.updateBinder(
          currentBinder.value!
        );
      } else {
        savedBinderResult = await binderService.addBinder(currentBinder.value!);
      }
    } catch (error) {
      console.error(error);
    } finally {
      isBinderLoading.value = false;
      if (savedBinderResult && savedBinderResult.succeeded) {
        currentBinder.value = savedBinderResult.payload;
      } else {
        console.error('Something went wrong when saving the binder.');
      }
    }
  };

  const addSelectedItemsToBinder = async () => {
    const excludedIds = new Set(
      currentBinder.value?.documents.map((d) => d.documentId)
    );

    // Exclude documents that are already in the binder
    const newDocuments = selectedItems.value.filter(
      (d) => !excludedIds.has(d.civilDocumentId)
    );

    if (newDocuments.length === 0) {
      selectedItems.value = [];
      return;
    }

    newDocuments.forEach((d) => {
      currentBinder.value?.documents.push({
        documentId: d.civilDocumentId,
        order: currentBinder.value?.documents.length,
      } as BinderDocument);
    });

    selectedItems.value = [];
    await saveBinder();
  };

  const handleReordering = (documentData) => {
    if (!currentBinder.value?.documents) {
      return;
    }
    const docs = currentBinder.value.documents;
    const [moved] = docs.splice(documentData.oldIndex, 1);
    docs.splice(documentData.newIndex, 0, moved);
    // Update order property for each document
    docs.forEach((doc, idx) => (doc.order = idx));
    return saveBinder();
  };
</script>

<style>
  .v-chip {
    cursor: default;
  }

  .v-alert {
    font-size: 0.875rem;
  }

  .small-claims .v-alert__border {
    opacity: 100%;
    border-color: var(--bg-purple-500) !important;
  }

  .family .v-alert__border {
    opacity: 100%;
    border-color: var(--bg-green-500) !important;
  }
</style>
