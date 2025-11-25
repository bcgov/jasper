import { FilePDFStrategy } from './FilePDFStrategy';
import { BundlePDFStrategy } from './BundlePDFStrategy';
import { TranscriptPDFStrategy } from './TranscriptPDFStrategy';
import { PDFViewerStrategy } from '@/components/documents/FileViewer.vue';

export enum PDFViewerType {
  FILE = 'file',
  BUNDLE = 'bundle',
  TRANSCRIPT = 'transcript',
}

export class PDFStrategyFactory {
  static createStrategy(type: PDFViewerType): PDFViewerStrategy {
    switch (type) {
      case PDFViewerType.FILE:
        return new FilePDFStrategy();
      case PDFViewerType.BUNDLE:
        return new BundlePDFStrategy();
      case PDFViewerType.TRANSCRIPT:
        return new TranscriptPDFStrategy();
      default:
        throw new Error(`Unknown PDF viewer type: ${type}`);
    }
  }
}

export function usePDFStrategy(type: PDFViewerType) {
  return PDFStrategyFactory.createStrategy(type);
}
