<template>
  <FileViewer :strategy="strategy" />
</template>

<script setup lang="ts">
  import FileViewer from '@/components/documents/FileViewer.vue';
  import {
    PDFViewerType,
    usePDFStrategy,
  } from '@/components/documents/strategies/PDFStrategyFactory';
  import { computed } from 'vue';
  import { useRoute } from 'vue-router';

  const route = useRoute();

  const strategy = computed(() => {
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

    console.warn('Could not determine PDF viewer type');
    // Provide a default strategy to avoid returning undefined
    return usePDFStrategy(PDFViewerType.FILE);
  });
</script>