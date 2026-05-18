import { PDFViewerStrategy } from './PDFViewerTypes';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { FilePDFStrategy } from './FilePDFStrategy';
import { OrderPDFStrategy } from './OrderPDFStrategy';
import { TransitoryBundleStrategy } from './TransitoryBundleStrategy';
import { CriminalDocumentPDFStrategy } from './CriminalDocumentPDFStrategy';
import { JudicialBinderPDFStrategy } from './JudicialBinderPDFStrategy';

export enum PDFViewerType {
  FILE = 'file',
  ORDER = 'order',
  TRANSITORY_BUNDLE = 'transitory-bundle',
  CRIMINAL_BUNDLE = 'criminal-bundle',
  JUDICIAL_BINDER = 'judicial-binder',
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
      case PDFViewerType.CRIMINAL_BUNDLE:
        return new CriminalDocumentPDFStrategy();
      case PDFViewerType.JUDICIAL_BINDER:
        return new JudicialBinderPDFStrategy();
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
