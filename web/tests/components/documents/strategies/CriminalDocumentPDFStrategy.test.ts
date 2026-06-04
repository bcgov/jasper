import { CriminalDocumentPDFStrategy } from '@/components/documents/strategies/CriminalDocumentPDFStrategy';
import { useCriminalDocumentBundleStore } from '@/stores';
import { CriminalDocumentAppearanceRequest } from '@/stores/CriminalDocumentBundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { inject } from 'vue';

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
    groupKeyOne: 'FN1',
    groupKeyTwo: 'John Doe',
    documentName: 'John Doe',
    physicalFileId: 'F1',
    participantId: 'P1',
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
    groupKeyOne: 'FN1',
    groupKeyTwo: 'Jane Doe',
    documentName: 'Jane Doe',
    physicalFileId: 'F2',
    participantId: 'P2',
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
    groupKeyOne: 'FN2',
    groupKeyTwo: 'Alice Smith',
    documentName: 'Alice Smith',
    physicalFileId: 'F3',
    participantId: 'P3',
    appearance: {
      physicalFileId: 'F3',
      participantId: 'P3',
      appearanceId: 'APP3',
      courtClassCd: 'CLS3',
    },
  },
];

const mockKeyDocumentStore = {
  hasPdfData: vi.fn(() => true),
  getPdfItems: vi.fn(() => mockAppearanceRequests),
  clearPdfItems: vi.fn(),
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
    mockKeyDocumentStore.hasPdfData.mockImplementation(() => true);
    mockKeyDocumentStore.getPdfItems.mockImplementation(
      () => mockAppearanceRequests
    );
    mockKeyDocumentStore.clearPdfItems.mockClear();
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

  it('getRawData returns appearance requests from the active session', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const rawData = strategy.getRawData();
    expect(rawData).toEqual(mockAppearanceRequests);
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
    expect(mockKeyDocumentStore.clearPdfItems).toHaveBeenCalled();
  });

  it('maps page ranges by backend document identity instead of rawData position', () => {
    const strategy = new CriminalDocumentPDFStrategy();
    const rawData = [mockAppearanceRequests[2], mockAppearanceRequests[0]];

    const outline = strategy.createOutline(rawData, mockApiResponse);

    expect(outline[0]?.title).toBe('FN2');
    expect(outline[0]?.children?.[0]?.children?.[0]?.pageIndex).toBe(4);
    expect(outline[1]?.title).toBe('FN1');
    expect(outline[1]?.children?.[0]?.children?.[0]?.pageIndex).toBe(1);
  });
});
