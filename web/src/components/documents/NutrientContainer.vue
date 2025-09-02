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
  import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
  import { FilesService } from '@/services/FilesService';
  import { usePDFViewerStore } from '@/stores';
  import { StoreDocument } from '@/stores/PDFViewerStore';
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
  let documentResponse: GeneratePdfResponse | null = null;
  let pageIndex = 0;

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
    const groupedDocs = pdfStore.groupedDocuments;
    const allDocs: StoreDocument[] = [];
    Object.values(groupedDocs).forEach(
      (userGroup: Record<string, StoreDocument[]>) => {
        Object.values(userGroup).forEach((docs) => {
          allDocs.push(...(docs as StoreDocument[]));
        });
      }
    );

    documentResponse = await filesService.generatePdf(
      allDocs.map((doc) => doc.request)
    );
    loading.value = false;

    let instance = await NutrientViewer.load({
      ...configuration,
      document: `data:application/pdf;base64,${documentResponse.base64Pdf}`,
    });
    configureOutline(instance);
  };

  const configureOutline = (instance: any) => {
    const outline = NutrientViewer.Immutable.List(
      Object.entries(pdfStore.groupedDocuments).map(([groupKey, userGroup]) =>
        makeCaseElement(groupKey, userGroup)
      )
    );
    instance.setDocumentOutline(outline);
  };

  const makeCaseElement = (
    groupKey: string,
    userGroup: Record<string, StoreDocument[]>
  ) => {
    return new NutrientViewer.OutlineElement({
      title: groupKey,
      children: NutrientViewer.Immutable.List(
        Object.entries(userGroup).map(([name, docs]) =>
          makeMemberElement(name || 'Documents', docs)
        )
      ),
    });
  };

  const makeMemberElement = (memberName: string, docs: StoreDocument[]) => {
    return new NutrientViewer.OutlineElement({
      title: memberName,
      children: NutrientViewer.Immutable.List(
        docs.map((doc) => makeDocElement(doc))
      ),
    });
  };

  const makeDocElement = (doc: StoreDocument) => {
    return new NutrientViewer.OutlineElement({
      title: doc.documentName,
      action: new NutrientViewer.Actions.GoToAction({
        pageIndex: documentResponse?.pageRanges?.[pageIndex++]?.start,
      }),
    });
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
