<template>
  <FileViewer :strategy="strategy" />
</template>

<script setup lang="ts">
  import FileViewer from '@/components/documents/FileViewer.vue';
  import {
    PDFViewerType,
    usePDFStrategy,
  } from '@/components/documents/strategies/PDFStrategyFactory';
  import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
  import { computed, inject } from 'vue';
  import { useRoute } from 'vue-router';

  const route = useRoute();
  const transitoryDocumentsService = inject<TransitoryDocumentsService>(
    'transitoryDocumentsService'
  );

  const strategy = computed(() => {
    if (route.query.type) {
      const queryType = route.query.type as string;
      switch (queryType.toLowerCase()) {
        case 'bundle':
          return usePDFStrategy(PDFViewerType.BUNDLE);
        case 'order':
          return usePDFStrategy(PDFViewerType.ORDER);
        case 'transitory-bundle':
          if (!transitoryDocumentsService) {
            throw new Error('TransitoryDocumentsService is not available!');
          }
          const storageKey = route.query.tdKey as string | undefined;
          if (!storageKey) {
            throw new Error('Missing transitory bundle key.');
          }
          return usePDFStrategy(
            PDFViewerType.TRANSITORY_BUNDLE,
            transitoryDocumentsService,
            storageKey
          );
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
