<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <v-row class="py-12" v-if="emptyStore">
    <v-col>
      <p class="text-center mx-auto">No documents available to display.</p>
    </v-col>
  </v-row>

  <ReviewModal
    v-model="showReviewModal"
    :can-approve="canApprove"
    @approveOrder="approveOrder"
  />

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { onMounted, onUnmounted, ref, inject } from 'vue';
  import {
    mdiNotebookOutline,
    mdiFileDocumentArrowRightOutline,
  } from '@mdi/js';
  import { OrderService } from '@/services';
  import ReviewModal from './ReviewModal.vue';

  // Declare NutrientViewer global
  declare global {
    const NutrientViewer: any;
  }

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

    // Shows options related to reviewing order files
    showOrderReviewOptions?: boolean;

    approveOrder?(comments: string, pdfString: string): Promise<void>;
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
  const loading = ref(false);
  const emptyStore = ref(false);
  const showReviewModal = ref(false);
  const canApprove = ref<boolean>(false);

  const orderService = inject<OrderService>('orderService');
  if (!orderService) {
    throw new Error('Service(s) is undefined.');
  }

  let instance = {} as any;

  const configuration = {
    container: '.pdf-container',
    licenseKey: commonStore.appInfo?.nutrientFeLicenseKey ?? '',
  };

  async function hasImageAnnotation(pageIndex: number) {
    const annotations = await instance.getAnnotations(pageIndex);
    return annotations.filter((a) => a.contentType?.includes('image')).size > 0;
  }

  async function checkDocumentForAnnotations() {
    for (let i = 0; i < instance.totalPageCount; i++) {
      if (await hasImageAnnotation(i)) return true;
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
        type: 'custom',
        id: 'open-information',
        title: 'Supporting information',
        icon: `<svg><path d="${mdiNotebookOutline}"/></svg>`,
        onPress: () => {
          let firstPhysicalFileId: string | undefined;
          let isCriminal: boolean | undefined;
          Object.values(rawData).forEach((personDocuments) => {
            Object.values(personDocuments as any)
              .flat()
              .forEach((doc: any) => {
                if (doc?.physicalFileId) {
                  firstPhysicalFileId ??= doc.physicalFileId;
                }
                if (doc?.request?.data?.isCriminal !== undefined) {
                  isCriminal ??= doc.request.data.isCriminal;
                }
              });
          });

          window.open(
            `${isCriminal ? 'criminal-file/' : 'civil-file/'}${firstPhysicalFileId}`,
            'relatedCaseInfo'
          );
        },
      };

      const reviewItem = {
        type: 'custom',
        id: 'open-document-review',
        title: 'Open document review',
        icon: `<svg><path d="${mdiFileDocumentArrowRightOutline}"/></svg>`,
        onPress: () => {
          showReviewModal.value = true;
        },
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
      instance.setToolbarItems((items: any) => {
        if (props.strategy.showOrderReviewOptions) {
          items.push(openInfoItem, reviewItem);
        }
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
    const baseElement = {
      title: item.title,
      action:
        item.pageIndex !== undefined
          ? new NutrientViewer.Actions.GoToAction({ pageIndex: item.pageIndex })
          : undefined,
    };

    if (item.children?.length) {
      return new NutrientViewer.OutlineElement({
        ...baseElement,
        isExpanded: item.isExpanded ?? true,
        children: NutrientViewer.Immutable.List(
          item.children.map((child) => createOutlineElement(child))
        ),
      });
    }

    return new NutrientViewer.OutlineElement(baseElement);
  };

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  // const approveOrder = async (comments: string) => {
  //   showReviewModal.value = false;
  // };

   const approveOrder = async (comments: string) => {
    showReviewModal.value = false;
    
    // Check if strategy supports order approval
    if (props.strategy.approveOrder) {
      try {
        await props.strategy.approveOrder(comments);
        // Add success notification/handling here
      } catch (error) {
        console.error('Error approving order:', error);
        // Add error notification/handling here
      }
    }
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
