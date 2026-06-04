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
    @reviewOrder="reviewOrder"
  />

  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import type { OrderService } from '@/services';
  import { useCommonStore } from '@/stores';
  import type { OrderReview } from '@/types';
  import { OrderReviewStatus } from '@/types/common';
  import { arrayBufferToBase64 } from '@/utils/utils';
  import {
    mdiFileDocumentArrowRightOutline,
    mdiNotebookOutline,
  } from '@mdi/js';
  import { inject, onMounted, onUnmounted, ref } from 'vue';
  import ReviewModal from './ReviewModal.vue';
  import type { AnyPDFViewerStrategy } from './strategies/PDFStrategyFactory';
  import type {
    EmbeddedOutlineAwarePDFViewerStrategy,
    OutlineItem,
  } from './strategies/PDFViewerTypes';

  // Props for the generic component
  interface Props<
    TStrategy extends AnyPDFViewerStrategy = AnyPDFViewerStrategy,
  > {
    strategy: TStrategy;
  }

  const props = defineProps<Props>();
  const commonStore = useCommonStore();
  const loading = ref(false);
  const emptyStore = ref(false);
  const showReviewModal = ref(false);
  const canApprove = ref<boolean>(false);
  const nutrientViewer = globalThis.NutrientViewer as any;

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

      const base64Pdf = props.strategy.extractBase64PDF(apiResponse);

      const openInfoItem = {
        type: 'custom',
        id: 'open-information',
        title: 'Supporting information',
        icon: `<svg><path d="${mdiNotebookOutline}"/></svg>`,
        onPress: () => {
          let firstPhysicalFileId: string | undefined;
          let isCriminal: boolean | undefined;
          Object.values(rawData as Record<string, unknown>).forEach(
            (personDocuments) => {
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
            }
          );

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

      instance = await nutrientViewer.load({
        ...configuration,
        document: `data:application/pdf;base64,${base64Pdf}`,
      });

      if (supportsEmbeddedOutline(props.strategy)) {
        const outline = props.strategy.createOutlineWithEmbeddedOutline(
          rawData,
          apiResponse,
          await getEmbeddedOutline()
        );

        if (outline?.length) {
          const nutrientOutline = createNutrientOutline(outline);
          instance.setDocumentOutline(nutrientOutline);
        }
      } else {
        const outline =
          props.strategy.createOutline(rawData, apiResponse) ?? [];
        const nutrientOutline = createNutrientOutline(outline);
        instance.setDocumentOutline(nutrientOutline);
      }

      instance.setViewState((viewState) =>
        viewState.set(
          'sidebarMode',
          nutrientViewer.SidebarMode.DOCUMENT_OUTLINE
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
    return nutrientViewer.Immutable.List(
      outlineData.map((item) => createOutlineElement(item))
    );
  };

  const supportsEmbeddedOutline = (
    strategy: AnyPDFViewerStrategy
  ): strategy is AnyPDFViewerStrategy &
    EmbeddedOutlineAwarePDFViewerStrategy<unknown, unknown> => {
    return (
      typeof (strategy as { createOutlineWithEmbeddedOutline?: unknown })
        .createOutlineWithEmbeddedOutline === 'function'
    );
  };

  const getEmbeddedOutline = async (): Promise<OutlineItem[] | undefined> => {
    if (typeof instance.getDocumentOutline !== 'function') {
      console.warn(
        'Embedded outline API is unavailable; continuing without embedded outline.'
      );
      return undefined;
    }

    try {
      const embeddedOutline = await instance.getDocumentOutline();
      const outlineItems = convertOutlineListToItems(embeddedOutline);

      return outlineItems.length > 0 ? outlineItems : undefined;
    } catch (error) {
      console.warn(
        'Failed to extract embedded outline; continuing without embedded outline.',
        error
      );
      return undefined;
    }
  };

  const convertOutlineListToItems = (outlineList: any): OutlineItem[] => {
    const outlineElements = Array.isArray(outlineList)
      ? outlineList
      : (outlineList?.toArray?.() ?? Array.from(outlineList ?? []));

    return outlineElements.map((element: any) =>
      convertOutlineElementToItem(element)
    );
  };

  const convertOutlineElementToItem = (element: any): OutlineItem => {
    if (!element || typeof element.title !== 'string') {
      throw new TypeError('Embedded outline element is malformed.');
    }

    const childItems = convertOutlineListToItems(element.children);
    const actionPageIndex = element.action?.pageIndex;

    return {
      title: element.title,
      pageIndex:
        typeof actionPageIndex === 'number' ? actionPageIndex : undefined,
      isExpanded: element.isExpanded,
      children: childItems.length > 0 ? childItems : undefined,
    };
  };

  const createOutlineElement = (item: OutlineItem): any => {
    const baseElement = {
      title: item.title,
      action: createOutlineAction(item.pageIndex),
    };

    if (item.children?.length) {
      return new nutrientViewer.OutlineElement({
        ...baseElement,
        isExpanded: item.isExpanded ?? true,
        children: nutrientViewer.Immutable.List(
          item.children.map((child) => createOutlineElement(child))
        ),
      });
    }

    return new nutrientViewer.OutlineElement(baseElement);
  };

  const createOutlineAction = (pageIndex: number | undefined): any => {
    if (pageIndex === undefined) {
      return undefined;
    }

    return new nutrientViewer.Actions.GoToAction({ pageIndex });
  };

  const reviewOrder = async (orderReview: OrderReview) => {
    showReviewModal.value = false;
    if (!props.strategy.reviewOrder) {
      return;
    }
    // Check if strategy supports order review
    try {
      // If the user approved the Order and did not upload a supporting document, export the flattened PDF
      if (
        orderReview.status === OrderReviewStatus.Approved &&
        !orderReview.supportingDocumentData
      ) {
        const arrayBuffer = await instance.exportPDF({ flatten: true });
        orderReview.documentData = arrayBufferToBase64(arrayBuffer);
      }
      await props.strategy.reviewOrder(orderReview);
    } catch (error) {
      console.error('Error reviewing order:', error);
    }
  };

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    if (nutrientViewer) {
      nutrientViewer.unload('.pdf-container');
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
