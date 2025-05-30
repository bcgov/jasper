<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup>
  import { usePDFViewerStore } from '@/stores';
import { onMounted, onUnmounted, ref, useTemplateRef } from 'vue';

  const pdfStore = usePDFViewerStore();
  const containerRef = useTemplateRef('pdf-container');
  const loading = ref(false);
  const configuration = {
    initialViewState: new NutrientViewer.ViewState({
      sidebarMode: NutrientViewer.SidebarMode.THUMBNAILS,
    }),
  };

  async function loadNutrient() {
    loading.value = true;

    const instance = await NutrientViewer.load({
      container: containerRef.value,
      document: pdfStore.documentUrls[0],
      headless: true,
    });

    // We skip the first index since we used it to load the nutrient viewer.
    const documentBlobs = await Promise.all(
      pdfStore.documentUrls
        .slice(1)
        .map((url) => fetch(url).then((result) => result.blob()))
    );
    // We need to keep track of the target page index for the imported document.
    let afterPageIndex = instance.totalPageCount - 1;

    const mergeDocumentOperations = await Promise.all(
      documentBlobs.map(async (blob, idx) => {
        const operation = {
          type: 'importDocument',
          afterPageIndex,
          treatImportedDocumentAsOnePage: false,
          document: blob,
        };
        // Retrieve page count of the merged document to calculate page index
        // of the next imported document. This can be skipped for the last
        // operation since we don't care how large the last document is.
        if (idx < documentBlobs.length - 1) {
          const documentInstance = await NutrientViewer.load({
            ...configuration,
            document: await blob.arrayBuffer(),
            headless: true,
          });
          afterPageIndex += documentInstance.totalPageCount - 1;
          NutrientViewer.unload(documentInstance);
        }
        return operation;
      })
    );

    const mergedDocument = await instance.exportPDFWithOperations(
      mergeDocumentOperations
    );
    // We set our own loader to false, nutrient's internal loader can take it from here
    loading.value = false;

    NutrientViewer.load({
      ...configuration,
      container: containerRef.value,
      document: mergedDocument,
    });
  }

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    const container = containerRef.value;

    if (container && NutrientViewer) {
      NutrientViewer.unload(container);
    }
    pdfStore.clearUrls();
  });
</script>

<style scoped>
  .pdf-container {
    height: 100vh;
  }
  .v-skeleton-loader {
    height: 100%;
  }
</style>
