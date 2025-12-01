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
        <JudicialBinder
          :documents="binderDocuments"
          :fileId
          :fileNumberTxt="documentDetails.fileNumberTxt"
          :courtLevel="documentDetails.courtLevelCd"
          :agencyId="documentDetails.agencyId"
        />
      </v-tabs-window-item>

      <v-tabs-window-item value="parties">
        <ScheduledParties :file-id="fileId" :appearance-id="appearanceId" />
      </v-tabs-window-item>
      <v-tabs-window-item
        v-if="methods.appearanceMethod?.length"
        value="methods"
      >
        <CivilAppearanceMethods :appearanceMethod="methods.appearanceMethod" />
      </v-tabs-window-item>
    </v-tabs-window>
  </v-card-text>
</template>

<script setup lang="ts">
  import CivilAppearanceMethods from '@/components/case-details/civil/appearances/CivilAppearanceMethods.vue';
  import ScheduledDocuments from '@/components/case-details/civil/appearances/ScheduledDocuments.vue';
  import ScheduledParties from '@/components/case-details/civil/appearances/ScheduledParties.vue';
  import { FilesService } from '@/services';
  import {
    CivilAppearanceDetailDocuments,
    CivilAppearanceDetailMethods,
    civilDocumentType,
  } from '@/types/civil/jsonTypes';
  import { inject, onMounted, ref } from 'vue';
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

  const filesService = inject<FilesService>('filesService');

  if (!filesService) {
    throw new Error('Service is undefined.');
  }

  const tab = ref('documents');
  const documentDetails = ref<CivilAppearanceDetailDocuments>(
    {} as CivilAppearanceDetailDocuments
  );
  const methods = ref<CivilAppearanceDetailMethods>(
    {} as CivilAppearanceDetailMethods
  );
  const documentsLoading = ref(false);
  const binderLoading = ref(false);
  const binderDocuments = ref<civilDocumentType[]>([]);

  onMounted(async () => {
    try {
      await Promise.all([
        loadBinderDocuments(),
        loadMethods(),
        loadDocuments(),
      ]);
    } catch (error) {
      console.error(
        'Error occurred while retrieving appearance details:',
        error
      );
    }
  });

  const loadMethods = async () => {
    try {
      const methodResponse = await filesService.civilAppearanceMethods(
        props.fileId,
        props.appearanceId
      );
      methods.value = methodResponse;
    } catch (e) {
      console.error('Error occurred while retrieving appearance methods:', e);
    }
  };

  const loadDocuments = async () => {
    documentsLoading.value = true;
    const documentsResponse = await filesService.civilAppearanceDocuments(
      props.fileId,
      props.appearanceId
    );

    documentDetails.value = documentsResponse;
    documentsLoading.value = false;
  };

  const loadBinderDocuments = async () => {
    binderLoading.value = true;
    try {
      binderDocuments.value = await filesService.civilBinderDocuments(
        props.fileId
      );
    } catch (error) {
      console.error(`Error occured while retrieving user's binders: ${error}`);
    } finally {
      binderLoading.value = false;
    }
  };
</script>

<style scoped>
  .v-tabs {
    flex: 10;
  }
  .v-skeleton-loader {
    display: block;
  }
</style>
