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
      >
        <template v-slot:item="{ props: itemProps, item }">
          <v-list-item
            v-bind="itemProps"
            :title="item.title + ' (' + typeCount(item.raw) + ')'"
          ></v-list-item>
        </template>
      </v-select>
    </v-col>
  </v-row>

  <JudicialBinder
    :courtClassCdStyle
    :binderDocuments
    :isBinderLoading
    :rolesLoading
    :roles="roles ?? []"
    :openIndividualDocument
    :removeDocumentFromBinder
    :deleteBinder="deleteCurrentBinder"
    :baseHeaders="headers"
    :selectedItems="selectedBinderItems"
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

  <ActionBar v-if="showActionbar" :selected="selectedItems">
    <template #default>
      <v-btn
        size="large"
        class="mx-2"
        :prepend-icon="mdiFileDocumentMultipleOutline"
        style="letter-spacing: 0.001rem"
        @click="openMergedDocuments()"
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
  } from '@/types/shared';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { getCourtClassStyle, getRoles } from '@/utils/utils';
  import { mdiFileDocumentMultipleOutline, mdiNotebookOutline } from '@mdi/js';
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

  const selectedItems = ref<civilDocumentType[]>([]);
  const selectedBinderItems = ref<civilDocumentType[]>([]);
  const showActionbar = computed<boolean>(() => selectedItems.value.length > 1);
  const enableViewTogether = computed<boolean>(
    () => selectedItems.value.filter((d) => d.imageId).length > 1
  );
  const selectedType = ref<string>();
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

  const documentTypes = ref<any[]>([
    ...new Map(
      props.documents.map((doc) => [
        doc.documentTypeCd,
        { title: doc.documentTypeDescription, value: doc.documentTypeCd },
      ])
    ).values(),
  ]);
  const filterByType = (item: civilDocumentType) =>
    !selectedType.value ||
    item.documentTypeCd?.toLowerCase() === selectedType.value?.toLowerCase();

  const filteredDocuments = computed(() =>
    props.documents.filter(filterByType)
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
    const filteredAndSorted = binderDocumentIds
      .map((id) => documentsMaps.get(id))
      .filter(
        (item): item is (typeof props.documents)[number] => item !== undefined
      )
      .filter(filterByType);
    return filteredAndSorted;
  });

  const typeCount = (type: any): number =>
    props.documents.filter((doc) => doc.documentTypeCd === type.value).length;

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
  const openMergedDocuments = () => {
    const documents: [CourtDocumentType, DocumentData][] = [];
    selectedItems.value
      .filter((item) => item.imageId)
      .forEach((item) => {
        const documentType = getCivilDocumentType(item);
        const documentData = prepareCivilDocumentData(item);
        documents.push([documentType, documentData]);
      });
    shared.openMergedDocumentsPdf(documents);
  };

  const loadBinder = async () => {
    // Get binders associated to the current user. In Phase 1, we are supporting 1 binder per case per user.
    const binders = await binderService.getBinders(labels);

    currentBinder.value =
      binders && binders.payload.length > 0
        ? binders.payload[0]
        : ({ id: null, labels, documents: [] } as Binder);
  };

  const addDocumentToBinder = async (documentId: string) => {
    currentBinder.value?.documents.push({
      documentId,
      order: currentBinder.value?.documents.length,
    } as BinderDocument);

    await saveBinder();
  };

  const removeDocumentFromBinder = async (documentId: string) => {
    if(!currentBinder.value?.documents) {
      return;
    }
    currentBinder.value.documents = currentBinder.value?.documents.filter(
      (d) => d.documentId !== documentId
    );

    await saveBinder();
  };

  const deleteCurrentBinder = async () => {
    if(!currentBinder.value?.id) {
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

  const handleReordering =  (documentData) => {
    if(!currentBinder.value?.documents){
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
