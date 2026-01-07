import { PDFViewerStrategy } from '@/components/documents/FileViewer.vue';
import { OrderPDFStrategy } from './OrderPDFStrategy';
import { BundlePDFStrategy } from './BundlePDFStrategy';
import { FilePDFStrategy } from './FilePDFStrategy';
import { TransitoryBundleStrategy } from './TransitoryBundleStrategy';

export enum PDFViewerType {
  FILE = 'file',
  BUNDLE = 'bundle',
  ORDER = 'order',
  TRANSITORY_BUNDLE = 'transitory-bundle',
}

export class PDFStrategyFactory {
  static createStrategy(type: PDFViewerType): PDFViewerStrategy {
    switch (type) {
      case PDFViewerType.FILE:
        return new FilePDFStrategy();
      case PDFViewerType.BUNDLE:
        return new BundlePDFStrategy();
      case PDFViewerType.ORDER:
        return new OrderPDFStrategy();
      case PDFViewerType.TRANSITORY_BUNDLE:
        return new TransitoryBundleStrategy();
      default:
        throw new Error(`Unknown PDF viewer type: ${type}`);
    }
  }
}

export function usePDFStrategy(type: PDFViewerType) {
  return PDFStrategyFactory.createStrategy(type);
}
