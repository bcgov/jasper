<template>
  <v-tabs v-model="tab" align-tabs="start" slider-color="primary">
    <v-tab value="documents">Scheduled Documents</v-tab>
    <v-tab
      data-testid="binder-tab"
      v-if="showBinder"
      :disabled="binderLoading || binderDocuments.length === 0"
      value="binder"
      >
      <v-progress-circular
        v-if="binderLoading"
          indeterminate
          size="18"
          width="2"
          color="primary"
          class="mr-2 align-middle"
        />
      Judicial Binder</v-tab
    >
    <v-tab value="parties">Scheduled Parties</v-tab>
    <v-tab value="methods" v-if="methods.appearanceMethod?.length"
      >Appearance Methods</v-tab
    >
  </v-tabs>

  <v-card-text>
    <v-tabs-window v-model="tab">
        <v-tabs-window-item value="documents">
          <v-skeleton-loader
            class="my-0"
            type="table"
            :height="200"
            color="var(--bg-gray-200)"
            :loading="documentsLoading"
          >
        <ScheduledDocuments
            :documents="documentDetails.document"
            :fileId
            :fileNumberTxt="documentDetails.fileNumberTxt"
            :courtLevel="documentDetails.courtLevelCd"
            :agencyId="documentDetails.agencyId"
          />
          </v-skeleton-loader>
        </v-tabs-window-item>
        <v-tabs-window-item v-if="showBinder" value="binder">
          <JudicialBinder :documents="binderDocuments"
            :fileId
            :fileNumberTxt="documentDetails.fileNumberTxt"
            :courtLevel="documentDetails.courtLevelCd"
            :agencyId="documentDetails.agencyId" />
        </v-tabs-window-item>

        <v-tabs-window-item value="parties">
          <ScheduledParties :file-id="fileId" :appearance-id="appearanceId" />
        </v-tabs-window-item>
        <v-tabs-window-item
          v-if="methods.appearanceMethod?.length"
          value="methods"
        >
          <CivilAppearanceMethods
            :appearanceMethod="methods.appearanceMethod"
          />
        </v-tabs-window-item>
      
    </v-tabs-window>
  </v-card-text>
</template>

<script setup lang="ts">
  import CivilAppearanceMethods from '@/components/case-details/civil/appearances/CivilAppearanceMethods.vue';
  import ScheduledDocuments from '@/components/case-details/civil/appearances/ScheduledDocuments.vue';
  import ScheduledParties from '@/components/case-details/civil/appearances/ScheduledParties.vue';
  import { FilesService, BinderService } from '@/services';
  import { CivilAppearanceDetailDocuments, CivilAppearanceDetailMethods } from '@/types/civil/jsonTypes';
  import { inject, onMounted, ref, computed } from 'vue';
  import { useCommonStore } from '@/stores';
  import { ApiResponse } from '@/types/ApiResponse';
  import { Binder } from '@/types';
  import JudicialBinder from '../case-details/civil/appearances/JudicialBinder.vue';

  const props = withDefaults(
    defineProps<{
      fileId: string;
      appearanceId: string;
      showBinder?: boolean;
      courtClassCd: string;
    }>(),
    {
      showBinder: false,
    }
  );

  const binderService = inject<BinderService>('binderService');
  const filesService = inject<FilesService>('filesService');
  const commonStore = useCommonStore();

  if (!binderService || !filesService) {
    throw new Error('Service is undefined.');
  }
  
  const tab = ref('documents');
  const documentDetails = ref<CivilAppearanceDetailDocuments>({} as CivilAppearanceDetailDocuments);
  const methods = ref<CivilAppearanceDetailMethods>({} as CivilAppearanceDetailMethods);
  const documentsLoading = ref(false);
  const binderLoading = ref(false);
  const currentBinder = ref<Binder>();
  const labels = {
    ['physicalFileId']: props.fileId,
    ['courtClassCd']: props.courtClassCd,
    ['judgeId']: commonStore.userInfo?.userId,
  };

  if (!filesService) {
    throw new Error('Files service is undefined.');
  }

  onMounted(async () => {
    try {
      // Start fetching binders and methods in the background
      loadBinder();
      loadMethods();
      // Wait for documents to load as it is the main tab
      await loadDocuments();
    } catch (error) {
      console.error('Error occurred while retrieving appearance details:', error);
    }
  });

  const loadMethods = async () => {
    filesService.civilAppearanceMethods(
        props.fileId,
        props.appearanceId
      ).then((methodResponse) => {
        methods.value = methodResponse;
      }).catch((e) => {
      // Optionally handle error
      console.error('Error occurred while retrieving appearance methods:', e);
    });
  };

  const loadDocuments = async () => {
    documentsLoading.value = true;
    const documentsResponse = await filesService.civilAppearanceDocuments(
      props.fileId,
      props.appearanceId
    );
      
    documentDetails.value = documentsResponse;
    documentsLoading.value = false;
  }

  const loadBinder = async () => {
    binderLoading.value = true;
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
      binderLoading.value = false;
    }
  };

  const binderDocuments = computed(() => {
    const binderDocumentIds = currentBinder.value?.documents
      .sort((d) => d.order)
      .map((d) => d.documentId);

    if (!binderDocumentIds || binderDocumentIds.length === 0) {
      return [];
    }

    const documentsMaps = new Map(
      documentDetails.value.document.map((d) => [d.civilDocumentId, d])
    );
    return binderDocumentIds
      .map((id) => documentsMaps.get(id))
      .filter(
        (item): item is (typeof documentDetails.value.document)[number] => item !== undefined
      );
  });
</script>

<style scoped>
  .v-tabs {
    flex: 10;
  }
  .v-skeleton-loader {
    display: block;
  }
</style>
