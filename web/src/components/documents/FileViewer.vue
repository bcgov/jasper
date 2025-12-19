<template>
  <v-progress-linear v-if="loading" indeterminate />
  <v-skeleton-loader v-if="loading" :loading="loading" type="ossein" />
  <v-row class="py-12" v-if="emptyStore">
    <v-col>
      <p class="text-center mx-auto">No documents available to display.</p>
    </v-col>
  </v-row>
  <!-- todo: extract out to own sfc -->
  <v-dialog v-model="showReviewModal" persistent max-width="750">
    <v-card>
      <!-- Header -->
      <v-card-title class="d-flex align-center">
        <v-icon class="me-2" :icon="mdiPencilBoxOutline" />
          Review Order
        <v-spacer />
        <v-btn
          icon
          variant="text"
          density="comfortable"
          @click="closeReviewModal()"
          aria-label="Close dialog"
        >
          <v-icon :icon="mdiClose" />
        </v-btn>
      </v-card-title>
      <v-divider />

      <!-- Body -->
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis">
          Add any notes or reasoning for your decision. These comments will be saved with the order. <br/>
          Note: Comments are required for any action other than Approval.
        </p>

        <v-textarea
          ref="commentsRef"
          v-model="comments"
          label="Review comments"
          rows="4"
          auto-grow
          clearable
          variant="outlined"
        />
        <v-alert
            v-if="approveBlocked"
            type="warning"
            variant="tonal"
            density="comfortable"
            class="mx-6 mt-2"
          >
          Document signature is required before Approval.
        </v-alert>
      </v-card-text>

      <v-divider />

      <!-- Actions -->
      <v-card-actions class="px-6 py-4">
        <!-- Left (destructive / secondary) -->
        <div class="d-flex ga-2">
          <v-btn
            color="error"
            variant="text"
            :prepend-icon="mdiClose"
            :disabled="!canReject"
            @click="closeReviewModal()"
          >
            Reject
          </v-btn>

          <v-btn
            color="warning"
            variant="outlined"
            :prepend-icon="mdiAccountClock"
            :disabled="!canReject"
            @click="closeReviewModal()"
          >
            Awaiting documentation
          </v-btn>
        </div>

        <v-spacer />

        <!-- Primary -->
        <v-btn
          color="success"
          size="large"
          :prepend-icon="mdiCheckBold"
          @click="approveOrder()"
        >
          Approve
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { onMounted, onUnmounted, ref, computed } from 'vue';
  import { mdiNotebookOutline, mdiFileDocumentArrowRightOutline, mdiCheckBold, mdiClose, mdiPencilBoxOutline, mdiAccountClock } from '@mdi/js';
  import { useCourtFileSearchStore } from '@/stores';
  import { KeyValueInfo } from '@/types/common';
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
  const comments = ref();
  const canReject = computed<boolean>(() => comments.value?.length > 0 );
  const approveBlocked = ref<boolean>(false);

  let instance = {} as any; // NutrientViewer.Instance

  const configuration = {
    container: '.pdf-container',
    licenseKey: commonStore.appInfo?.nutrientFeLicenseKey ?? '',
  };

  const approveOrder = async () => {

    async function hasAnnotations(pageIndex: number) {
      const annotations = await instance.getAnnotations(pageIndex);
      return annotations.size > 0;
    }

    // Usage example.
    async function checkDocument() {
      for (let i = 0; i < instance.totalPageCount; i++) {
        if (await hasAnnotations(i)) {
          console.log("Document contains annotations");
          approveBlocked.value = false
          showReviewModal.value = false
          return true;
        }
      }
      console.log("No annotations found");
      approveBlocked.value = true
      return false;
    }

    checkDocument();
    return true;
  };

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
        onPress: (event) => {
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
        onPress: (event) => {
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
        items.push(openInfoItem);
        items.push(reviewItem);
        return items;
      });
    } catch (error) {
      console.error('Error loading PDF:', error);
      loading.value = false;
      emptyStore.value = true;
    }
  };

  const closeReviewModal = () => {
    showReviewModal.value = false;
    approveBlocked.value = false;
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
