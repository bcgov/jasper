<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <v-row class="py-12" v-if="emptyStore">
    <v-col>
      <p class="text-center mx-auto">No documents available to display.</p>
    </v-col>
  </v-row>

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import { FilesService } from '@/services/FilesService';
  import { usePDFViewerStore } from '@/stores';
  import { inject, onMounted, onUnmounted, ref } from 'vue';

  const pdfStore = usePDFViewerStore();
  const filesService = inject<FilesService>('filesService');
  if (!filesService) {
    throw new Error('HttpService is not available!');
  }
  const loading = ref(false);
  const emptyStore = ref(false);
  const configuration = {
    initialViewState: new NutrientViewer.ViewState({
      sidebarMode: NutrientViewer.SidebarMode.DOCUMENT_OUTLINE,
    }),
    container: '.pdf-container',
  };

  const loadNutrient = async () => {
    loading.value = true;
    emptyStore.value = false;

    if (!pdfStore.documents.length) {
      loading.value = false;
      emptyStore.value = true;
      return;
    }
    return loadMultiple();
  };

  const loadMultiple = async () => {
    const documentResponse = await filesService.generatePdf(pdfStore.documents);
    loading.value = false;

    await NutrientViewer.load({
      ...configuration,
      document: `data:application/pdf;base64,${documentResponse.base64Pdf}`,
    });
    // Todo - Render outline from page ranges
  };

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    if (NutrientViewer) {
      NutrientViewer.unload('.pdf-container');
    }

    pdfStore.clearDocuments();
  });
</script>

<style scoped>
  .pdf-container {
    height: 90vh;
  }
  .v-skeleton-loader {
    height: 100%;
  }
</style>
