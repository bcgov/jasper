import { describe, it, expect, beforeEach, vi } from 'vitest';
import { JudicialBinderPDFStrategy } from '@/components/documents/strategies/JudicialBinderPDFStrategy';
import { useJudicialBinderStore } from '@/stores';
import { inject } from 'vue';
import { ApiResponse } from '@/types/ApiResponse';

vi.mock('@/stores', () => ({
  useJudicialBinderStore: vi.fn(),
}));
vi.mock('vue', () => ({
  inject: vi.fn(),
}));
vi.mock('@/services', () => ({
  BinderService: vi.fn(),
}));

const mockBundles = [
  {
    id: 'bundle-1',
    binders: [
      {
        binder: {
          id: 'binder-1',
          labels: {
            physicalFileId: 'F1',
            participantId: 'P1',
            courtClassCd: 'CLS1',
          },
          documents: [
            {
              documentId: '101',
              fileName: 'JudicialDoc1.pdf',
              documentType: 'PDF',
            },
            {
              documentId: '102',
              fileName: 'JudicialDoc2.pdf',
              documentType: 'PDF',
            },
          ],
        },
        fileNumber: 'F1',
        groupKeyOne: 'F1',
        groupKeyTwo: '',
        documentName: '1 - JudicialDoc1 - 01-Jun-2024',
        physicalFileId: 'F1',
        participantId: 'P1',
        documentId: '101',
      },
      {
        binder: {
          id: 'binder-1',
          labels: {
            physicalFileId: 'F1',
            participantId: 'P1',
            courtClassCd: 'CLS1',
          },
          documents: [
            {
              documentId: '101',
              fileName: 'JudicialDoc1.pdf',
              documentType: 'PDF',
            },
            {
              documentId: '102',
              fileName: 'JudicialDoc2.pdf',
              documentType: 'PDF',
            },
          ],
        },
        fileNumber: 'F1',
        groupKeyOne: 'F1',
        groupKeyTwo: '',
        documentName: '2 - JudicialDoc2 - 02-Jun-2024',
        physicalFileId: 'F1',
        participantId: 'P1',
        documentId: '102',
      },
      {
        binder: {
          id: 'binder-2',
          labels: {
            physicalFileId: 'F2',
            participantId: 'P2',
            courtClassCd: 'CLS2',
          },
          documents: [
            {
              documentId: '201',
              fileName: 'JudicialDoc3.pdf',
              documentType: 'PDF',
            },
          ],
        },
        fileNumber: 'F2',
        groupKeyOne: 'F2',
        groupKeyTwo: '',
        documentName: '3 - JudicialDoc3 - 03-Jun-2024',
        physicalFileId: 'F2',
        participantId: 'P2',
        documentId: '201',
      },
    ],
  },
];

const mockJudicialBinderStore = {
  hasPdfData: vi.fn(() => true),
  getPdfItems: vi.fn(() => mockBundles[0].binders),
  getBundles: mockBundles,
  clearPdfItems: vi.fn(),
  clearBundles: vi.fn(),
};

const mockBinderService = {
  viewBinderPDF: vi.fn(),
};

const mockApiResponse: ApiResponse<any> = {
  errors: [],
  succeeded: true,
  payload: {
    pdfResponse: {
      base64Pdf: 'base64judicial',
      pageRanges: [
        { start: 1, end: 3 },
        { start: 4, end: 6 },
      ],
    },
    binders: [
      {
        labels: {
          physicalFileId: 'F1',
          participantId: 'P1',
        },
        documents: [
          {
            documentId: '101',
            fileName: 'JudicialDoc1.pdf',
            documentType: 'PDF',
          },
          {
            documentId: '102',
            fileName: 'JudicialDoc2.pdf',
            documentType: 'PDF',
          },
        ],
      },
      {
        labels: {
          physicalFileId: 'F2',
          participantId: 'P2',
        },
        documents: [
          {
            documentId: '201',
            fileName: 'JudicialDoc3.pdf',
            documentType: 'PDF',
          },
        ],
      },
    ],
  },
};

describe('JudicialBinderPDFStrategy', () => {
  beforeEach(() => {
    (useJudicialBinderStore as any).mockReturnValue(mockJudicialBinderStore);
    (inject as any).mockImplementation((key: string) => {
      if (key === 'binderService') return mockBinderService;
      return undefined;
    });
    mockJudicialBinderStore.clearBundles.mockClear();
    mockJudicialBinderStore.clearPdfItems.mockClear();
    mockJudicialBinderStore.getPdfItems.mockImplementation(
      () => mockBundles[0].binders
    );
    mockJudicialBinderStore.hasPdfData.mockImplementation(() => true);
    mockBinderService.viewBinderPDF.mockClear();
  });

  it('throws error if BinderService is not injected', () => {
    (inject as any).mockReturnValueOnce(undefined);
    expect(() => new JudicialBinderPDFStrategy()).toThrow(
      'BinderService is not available!'
    );
  });

  it('hasData returns true if binder documents exist in the active session', () => {
    const strategy = new JudicialBinderPDFStrategy();
    expect(strategy.hasData()).toBe(true);
  });

  it('hasData returns false if bundles are empty', () => {
    (useJudicialBinderStore as any).mockReturnValue({
      hasPdfData: vi.fn(() => false),
      getPdfItems: vi.fn(() => []),
      clearPdfItems: vi.fn(),
      clearBundles: vi.fn(),
    });
    const strategy = new JudicialBinderPDFStrategy();
    expect(strategy.hasData()).toBe(false);
  });

  it('getRawData returns the stored binder document entries', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    expect(rawData).toEqual(mockBundles[0].binders);
    expect(rawData).toHaveLength(3);
    expect(rawData[0].documentId).toBe('101');
  });

  it('processDataForAPI returns unique binder label contexts', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    const result = strategy.processDataForAPI(rawData);
    expect(result).toEqual([
      {
        physicalFileId: 'F1',
        participantId: 'P1',
        courtClassCd: 'CLS1',
      },
      {
        physicalFileId: 'F2',
        participantId: 'P2',
        courtClassCd: 'CLS2',
      },
    ]);
  });

  it('getPdf calls binderService.viewBinderPDF', async () => {
    const strategy = new JudicialBinderPDFStrategy();
    mockBinderService.viewBinderPDF.mockResolvedValue(mockApiResponse);
    const contexts = [{ physicalFileId: 'F1', courtClassCd: 'CLS1' }];
    const result = await strategy.getPdf(contexts);
    expect(mockBinderService.viewBinderPDF).toHaveBeenCalledWith(contexts, []);
    expect(result).toBe(mockApiResponse);
  });

  it('generatePDF delegates to getPdf', async () => {
    const strategy = new JudicialBinderPDFStrategy();
    mockBinderService.viewBinderPDF.mockResolvedValue(mockApiResponse);
    const contexts = [{ physicalFileId: 'F1', courtClassCd: 'CLS1' }];

    const getPdfSpy = vi.spyOn(strategy, 'getPdf');
    const result = await strategy.generatePDF(contexts);

    expect(getPdfSpy).toHaveBeenCalledWith(contexts);
    expect(result).toBe(mockApiResponse);
  });

  it('generatePDF passes categories from URL params to getPdf', async () => {
    const strategy = new JudicialBinderPDFStrategy();
    mockBinderService.viewBinderPDF.mockResolvedValue(mockApiResponse);

    // Mock location.search with category params
    Object.defineProperty(globalThis, 'location', {
      value: { search: '?category=INITIATING,BAIL' },
      writable: true,
    });

    const contexts = [{ physicalFileId: 'F1', courtClassCd: 'CLS1' }];
    await strategy.generatePDF(contexts);
    expect(mockBinderService.viewBinderPDF).toHaveBeenCalledWith(contexts, [
      'INITIATING',
      'BAIL',
    ]);
  });

  it('extractBase64PDF returns base64Pdf from response', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const base64 = strategy.extractBase64PDF(mockApiResponse);
    expect(base64).toBe('base64judicial');
  });

  it('extractPageRanges returns pageRanges from response', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const ranges = strategy.extractPageRanges(mockApiResponse);
    expect(ranges).toEqual([
      { start: 1, end: 3 },
      { start: 4, end: 6 },
    ]);
  });

  it('createOutline uses grouped bundle metadata for titles and page indexes', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    const outline = strategy.createOutline(rawData, mockApiResponse);

    expect(outline.length).toBe(2); // Two file numbers
    expect(outline[0].title).toBe('F1');
    expect(outline[0]?.children?.length).toBe(2); // Two documents in F1
    expect(outline[1].title).toBe('F2');
    expect(outline[1]?.children?.length).toBe(1); // One document in F2
    expect(outline[0]?.children?.[0]?.title).toBe(
      '1 - JudicialDoc1 - 01-Jun-2024'
    );
    expect(outline[0]?.children?.[1]?.title).toBe(
      '2 - JudicialDoc2 - 02-Jun-2024'
    );
    expect(outline[1]?.children?.[0]?.title).toBe(
      '3 - JudicialDoc3 - 03-Jun-2024'
    );
    expect(outline[0]?.children?.[0]?.pageIndex).toBe(1);
    expect(outline[0]?.children?.[1]?.pageIndex).toBe(4);
  });

  it('cleanup calls binderStore.clearBundles', () => {
    const strategy = new JudicialBinderPDFStrategy();
    strategy.cleanup();
    expect(mockJudicialBinderStore.clearBundles).toHaveBeenCalled();
  });

  it('createOutline leaves page index undefined when no matching document is returned', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = [
      {
        ...mockBundles[0].binders[0],
        documentId: 'missing-doc',
      },
    ] as any;

    const outline = strategy.createOutline(rawData, mockApiResponse);
    expect(outline[0]?.children?.[0]?.title).toBe(
      '1 - JudicialDoc1 - 01-Jun-2024'
    );
    expect(outline[0]?.children?.[0]?.pageIndex).toBeUndefined();
  });
});
