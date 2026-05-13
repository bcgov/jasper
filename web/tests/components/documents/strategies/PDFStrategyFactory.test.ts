import { FilePDFStrategy } from '@/components/documents/strategies/FilePDFStrategy';
import { CriminalDocumentPDFStrategy } from '@/components/documents/strategies/CriminalDocumentPDFStrategy';
import { JudicialBinderPDFStrategy } from '@/components/documents/strategies/JudicialBinderPDFStrategy';
import {
  PDFViewerType,
  usePDFStrategy,
} from '@/components/documents/strategies/PDFStrategyFactory';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { inject } from 'vue';

vi.mock('vue', () => ({
  inject: vi.fn(),
}));
vi.mock('@/services/FilesService');
class MockFilesService {
  generatePdf = vi.fn();
}
describe('PDFStrategyFactory', () => {
  let filesService: any;

  beforeEach(() => {
    vi.clearAllMocks();
    filesService = {} as MockFilesService;
    (inject as any).mockReturnValue(filesService);
    setActivePinia(createPinia());
  });

  it('should create FilePDFStrategy for FILE type', () => {
    const strategy = usePDFStrategy(PDFViewerType.FILE);
    expect(strategy).toBeInstanceOf(FilePDFStrategy);
  });

  it('should create KeyDocumentPDFStrategy for KEY_DOCUMENT type', () => {
    const strategy = usePDFStrategy(PDFViewerType.CRIMINAL_BUNDLE);
    expect(strategy).toBeInstanceOf(CriminalDocumentPDFStrategy);
  });

  it('should create JudicialBinderPDFStrategy for JUDICIAL_BINDER type', () => {
    const strategy = usePDFStrategy(PDFViewerType.JUDICIAL_BINDER);
    expect(strategy).toBeInstanceOf(JudicialBinderPDFStrategy);
  });
});
