import { describe, it, expect, beforeEach, vi } from 'vitest';
import { JudicialBinderPDFStrategy } from '@/components/documents/strategies/JudicialBinderPDFStrategy';
import { useJudicialBinderStore } from '@/stores';
import { inject } from 'vue';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import { BinderDocumentBundleRequest } from '@/types/DocumentBundleRequest';

vi.mock('@/stores', () => ({
  useJudicialBinderStore: vi.fn(),
}));
vi.mock('vue', () => ({
  inject: vi.fn(),
}));
vi.mock('@/services', () => ({
  BinderService: vi.fn(),
}));

const mockBinderRequest: BinderDocumentBundleRequest = {
  binders: [
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
  ],
};

const mockJudicialBinderStore = {
  getRequests: mockBinderRequest,
  clearBundles: vi.fn(),
};

const mockBinderService = {
  viewBinderPDF: vi.fn(),
};

const mockApiResponse: ApiResponse<any> = {
  errors:[],
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
    mockBinderService.viewBinderPDF.mockClear();
  });

  it('throws error if BinderService is not injected', () => {
    (inject as any).mockReturnValueOnce(undefined);
    expect(() => new JudicialBinderPDFStrategy()).toThrow('BinderService is not available!');
  });

  it('hasData returns true if binders exist in request', () => {
    const strategy = new JudicialBinderPDFStrategy();
    expect(strategy.hasData()).toBe(true);
  });

  it('hasData returns false if request is empty', () => {
    (useJudicialBinderStore as any).mockReturnValue({
      getRequests: { binders: [] },
      clearBundles: vi.fn(),
    });
    const strategy = new JudicialBinderPDFStrategy();
    expect(strategy.hasData()).toBe(false);
  });

  it('getRawData returns binder requests without grouping', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    expect(rawData.length).toBe(2);
    expect(rawData[0].binder).toEqual(mockBinderRequest.binders[0]);
    expect(rawData[0].fileNumber).toBe('F1');
    expect(rawData[1].binder).toEqual(mockBinderRequest.binders[1]);
    expect(rawData[1].fileNumber).toBe('F2');
  });

  it('processDataForAPI returns binder label contexts', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    const result = strategy.processDataForAPI(rawData);
    expect(result).toEqual([
      {
        physicalFileId: 'F1',
        courtClassCd: 'CLS1',
      },
      {
        physicalFileId: 'F2',
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
    expect(mockBinderService.viewBinderPDF).toHaveBeenCalledWith(
      contexts,
      ['INITIATING', 'BAIL']
    );
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

  it('createOutline creates simple outline by file number (no appearance grouping)', () => {
    const strategy = new JudicialBinderPDFStrategy();
    const rawData = strategy.getRawData();
    const outline = strategy.createOutline(rawData, mockApiResponse);
    
    expect(outline.length).toBe(2); // Two file numbers
    expect(outline[0].title).toBe('F1');
    expect(outline[0]?.children?.length).toBe(2); // Two documents in F1
    expect(outline[1].title).toBe('F2');
    expect(outline[1]?.children?.length).toBe(1); // One document in F2
    expect(outline[0]?.children?.[0]?.title).toBe('JudicialDoc1.pdf');
    expect(outline[0]?.children?.[1]?.title).toBe('JudicialDoc2.pdf');
    expect(outline[1]?.children?.[0]?.title).toBe('JudicialDoc3.pdf');
  });

  it('cleanup calls binderStore.clearBundles', () => {
    const strategy = new JudicialBinderPDFStrategy();
    strategy.cleanup();
    expect(mockJudicialBinderStore.clearBundles).toHaveBeenCalled();
  });

  it('makeDocElement returns correct OutlineItem', () => {
    const strategy = new JudicialBinderPDFStrategy();
    (strategy as any).count = 1;
    const doc: BinderDocument = {
      documentId: '999',
      fileName: 'TestJudicialDoc.pdf',
      documentType: 'PDF',
    };
    const apiResponse = {
      payload: {
        pdfResponse: {
          pageRanges: [{ start: 100 }, { start: 200 }],
        },
      },
    };
    const item = (strategy as any).makeDocElement(doc, apiResponse);
    expect(item.title).toBe('TestJudicialDoc.pdf');
    expect(item.pageIndex).toBe(200);
  });
});
