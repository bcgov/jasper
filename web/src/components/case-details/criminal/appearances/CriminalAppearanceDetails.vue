<template>
  <v-tabs v-model="tab" align-tabs="start" slider-color="primary">
    <v-tab value="charges">Charges</v-tab>
    <v-tab value="methods">Appearance Methods</v-tab>
    <v-tab value="counsel" v-if="isPast">Current Counsel</v-tab>
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
        <v-tabs-window-item value="charges">
          <AppearanceCharges :charges="details.charges" />
        </v-tabs-window-item>
        <v-tabs-window-item value="methods">
          <AppearanceMethods :appearanceMethods="details.appearanceMethods" />
        </v-tabs-window-item>
        <v-tabs-window-item value="counsel">
          <AppearanceCounsel v-if="isPast" />
        </v-tabs-window-item>
      </v-skeleton-loader>
    </v-tabs-window>
  </v-card-text>
</template>

<script setup lang="ts">
  import { FilesService } from '@/services/FilesService';
  import { CriminalAppearanceDetails } from '@/types/criminal/jsonTypes';
  import { inject, onMounted, ref } from 'vue';
  import AppearanceCharges from './AppearanceCharges.vue';
  import AppearanceCounsel from './AppearanceCounsel.vue';
  import AppearanceMethods from './AppearanceMethods.vue';

  const props = defineProps<{
    fileId: string;
    appearanceId: string;
    partId: string;
    isPast: boolean;
  }>();

  const filesService = inject<FilesService>('filesService');
  const tab = ref('charges');
  const details = ref<CriminalAppearanceDetails>(
    {} as CriminalAppearanceDetails
  );
  const loading = ref(false);
  if (!filesService) {
    throw new Error('Files service is undefined.');
  }

  onMounted(async () => {
    loading.value = true;
    details.value = await filesService.criminalAppearanceDetails(
      props.fileId,
      props.appearanceId,
      props.partId
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
