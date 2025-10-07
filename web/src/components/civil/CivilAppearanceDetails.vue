<template>
  <v-tabs v-model="tab" align-tabs="start" slider-color="primary">
    <v-tab value="documents">Scheduled Documents</v-tab>
    <v-tab disabled value="binder">Judicial Binder</v-tab>
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
            :agencyId="details.agencyId" />
        </v-tabs-window-item>
        <v-tabs-window-item value="binder"> Binder </v-tabs-window-item>

        <v-tabs-window-item value="parties">
          <ScheduledParties :parties="details.party" />
        </v-tabs-window-item>
        <v-tabs-window-item
          v-if="details.appearanceMethod?.length"
          value="methods"
        >
          <CivilAppearanceMethods :appearanceMethod="details.appearanceMethod" />
        </v-tabs-window-item>
      </v-skeleton-loader>
    </v-tabs-window>
  </v-card-text>
</template>

<script setup lang="ts">
  import CivilAppearanceMethods from '@/components/case-details/civil/appearances/CivilAppearanceMethods.vue';
  import ScheduledDocuments from '@/components/case-details/civil/appearances/ScheduledDocuments.vue';
  import ScheduledParties from '@/components/case-details/civil/appearances/ScheduledParties.vue';
  import { FilesService } from '@/services/FilesService';
  import { CivilAppearanceDetails } from '@/types/civil/jsonTypes';
  import { inject, onMounted, ref } from 'vue';

  const props = defineProps<{
    fileId: string;
    appearanceId: string;
  }>();

  const filesService = inject<FilesService>('filesService');
  const tab = ref('documents');
  const details = ref<CivilAppearanceDetails>({} as CivilAppearanceDetails);
  const loading = ref(false);
  if (!filesService) {
    throw new Error('Files service is undefined.');
  }

  onMounted(async () => {
    loading.value = true;
    details.value = await filesService.civilAppearanceDetails(
      props.fileId,
      props.appearanceId
    );
    loading.value = false;
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
