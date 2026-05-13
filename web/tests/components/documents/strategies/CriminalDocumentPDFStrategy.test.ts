import { describe, it, expect, beforeEach, vi } from 'vitest';
import { CriminalDocumentPDFStrategy } from '@/components/documents/strategies/CriminalDocumentPDFStrategy';
import { useCriminalDocumentBundleStore } from '@/stores';
import { inject } from 'vue';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import { CriminalDocumentAppearanceRequest } from '@/stores/CriminalDocumentBundleStore';
import { BinderService } from '@/services';

vi.mock('@/stores', () => ({
  useCriminalDocumentBundleStore: vi.fn(),
}));
vi.mock('vue', () => ({
  inject: vi.fn(),
}));
vi.mock('@/services', () => ({
  BinderService: vi.fn(),
}));

const mockAppearanceRequests: CriminalDocumentAppearanceRequest[] = [
  {
    fileNumber: 'FN1',
    fullName: 'John Doe',
    appearance: {
      physicalFileId: 'F1',
      participantId: 'P1',
      appearanceId: 'APP1',
      courtClassCd: 'CLS1',
    },
  },
  {
    fileNumber: 'FN1',
    fullName: 'Jane Doe',
    appearance: {
      physicalFileId: 'F2',
      participantId: 'P2',
      appearanceId: 'APP2',
      courtClassCd: 'CLS2',
    },
  },
  {
    fileNumber: 'FN2',
    fullName: 'Alice Smith',
    appearance: {
      physicalFileId: 'F3',
      participantId: 'P3',
      appearanceId: 'APP3',
      courtClassCd: 'CLS3',
    },
  },
];

const mockKeyDocumentStore = {
  getAppearanceRequests: mockAppearanceRequests,
  clearBundles: vi.fn(),
};

const mockBinderService = {
  generateBinderPDF: vi.fn(),
};

const mockApiResponse: ApiResponse<any> = {
  payload: {
    pdfResponse: {
      base64Pdf: 'base64string',
      pageRanges: [{ start: 1, end: 2 }, { start: 3 }, { start: 4, end: 5 }],
    },
    binders: [
      {
        labels: {
          physicalFileId: 'F1',
          participantId: 'P1',
        },
        documents: [
          {
            documentId: '1',
            fileName: 'Doc1.pdf',
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
            documentId: '2',
            fileName: 'Doc2.pdf',
            documentType: 'PDF',
          },
        ],
      },
      {
        labels: {
          physicalFileId: 'F3',
          participantId: 'P3',
        },
        documents: [
          {
            documentId: '3',
            fileName: 'Doc3.pdf',
            documentType: 'PDF',
          },
        ],
      },
    ],
  },
  succeeded: true,
  errors: [],
};

describe('CriminalDocumentPDFStrategy', () => {
  beforeEach(() => {
    (useCriminalDocumentBundleStore as any).mockReturnValue(
      mockKeyDocumentStore
    );
    (inject as any).mockImplementation((key: string) => {
      if (key === 'binderService') return mockBinderService;
      return undefined;
    });
    mockKeyDocumentStore.clearBundles.mockClear();
    mockBinderService.generateBinderPDF.mockClear();
  });

  it('throws error if BinderService is not injected', () => {
    (inject as any).mockReturnValueOnce(undefined);
    expect(() => new CriminalDocumentPDFStrategy()).toThrow(
      'BinderService is not available!'
    );
  });

  it('hasData returns true if appearance requests exist', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    expect(strategy.hasData()).toBe(true);
  });

  it('getRawData groups appearance requests by fileNumber and fullName', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const rawData = strategy.getRawData();
    expect(rawData['FN1']['John Doe'][0]).toEqual(mockAppearanceRequests[0]);
    expect(rawData['FN1']['Jane Doe'][0]).toEqual(mockAppearanceRequests[1]);
    expect(rawData['FN2']['Alice Smith'][0]).toEqual(mockAppearanceRequests[2]);
  });

  it('processDataForAPI flattens appearances', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const rawData = strategy.getRawData();
    const result = strategy.processDataForAPI(rawData);
    expect(result.appearances.length).toBe(3);
    expect(result.appearances[0]).toEqual(mockAppearanceRequests[0].appearance);
  });

  it('generatePDF calls binderService.generateBinderPDF', async () => {
    const strategy = new CriminalDocumentPDFStrategy();
    mockBinderService.generateBinderPDF.mockResolvedValue('pdf');
    const result = await strategy.generatePDF({ appearances: [] });
    expect(mockBinderService.generateBinderPDF).toHaveBeenCalledWith(
      { appearances: [] },
      []
    );
    expect(result).toBe('pdf');
  });

  it('generatePDF passes categories from URL params', async () => {
    const strategy = new CriminalDocumentPDFStrategy();
    mockBinderService.generateBinderPDF.mockResolvedValue('pdf');

    // Mock location.search with category params
    Object.defineProperty(globalThis, 'location', {
      value: { search: '?category=INITIATING,ROP' },
      writable: true,
    });

    await strategy.generatePDF({ appearances: [] });
    expect(mockBinderService.generateBinderPDF).toHaveBeenCalledWith(
      { appearances: [] },
      ['INITIATING', 'ROP']
    );
  });

  it('extractBase64PDF returns base64Pdf from response', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const base64 = strategy.extractBase64PDF(mockApiResponse);
    expect(base64).toBe('base64string');
  });

  it('extractPageRanges returns pageRanges from response', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const ranges = strategy.extractPageRanges(mockApiResponse);
    expect(ranges).toEqual([
      { start: 1, end: 2 },
      { start: 3 },
      { start: 4, end: 5 },
    ]);
  });

  it('createOutline creates outline structure from rawData and apiResponse', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const rawData = strategy.getRawData();
    const outline = strategy.createOutline(rawData, mockApiResponse);
    expect(outline.length).toBe(2); // FN1 and FN2
    expect(outline[0].title).toBe('FN1');
    expect(outline[0]?.children?.length).toBe(2); // John Doe, Jane Doe
    expect(outline[1].title).toBe('FN2');
    expect(outline[1]?.children?.length).toBe(1); // Alice Smith
    expect(outline[0]?.children?.[0]?.children?.[0]?.title).toBe('Doc1.pdf');
    expect(outline[0]?.children?.[1]?.children?.[0]?.title).toBe('Doc2.pdf');
    expect(outline[1]?.children?.[0]?.children?.[0]?.title).toBe('Doc3.pdf');
  });

  it('cleanup calls bundleStore.clearBundles', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    strategy.cleanup();
    expect(mockKeyDocumentStore.clearBundles).toHaveBeenCalled();
  });

  it('makeDocElement returns correct OutlineItem', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    (strategy as any).count = 1;
    const doc = {
      documentId: '123',
      fileName: 'TestDoc.pdf',
      documentType: 'File' as any,
    } as BinderDocument;
    const apiResponse = {
      payload: {
        pdfResponse: {
          pageRanges: [{ start: 10 }, { start: 20 }],
        },
      },
    };
    const item = (strategy as any).makeDocElement(doc, apiResponse);
    expect(item.title).toBe('TestDoc.pdf');
    expect(item.pageIndex).toBe(20);
  });
});
