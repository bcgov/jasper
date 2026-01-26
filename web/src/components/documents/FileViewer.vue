<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <v-row class="py-12" v-if="emptyStore">
    <v-col>
      <p class="text-center mx-auto">No documents available to display.</p>
    </v-col>
  </v-row>

  <ReviewModal v-model="showReviewModal" :can-approve="canApprove" @approveOrder="(comments: string) => approveOrder(comments)" />

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { onMounted, onUnmounted, ref, inject } from 'vue';
  import { mdiNotebookOutline, mdiFileDocumentArrowRightOutline } from '@mdi/js';
  import { useCourtFileSearchStore } from '@/stores';
  import { KeyValueInfo } from '@/types/common';
  import { OrderService } from '@/services';
  import { OrderReview } from '@/types/OrderReview';
  import ReviewModal from './ReviewModal.vue';
  //import NutrientViewer from '@nutrient-sdk/viewer';

  // Declare NutrientViewer global
  // declare global {
  //   const NutrientViewer: any;
  // }

  // Base interfaces for the strategy pattern
  export interface PDFViewerStrategy<
    TRawData = any,
    TProcessedData = any,
    TAPIResponse = any,
  > {
    // Check if there's data available to process
    hasData(): boolean;

    // Get raw data from the store/source
    getRawData(): TRawData;

    // Process raw data into format needed for API call
    processDataForAPI(rawData: TRawData): TProcessedData;

    // Make the API call to generate PDF
    generatePDF(processedData: TProcessedData): Promise<TAPIResponse>;

    // Extract base64 PDF from API response
    extractBase64PDF(apiResponse: TAPIResponse): string;

    // Extract page ranges from API response (optional)
    extractPageRanges(
      apiResponse: TAPIResponse
    ): Array<{ start: number; end?: number }> | undefined;

    // Create outline structure from raw data and API response
    createOutline(rawData: TRawData, apiResponse: TAPIResponse): OutlineItem[];

    // Cleanup function
    cleanup(): void;

    // Shows order review options
    showOrderReviewOptions?: boolean;
  }

  export interface OutlineItem {
    title: string;
    pageIndex?: number;
    children?: OutlineItem[];
    isExpanded?: boolean;
    action?: any;
  }

  // Props for the generic component
  interface Props<TStrategy extends PDFViewerStrategy = PDFViewerStrategy> {
    strategy: TStrategy;
  }

  const props = defineProps<Props>();
  const commonStore = useCommonStore();
  const courtFileSearchStore = useCourtFileSearchStore();
  const loading = ref(false);
  const emptyStore = ref(false);
  const showReviewModal = ref(false);
  const canApprove = ref<boolean>(false);

  const orderService = inject<OrderService>('orderService');
  if (!orderService) {
    throw new Error('Service(s) is undefined.');
  }
  
  let instance = {} as any; // NutrientViewer.Instance

  const configuration = {
    container: '.pdf-container',
    licenseKey: commonStore.appInfo?.nutrientFeLicenseKey ?? '',
  };

  async function hasImageAnnotation(pageIndex: number) {
    const annotations = await instance.getAnnotations(pageIndex);
    // As long as it has one image signature, we consider the document signed
    const imageAnnotations = annotations.filter(annotation => 
      annotation.contentType?.includes('image')
    );
    return imageAnnotations.size > 0;
  }

  async function checkDocumentForAnnotations() {
    for (let i = 0; i < instance.totalPageCount; i++) {
      if (await hasImageAnnotation(i)) {
        return true;
      }
    }
    return false;
  }

  async function updateCanApprove() {
    canApprove.value = await checkDocumentForAnnotations();
  }

  const loadNutrient = async () => {
    loading.value = true;
    emptyStore.value = false;

    if (!props.strategy.hasData()) {
      loading.value = false;
      emptyStore.value = true;
      return;
    }

    try {
      // Follow the strategy pattern workflow
      const rawData = props.strategy.getRawData();
      const processedData = props.strategy.processDataForAPI(rawData);
      const apiResponse = await props.strategy.generatePDF(processedData);

      loading.value = false;

      // Create outline and load PDF viewer
      const outline = props.strategy.createOutline(rawData, apiResponse);
      const base64Pdf = props.strategy.extractBase64PDF(apiResponse);

      const nutrientOutline = createNutrientOutline(outline);

      const openInfoItem = {
        type: "custom",
        id: "open-information",
        title: "Supporting information",
        icon: `<svg><path d="${mdiNotebookOutline}"/></svg>`,
        onPress: () => {
          console.log(rawData);
          // Extract all groupKeyOne values and their corresponding physicalFileIds
          const files: KeyValueInfo[] = [];
          let firstPhysicalFileId: string | undefined;

          Object.values(rawData).forEach(personDocuments => {
            Object.values(personDocuments as any).flat().forEach((doc: any) => {
              if (doc?.groupKeyOne && doc?.physicalFileId) {
                files.push({
                  key: doc.physicalFileId,
                  value: doc.groupKeyOne,
                });
                firstPhysicalFileId ??= doc.physicalFileId;
              }
            });
          });
          
          courtFileSearchStore.addFilesForViewing({
            searchCriteria: {},
            searchResults: [],
            files,
          });

          window.open(`criminal-file/${firstPhysicalFileId}`, 'relatedCaseInfo');
        }
      };

      const reviewItem = {
        type: "custom",
        id: "open-document-review",
        title: "Open document review",
        icon: `<svg><path d="${mdiFileDocumentArrowRightOutline}"/></svg>`,
        onPress: () => {
          showReviewModal.value = true;
        }
      };

      instance = await NutrientViewer.load({
        ...configuration,
        document: `data:application/pdf;base64,${base64Pdf}`,
      });

      instance.setDocumentOutline(nutrientOutline);
      instance.setViewState((viewState) =>
        viewState.set(
          'sidebarMode',
          NutrientViewer.SidebarMode.DOCUMENT_OUTLINE
        )
      );
      instance.setToolbarItems((items) => {
        props.strategy.showOrderReviewOptions ? items.push(openInfoItem) : null;
        props.strategy.showOrderReviewOptions ? items.push(reviewItem) : null;
        return items;
      });

      // Listen for annotation changes to update canApprove
      instance.addEventListener('annotations.create', updateCanApprove);
      instance.addEventListener('annotations.update', updateCanApprove);
      instance.addEventListener('annotations.delete', updateCanApprove);

      // Check if document can be approved initially
      await updateCanApprove();
    } catch (error) {
      console.error('Error loading PDF:', error);
      loading.value = false;
      emptyStore.value = true;
    }
  };



  const createNutrientOutline = (outlineData: OutlineItem[]): any => {
    return NutrientViewer.Immutable.List(
      outlineData.map((item) => createOutlineElement(item))
    );
  };

  const createOutlineElement = (item: OutlineItem): any => {
    if (item.children && item.children.length > 0) {
      // It's a group
      return new NutrientViewer.OutlineElement({
        title: item.title,
        isExpanded: item.isExpanded ?? true,
        children: NutrientViewer.Immutable.List(
          item.children.map((child) => createOutlineElement(child))
        ),
        action:
          item.pageIndex !== undefined
            ? new NutrientViewer.Actions.GoToAction({
                pageIndex: item.pageIndex,
              })
            : undefined,
      });
    } else {
      // It's a document
      return new NutrientViewer.OutlineElement({
        title: item.title,
        action:
          item.pageIndex !== undefined
            ? new NutrientViewer.Actions.GoToAction({
                pageIndex: item.pageIndex,
              })
            : undefined,
      });
    }
  };

  const approveOrder = async (comments: string) => {
    try {
      const arrayBuffer = await instance.exportPDF();
      const blob = new Blob([arrayBuffer], { type: "application/pdf" });
      
      // Extract physicalFileId from raw data
      const rawData = props.strategy.getRawData();
      let physicalFileId: string | undefined;
      
      Object.values(rawData).forEach(personDocuments => {
        Object.values(personDocuments as any).flat().forEach((doc: any) => {
          if (doc?.physicalFileId) {
            physicalFileId ??= doc.physicalFileId;
          }
        });
      });
      
      const review: OrderReview = {
        signed: true,
        comments: comments,
        documentData: btoa(await blob.text()), // base64 representation
        status: "Approved"
      }
      var response = await orderService.review(physicalFileId, review);

      if (!response.ok) {
        throw new Error(`Upload failed: ${response.statusText}`);
      }
      console.log("PDF uploaded successfully");
    } catch (error) {
      console.error("Failed to save PDF to server:", error.message);
    }
    showReviewModal.value = false;
  };

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    if (NutrientViewer) {
      NutrientViewer.unload('.pdf-container');
    }
    if (props.strategy.cleanup) {
      props.strategy.cleanup();
    }
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
