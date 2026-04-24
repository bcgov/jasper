import {
  FileMetadataDto,
  TransitoryMergeContext,
  TransitoryViewerPayload,
} from '@/types/transitory-documents';
import { Router } from 'vue-router';

const generateViewerStorageKey = (): string => {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
};

export const openTransitoryDocumentsInNutrient = (
  router: Router,
  docs: FileMetadataDto[],
  context: TransitoryMergeContext
): void => {
  const viewerKey = generateViewerStorageKey();
  const payload: TransitoryViewerPayload = {
    files: docs,
    context,
  };

  sessionStorage.setItem(
    `transitoryDocuments:${viewerKey}`,
    JSON.stringify(payload)
  );

  const route = router.resolve({
    name: 'NutrientContainer',
    query: { type: 'transitory-bundle', tdKey: viewerKey },
  });

  window.open(route.href, '_blank');
};
