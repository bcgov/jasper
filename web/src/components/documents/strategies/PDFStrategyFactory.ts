import { PDFViewerStrategy } from './PDFViewerTypes';
import { FilePDFStrategy } from './FilePDFStrategy';
import { OrderPDFStrategy } from './OrderPDFStrategy';
import { CriminalDocumentPDFStrategy } from './CriminalDocumentPDFStrategy';
import { JudicialBinderPDFStrategy } from './JudicialBinderPDFStrategy';

export enum PDFViewerType {
  FILE = 'file',
  ORDER = 'order',
  CRIMINAL_BUNDLE = 'criminal-bundle',
  JUDICIAL_BINDER = 'judicial-binder',
}

export class PDFStrategyFactory {
  static createStrategy(type: PDFViewerType): PDFViewerStrategy {
    switch (type) {
      case PDFViewerType.FILE:
        return new FilePDFStrategy();
      case PDFViewerType.CRIMINAL_BUNDLE:
        return new CriminalDocumentPDFStrategy();
      case PDFViewerType.JUDICIAL_BINDER:
        return new JudicialBinderPDFStrategy();
      case PDFViewerType.ORDER:
        return new OrderPDFStrategy();
      default:
        throw new Error(`Unknown PDF viewer type: ${type}`);
    }
  }
}

export function usePDFStrategy(type: PDFViewerType) {
  return PDFStrategyFactory.createStrategy(type);
}
