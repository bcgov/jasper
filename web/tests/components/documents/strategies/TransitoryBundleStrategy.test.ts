import { TransitoryBundleStrategy } from '@/components/documents/strategies/TransitoryBundleStrategy';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { FileMetadataDto } from '@/types/transitory-documents';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockTransitoryDocumentsService: Pick<
  TransitoryDocumentsService,
  'mergePdfs'
> = {
  mergePdfs: vi.fn(),
};

describe('TransitoryBundleStrategy', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('wraps embedded outline items under the matching document header', () => {
    const rawData: FileMetadataDto[] = [
      {
        fileName: 'Document A.pdf',
        extension: 'pdf',
        sizeBytes: 100,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'a.pdf',
        matchedRoomFolder: null,
      },
      {
        fileName: 'Document B.pdf',
        extension: 'pdf',
        sizeBytes: 200,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'b.pdf',
        matchedRoomFolder: null,
      },
    ];

    sessionStorage.setItem(
      'transitoryDocuments:test-key',
      JSON.stringify({ files: rawData, context: undefined })
    );

    const strategy = new TransitoryBundleStrategy(
      mockTransitoryDocumentsService,
      'test-key'
    );

    const outline = strategy.createOutlineWithEmbeddedOutline(
      rawData,
      {
        base64Pdf: 'base64pdf',
        pageRanges: [
          { start: 0, end: 2 },
          { start: 2, end: 4 },
        ],
      },
      [
        {
          title: 'Section A',
          pageIndex: 0,
          children: [
            {
              title: 'Section A.1',
              pageIndex: 1,
            },
          ],
        },
        {
          title: 'Section B',
          pageIndex: 2,
        },
      ]
    );

    expect(outline).toEqual([
      {
        title: 'Document A.pdf',
        pageIndex: 0,
        isExpanded: true,
        children: [
          {
            title: 'Section A',
            pageIndex: 0,
            isExpanded: true,
            children: [
              {
                title: 'Section A.1',
                pageIndex: 1,
                isExpanded: true,
                children: undefined,
              },
            ],
          },
        ],
      },
      {
        title: 'Document B.pdf',
        pageIndex: 2,
        isExpanded: true,
        children: [
          {
            title: 'Section B',
            pageIndex: 2,
            isExpanded: true,
            children: undefined,
          },
        ],
      },
    ]);
  });

  it('preserves wrapper parents without page targets and drops out-of-range parents with page targets', () => {
    const rawData: FileMetadataDto[] = [
      {
        fileName: 'Document A.pdf',
        extension: 'pdf',
        sizeBytes: 100,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'a.pdf',
        matchedRoomFolder: null,
      },
      {
        fileName: 'Document B.pdf',
        extension: 'pdf',
        sizeBytes: 200,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'b.pdf',
        matchedRoomFolder: null,
      },
    ];

    sessionStorage.setItem(
      'transitoryDocuments:test-key',
      JSON.stringify({ files: rawData, context: undefined })
    );

    const strategy = new TransitoryBundleStrategy(
      mockTransitoryDocumentsService,
      'test-key'
    );

    const outline = strategy.createOutlineWithEmbeddedOutline(
      rawData,
      {
        base64Pdf: 'base64pdf',
        pageRanges: [
          { start: 0, end: 2 },
          { start: 2, end: 5 },
        ],
      },
      [
        {
          title: 'Wrapper Parent',
          children: [
            {
              title: 'In Range Child',
              pageIndex: 3,
            },
          ],
        },
        {
          title: 'Out-of-Range Parent',
          pageIndex: 8,
          children: [
            {
              title: 'Nested In Range Child',
              pageIndex: 4,
            },
          ],
        },
      ]
    );

    expect(outline?.[1]).toEqual({
      title: 'Document B.pdf',
      pageIndex: 2,
      isExpanded: true,
      children: [
        {
          title: 'Wrapper Parent',
          isExpanded: true,
          children: [
            {
              title: 'In Range Child',
              pageIndex: 3,
              isExpanded: true,
              children: undefined,
            },
          ],
        },
        {
          title: 'Nested In Range Child',
          pageIndex: 4,
          isExpanded: true,
          children: undefined,
        },
      ],
    });
  });

  it('uses explicit pageRange end boundaries and keeps the last document open-ended', () => {
    const rawData: FileMetadataDto[] = [
      {
        fileName: 'Document A.pdf',
        extension: 'pdf',
        sizeBytes: 100,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'a.pdf',
        matchedRoomFolder: null,
      },
      {
        fileName: 'Document B.pdf',
        extension: 'pdf',
        sizeBytes: 200,
        createdUtc: '2026-06-04T00:00:00Z',
        relativePath: 'b.pdf',
        matchedRoomFolder: null,
      },
    ];

    sessionStorage.setItem(
      'transitoryDocuments:test-key',
      JSON.stringify({ files: rawData, context: undefined })
    );

    const strategy = new TransitoryBundleStrategy(
      mockTransitoryDocumentsService,
      'test-key'
    );

    const outline = strategy.createOutlineWithEmbeddedOutline(
      rawData,
      {
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 1 }, { start: 5 }],
      },
      [
        {
          title: 'Boundary Item',
          pageIndex: 1,
        },
        {
          title: 'Last Document Item',
          pageIndex: 9,
        },
      ]
    );

    expect(outline?.[0]?.children).toBeUndefined();
    expect(outline?.[1]).toEqual({
      title: 'Document B.pdf',
      pageIndex: 5,
      isExpanded: true,
      children: [
        {
          title: 'Last Document Item',
          pageIndex: 9,
          isExpanded: true,
          children: undefined,
        },
      ],
    });
  });
});
