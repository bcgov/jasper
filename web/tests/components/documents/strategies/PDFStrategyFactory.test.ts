import { BundlePDFStrategy } from '@/components/documents/strategies/BundlePDFStrategy';
import { FilePDFStrategy } from '@/components/documents/strategies/FilePDFStrategy';
import {
    PDFViewerType,
    usePDFStrategy,
  } from '@/components/documents/strategies/PDFStrategyFactory';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia'
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

        vi.mock('@/stores/PDFViewerStore', () => {
            return {
                usePDFViewerStore: vi.fn(() => ({})),
            };
        });
        (inject as unknown as vi.mock).mockReturnValue(filesService);
        setActivePinia(createPinia());
    });

  it('should create FilePDFStrategy for FILE type', () => {
    const strategy = usePDFStrategy(PDFViewerType.FILE);
    expect(strategy).toBeInstanceOf(FilePDFStrategy);
  });
    it('should create BundlePDFStrategy for BUNDLE type', () => {
    const strategy = usePDFStrategy(PDFViewerType.BUNDLE);
    expect(strategy).toBeInstanceOf(BundlePDFStrategy);
  });
});