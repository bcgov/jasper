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

<script setup>
  import { usePDFViewerStore } from '@/stores';
  import { onMounted, onUnmounted, ref, useTemplateRef } from 'vue';

  const pdfStore = usePDFViewerStore();
  const containerRef = useTemplateRef('pdf-container');
  const loading = ref(false);
  const emptyStore = ref(false);
  const configuration = {
    initialViewState: new NutrientViewer.ViewState({
      sidebarMode: NutrientViewer.SidebarMode.THUMBNAILS,
    }),
  };

  const loadNutrient = () => {
    loading.value = true;
    emptyStore.value = false;

    if (!pdfStore.documentUrls.length) {
      loading.value = false;
      emptyStore.value = true;
      return;
    }
    return pdfStore.documentUrls.length === 1 ? loadSingle() : loadMultiple();
  };

  const loadMultiple = async () => {
    const instance = await NutrientViewer.load({
      ...configuration,
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

    await NutrientViewer.load({
      ...configuration,
      container: containerRef.value,
      document: mergedDocument,
    });
  };

  const loadSingle = async () => {
    const documentBlob = await fetch(pdfStore.documentUrls[0]).then((result) =>
      result.blob()
    );
    const documentBuffer = await documentBlob.arrayBuffer();
    // We set our own loader to false, nutrient's internal loader can take it from here
    loading.value = false;

    await NutrientViewer.load({
      ...configuration,
      container: containerRef.value,
      document: documentBuffer,
    });
  };

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
    height: 90vh;
  }
  .v-skeleton-loader {
    height: 100%;
  }
</style>
