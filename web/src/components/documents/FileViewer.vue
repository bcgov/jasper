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
    :review-order="reviewOrder"
  />
  <div v-show="!loading" ref="pdf-container" class="pdf-container" />
</template>

<script setup lang="ts">
  import type { OrderService } from '@/services';
  import { useCommonStore } from '@/stores';
  import type { OrderReview } from '@/types';
  import { OrderReviewStatus } from '@/types/common';
  import { arrayBufferToBase64 } from '@/utils/utils';
  import type NutrientViewer from '@nutrient-sdk/viewer';
  import type { Instance, ToolbarItem } from '@nutrient-sdk/viewer';
  import { computed, inject, onMounted, onUnmounted, ref } from 'vue';
  import { useRoute } from 'vue-router';
  import ReviewModal from './ReviewModal.vue';
  import type { AnyPDFViewerStrategy } from './strategies/PDFStrategyFactory';
  import type {
    EmbeddedOutlineAwarePDFViewerStrategy,
    OutlineItem,
    PDFViewerInformationContext,
    PDFViewerToolbarContext,
  } from './strategies/PDFViewerTypes';

  // Props for the generic component
  interface Props<
    TStrategy extends AnyPDFViewerStrategy = AnyPDFViewerStrategy,
  > {
    strategy: TStrategy;
  }

  const props = defineProps<Props>();
  const route = useRoute();
  const commonStore = useCommonStore();
  const loading = ref(false);
  const emptyStore = ref(false);
  const showReviewModal = ref(false);
  const canApprove = ref<boolean>(false);
  const sessionId = computed(() => {
    const value = route.query.sessionId;

    return typeof value === 'string' && value.length > 0 ? value : undefined;
  });
  const nutrientViewer: typeof NutrientViewer = globalThis.NutrientViewer;
  if (!nutrientViewer) {
    throw new Error('Nutrient Web SDK is not loaded.');
  }

  const orderService = inject<OrderService>('orderService');
  if (!orderService) {
    throw new Error('Service(s) is undefined.');
  }

  let instance!: Instance;

  const configuration = {
    container: '.pdf-container',
    licenseKey: commonStore.appInfo?.nutrientFeLicenseKey ?? '',
    styleSheets: [`${import.meta.env.BASE_URL}styles/nutrient-toolbar.css`],
  };

  async function hasImageAnnotation(
    pageIndex: number,
    requiredDescriptions: string[] | undefined
  ) {
    const annotations = await instance.getAnnotations(pageIndex);
    return (
      annotations.filter((a) => {
        if (!(a instanceof nutrientViewer.Annotations.ImageAnnotation)) {
          return false;
        }
        return (
          a.contentType?.includes('image') &&
          (!requiredDescriptions ||
            (a.description !== null &&
              requiredDescriptions.includes(a.description)))
        );
      }).size > 0
    );
  }

  async function checkDocumentForAnnotations() {
    const requiredDescriptions =
      props.strategy.getRequiredApprovalAnnotations?.();
    for (let i = 0; i < instance.totalPageCount; i++) {
      if (await hasImageAnnotation(i, requiredDescriptions)) return true;
    }
    return false;
  }

  async function updateCanApprove() {
    canApprove.value = await checkDocumentForAnnotations();
  }

  const loadNutrient = async () => {
    loading.value = true;
    emptyStore.value = false;

    if (!props.strategy.hasData(sessionId.value)) {
      loading.value = false;
      emptyStore.value = true;
      return;
    }

    try {
      // Follow the strategy pattern workflow
      const rawData = props.strategy.getRawData(sessionId.value);
      const processedData = props.strategy.processDataForAPI(rawData);

      const [apiResponse] = await Promise.all([
        props.strategy.generatePDF(processedData),
        props.strategy.initialize?.(),
      ]);

      loading.value = false;

      const base64Pdf = props.strategy.extractBase64PDF(apiResponse);

      instance = await nutrientViewer.load({
        ...configuration,
        document: `data:application/pdf;base64,${base64Pdf}`,
      });

      // Default the built-in line tool's stroke color to red instead of blue.
      instance.setAnnotationPresets((presets) => ({
        ...presets,
        line: {
          ...presets.line,
          strokeColor: nutrientViewer.Color.RED,
        },
      }));

      if (supportsEmbeddedOutline(props.strategy)) {
        const outline = props.strategy.createOutlineWithEmbeddedOutline(
          rawData,
          apiResponse,
          await getEmbeddedOutline()
        );

        if (outline?.length) {
          const nutrientOutline = createNutrientOutline(outline);
          await instance.setDocumentOutline(nutrientOutline);
        }
      } else {
        const outline =
          props.strategy.createOutline(rawData, apiResponse) ?? [];
        const nutrientOutline = createNutrientOutline(outline);
        await instance.setDocumentOutline(nutrientOutline);
      }

      instance.setViewState((viewState) =>
        viewState.set(
          'sidebarMode',
          nutrientViewer.SidebarMode.DOCUMENT_OUTLINE
        )
      );

      addCustomToolbarItems(rawData);

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

  const resolveInformationContext = (
    rawData: unknown
  ): PDFViewerInformationContext | undefined => {
    const strategyContext = props.strategy.resolveInformationContext?.(rawData);

    if (strategyContext) {
      return strategyContext;
    }

    for (const item of getKnownRawItems(rawData)) {
      const itemContext = resolveInformationContextFromItem(item);

      if (itemContext) {
        return itemContext;
      }
    }

    return undefined;
  };

  const getKnownRawItems = (rawData: unknown): unknown[] => {
    if (Array.isArray(rawData)) {
      return rawData;
    }

    if (!isRecord(rawData)) {
      return [];
    }

    return Object.values(rawData).flatMap((value) => {
      if (Array.isArray(value)) {
        return value;
      }

      if (!isRecord(value)) {
        return [];
      }

      return Object.values(value).flatMap((nestedValue) =>
        Array.isArray(nestedValue) ? nestedValue : []
      );
    });
  };

  const resolveInformationContextFromItem = (
    item: unknown
  ): PDFViewerInformationContext | undefined => {
    if (!isRecord(item)) {
      return undefined;
    }

    const physicalFileId = getString(item.physicalFileId);
    const request = item.request;
    const requestData = isRecord(request) ? request.data : undefined;

    if (physicalFileId && isRecord(requestData)) {
      return {
        physicalFileId,
        isCriminal: requestData.isCriminal === true,
      };
    }

    const appearance = item.appearance;
    if (isRecord(appearance)) {
      const appearanceFileId =
        physicalFileId || getString(appearance.physicalFileId);

      if (appearanceFileId) {
        return {
          physicalFileId: appearanceFileId,
          isCriminal: true,
        };
      }
    }

    const labels = item.labels;
    if (physicalFileId && isRecord(labels)) {
      return {
        physicalFileId,
        isCriminal: getString(labels.isCriminal)?.toLowerCase() === 'true',
      };
    }

    return undefined;
  };

  const isRecord = (value: unknown): value is Record<string, unknown> =>
    typeof value === 'object' && value !== null;

  const getString = (value: unknown): string | undefined =>
    typeof value === 'string' && value.length > 0 ? value : undefined;

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
      isExpanded: true,
    };

    if (item.children?.length) {
      return new nutrientViewer.OutlineElement({
        ...baseElement,
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
    if (!props.strategy.reviewOrder) {
      return;
    }

    // If the user 'submitted' the Order and did not upload a supporting document, export the flattened PDF
    if (
      (orderReview.status === OrderReviewStatus.Approved ||
        orderReview.status === OrderReviewStatus.OrderMade) &&
      !orderReview.supportingDocumentData
    ) {
      const arrayBuffer = await instance.exportPDF({ flatten: true });
      orderReview.documentData = arrayBufferToBase64(arrayBuffer);
    }
    await props.strategy.reviewOrder(orderReview);
  };

  const addCustomToolbarItems = (rawData: unknown) => {
    const context: PDFViewerToolbarContext = {
      instance,
      nutrientViewer,
      rawData,
      resolveInformationContext,
      openReviewModal: () => {
        showReviewModal.value = true;
      },
      updateCanApprove,
    };

    instance.setToolbarItems((items: ToolbarItem[]) => {
      // Add custom toolbar items based on the strategy and context
      const allItems =
        props.strategy.setToolbarItems?.(items, context) ?? items;

      return arrangeToolbarItems(allItems);
    });
  };

  onMounted(() => {
    loadNutrient();
  });

  onUnmounted(() => {
    if (instance) {
      nutrientViewer.unload(instance);
    }
    if (props.strategy.cleanup) {
      props.strategy.cleanup(sessionId.value);
    }
  });

  const arrangeToolbarItems = (allItems: ToolbarItem[]): ToolbarItem[] => {
    // Desired order of default toolbar items
    const desiredTypes: ToolbarItem['type'][] = [
      'line',
      'arrow',
      'rectangle',
      'ellipse',
      'polygon',
      'cloudy-polygon',
      'polyline',
      'ink',
      'highlighter',
      'text-highlighter',
      'ink-eraser',
      'content-editor',
      'search',
      'export-pdf',
    ];

    // Create the ordered block, preserving existing items and adding missing ones.
    const desiredTypeSet = new Set(desiredTypes);
    const desiredItems: ToolbarItem[] = desiredTypes.map(
      (type) => allItems.find((item) => item.type === type) ?? { type }
    );

    // Keep the remaining items (those not part of the block) in their original order.
    const remainingItems = allItems.filter(
      (item) => !desiredTypeSet.has(item.type)
    );

    // Insert the sequence just after the text tool (or append if absent).
    const textIndex = remainingItems.findIndex((item) => item.type === 'text');
    const insertAt = textIndex === -1 ? remainingItems.length : textIndex + 1;

    return [
      ...remainingItems.slice(0, insertAt),
      ...desiredItems,
      ...remainingItems.slice(insertAt),
    ];
  };
</script>

<style scoped>
  .pdf-container {
    height: 90vh;
  }
  .v-skeleton-loader {
    height: 100%;
  }
</style>
