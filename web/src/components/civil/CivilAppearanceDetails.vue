<template>
  <v-tabs v-model="tab" align-tabs="start" slider-color="primary">
    <v-tab value="documents">Scheduled Documents</v-tab>
    <v-tab
      data-testid="binder-tab"
      v-if="showBinder"
      :disabled="loading || !details || details.binderDocuments?.length === 0"
      value="binder"
      >Judicial Binder</v-tab
    >
    <v-tab value="parties">Scheduled Parties</v-tab>
    <v-tab value="methods" v-if="details.appearanceMethod?.length"
      >Appearance Methods</v-tab
    >
  </v-tabs>

  <v-card-text>
    <v-tabs-window v-model="tab">
      <v-skeleton-loader
        class="my-0"
        type="table"
        :height="200"
        color="var(--bg-gray-200)"
        :loading="loading"
      >
        <v-tabs-window-item value="documents">
          <ScheduledDocuments
            :documents="details.document"
            :fileId
            :fileNumberTxt="details.fileNumberTxt"
            :courtLevel="details.courtLevelCd"
            :agencyId="details.agencyId"
          />
        </v-tabs-window-item>
        <v-tabs-window-item v-if="showBinder" value="binder">
          <JudicialBinder :documents="details.binderDocuments"
            :fileId
            :fileNumberTxt="details.fileNumberTxt"
            :courtLevel="details.courtLevelCd"
            :agencyId="details.agencyId" />
        </v-tabs-window-item>

        <v-tabs-window-item value="parties">
          <ScheduledParties :parties="details.party" />
        </v-tabs-window-item>
        <v-tabs-window-item
          v-if="details.appearanceMethod?.length"
          value="methods"
        >
          <CivilAppearanceMethods
            :appearanceMethod="details.appearanceMethod"
          />
        </v-tabs-window-item>
      </v-skeleton-loader>
    </v-tabs-window>
  </v-card-text>
</template>

<script setup lang="ts">
  import CivilAppearanceMethods from '@/components/case-details/civil/appearances/CivilAppearanceMethods.vue';
  import ScheduledDocuments from '@/components/case-details/civil/appearances/ScheduledDocuments.vue';
  import ScheduledParties from '@/components/case-details/civil/appearances/ScheduledParties.vue';
  import { FilesService } from '@/services';
  import { CivilAppearanceDetails } from '@/types/civil/jsonTypes';
  import { inject, onMounted, ref } from 'vue';
  import JudicialBinder from '../case-details/civil/appearances/JudicialBinder.vue';
  const props = withDefaults(
    defineProps<{
      fileId: string;
      appearanceId: string;
      showBinder?: boolean;
    }>(),
    {
      showBinder: false,
    }
  );

  const filesService = inject<FilesService>('filesService');
  const tab = ref('documents');
  const details = ref<CivilAppearanceDetails>({} as CivilAppearanceDetails);
  const loading = ref(false);

  if (!filesService) {
    throw new Error('Files service is undefined.');
  }

  onMounted(async () => {
    loading.value = true;
    try {
      const response = await filesService.civilAppearanceDetails(
        props.fileId,
        props.appearanceId,
        true
      );
      details.value = response;
    } catch (error) {
      console.error(
        'Error occurred while retrieving appearance details:',
        error
      );
      details.value = {} as CivilAppearanceDetails;
    } finally {
      loading.value = false;
    }
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
