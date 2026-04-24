import { PDFViewerStrategy } from '@/components/documents/FileViewer.vue';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { BundlePDFStrategy } from './BundlePDFStrategy';
import { FilePDFStrategy } from './FilePDFStrategy';
import { OrderPDFStrategy } from './OrderPDFStrategy';
import { TransitoryBundleStrategy } from './TransitoryBundleStrategy';

export enum PDFViewerType {
  FILE = 'file',
  BUNDLE = 'bundle',
  ORDER = 'order',
  TRANSITORY_BUNDLE = 'transitory-bundle',
}

export class PDFStrategyFactory {
  static createStrategy(
    type: PDFViewerType,
    transitoryDocumentsService?: TransitoryDocumentsService,
    transitoryStorageKey?: string
  ): PDFViewerStrategy {
    switch (type) {
      case PDFViewerType.FILE:
        return new FilePDFStrategy();
      case PDFViewerType.BUNDLE:
        return new BundlePDFStrategy();
      case PDFViewerType.ORDER:
        return new OrderPDFStrategy();
      case PDFViewerType.TRANSITORY_BUNDLE: {
        if (!transitoryDocumentsService) {
          throw new Error('TransitoryDocumentsService is not available!');
        }
        if (!transitoryStorageKey) {
          throw new Error('Transitory bundle key is missing.');
        }
        return new TransitoryBundleStrategy(
          transitoryDocumentsService,
          transitoryStorageKey
        );
      }
      default:
        throw new Error(`Unknown PDF viewer type: ${type}`);
    }
  }
}

export function usePDFStrategy(
  type: PDFViewerType,
  transitoryDocumentsService?: TransitoryDocumentsService,
  transitoryStorageKey?: string
) {
  return PDFStrategyFactory.createStrategy(
    type,
    transitoryDocumentsService,
    transitoryStorageKey
  );
}
