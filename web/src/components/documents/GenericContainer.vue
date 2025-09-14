<template>
  <GenericPDFViewer :strategy="strategy" />
</template>

<script setup lang="ts">
  import GenericPDFViewer from '@/components/documents/shared/GenericPDFViewer.vue';
  import {
    PDFViewerType,
    usePDFStrategy,
  } from '@/strategies/PDFStrategyFactory';
  import { computed } from 'vue';
  import { useRoute } from 'vue-router';

  const route = useRoute();

  // Determine strategy based on query parameter or route configuration
  const strategy = computed(() => {
    // Priority 1: Explicit query parameter (e.g., /viewer?type=bundle)
    if (route.query.type) {
      const queryType = route.query.type as string;
      switch (queryType.toLowerCase()) {
        case 'bundle':
          return usePDFStrategy(PDFViewerType.BUNDLE);
        case 'nutrient':
        case 'file':
        case 'pdf':
          return usePDFStrategy(PDFViewerType.FILE);
        default:
          throw new Error(`Unknown PDF viewer type: ${queryType}`);
      }
    }

    // // Priority 2: Route meta field
    // if (route.meta?.pdfViewerType) {
    //   const metaType = route.meta.pdfViewerType as PDFViewerType;
    //   return usePDFStrategy(metaType);
    // }

    // // Priority 3: Route name pattern
    // const routeName = route.name as string;
    // if (routeName?.toLowerCase().includes('bundle')) {
    //   return usePDFStrategy(PDFViewerType.BUNDLE);
    // } else if (routeName?.toLowerCase().includes('pdf') || routeName?.toLowerCase().includes('file')) {
    //   return usePDFStrategy(PDFViewerType.FILE);
    // }

    // // Priority 4: Route path pattern
    // if (route.path.includes('/bundle')) {
    //   return usePDFStrategy(PDFViewerType.BUNDLE);
    // } else if (route.path.includes('/pdf') || route.path.includes('/file')) {
    //   return usePDFStrategy(PDFViewerType.FILE);
    // }

    // Fallback
    console.warn('Could not determine PDF viewer type, defaulting to NUTRIENT');
    return usePDFStrategy(PDFViewerType.FILE);
  });
</script>
