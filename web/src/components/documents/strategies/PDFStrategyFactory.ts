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

/**
 * Strategy type-erasure alias.
 *
 * Each concrete strategy has different raw data, processed data, and API response
 * types, so callers that only need the common viewer workflow can use this type.
 */
export type AnyPDFViewerStrategy = PDFViewerStrategy<unknown, unknown, unknown>;

export class PDFStrategyFactory {
  static createStrategy(
    type: PDFViewerType,
    transitoryDocumentsService?: TransitoryDocumentsService,
    transitoryStorageKey?: string
  ): AnyPDFViewerStrategy {
    switch (type) {
      case PDFViewerType.FILE:
        return new FilePDFStrategy() as AnyPDFViewerStrategy;

      case PDFViewerType.ORDER:
        return new OrderPDFStrategy() as AnyPDFViewerStrategy;

      case PDFViewerType.CRIMINAL_BUNDLE:
        return new CriminalDocumentPDFStrategy() as AnyPDFViewerStrategy;

      case PDFViewerType.JUDICIAL_BINDER:
        return new JudicialBinderPDFStrategy() as AnyPDFViewerStrategy;

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
        return assertNever(type);
    }
  }

  static parseType(type: string | null | undefined): PDFViewerType {
    if (isPDFViewerType(type)) {
      return type;
    }

    throw new Error(`Unknown PDF viewer type: ${type ?? '(missing)'}`);
  }

  static createStrategyFromTypeParam(
    type: string | null | undefined
  ): AnyPDFViewerStrategy {
    return this.createStrategy(this.parseType(type));
  }
}

export function usePDFStrategy(
  type: PDFViewerType,
  transitoryDocumentsService?: TransitoryDocumentsService,
  transitoryStorageKey?: string
): AnyPDFViewerStrategy {
  return PDFStrategyFactory.createStrategy(
    type,
    transitoryDocumentsService,
    transitoryStorageKey
  );
}

export function isPDFViewerType(
  type: string | null | undefined
): type is PDFViewerType {
  return Object.values(PDFViewerType).includes(type as PDFViewerType);
}

function assertNever(value: never): never {
  throw new Error(`Unknown PDF viewer type: ${String(value)}`);
}
