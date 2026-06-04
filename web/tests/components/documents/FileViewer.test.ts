import FileViewer from '@/components/documents/FileViewer.vue';
import { useCommonStore } from '@/stores';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/stores', () => ({
  useCommonStore: vi.fn(),
}));

vi.mock('@/components/documents/ReviewModal.vue', () => ({
  default: {
    name: 'ReviewModal',
    template: '<div />',
  },
}));

const mockedUseCommonStore = vi.mocked(useCommonStore);
const globalWithNutrientViewer = globalThis as typeof globalThis & {
  NutrientViewer: any;
};

describe('FileViewer.vue', () => {
  const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
  const mockInstance = {
    getDocumentOutline: vi.fn(),
    setDocumentOutline: vi.fn(),
    setViewState: vi.fn((callback) => callback({ set: vi.fn() })),
    setToolbarItems: vi.fn(),
    addEventListener: vi.fn(),
    getAnnotations: vi.fn().mockResolvedValue({
      filter: () => ({ size: 0 }),
    }),
    totalPageCount: 0,
  };

  const mockOrderService = {};

  beforeEach(() => {
    mockedUseCommonStore.mockReturnValue({
      appInfo: { nutrientFeLicenseKey: 'license-key' },
    } as ReturnType<typeof useCommonStore>);

    mockInstance.setDocumentOutline.mockClear();
    mockInstance.setViewState.mockClear();
    mockInstance.setToolbarItems.mockClear();
    mockInstance.addEventListener.mockClear();
    mockInstance.getAnnotations.mockClear();
    mockInstance.getDocumentOutline.mockReset();
    mockInstance.getDocumentOutline.mockResolvedValue([]);
    warnSpy.mockClear();

    globalWithNutrientViewer.NutrientViewer = {
      load: vi.fn().mockResolvedValue(mockInstance),
      unload: vi.fn(),
      SidebarMode: { DOCUMENT_OUTLINE: 'DOCUMENT_OUTLINE' },
      Actions: {
        GoToAction: class {
          constructor(public readonly config: unknown) {}
        },
      },
      OutlineElement: class {
        constructor(public readonly config: unknown) {}
      },
      Immutable: {
        List: (items: unknown[]) => items,
      },
    };
  });

  it('clears embedded outline for non-transitory strategies when no custom outline is returned', async () => {
    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'embedded-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 1, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 1, end: 2 }],
      createOutline: () => undefined,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(mockInstance.setDocumentOutline).toHaveBeenCalledWith([]);
    expect(mockInstance.setViewState).toHaveBeenCalled();
  });

  it('does not override embedded outline when embedded-outline-aware strategy returns no custom outline', async () => {
    const createOutlineWithEmbeddedOutline = vi.fn().mockReturnValue(undefined);

    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'embedded-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 1, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 1, end: 2 }],
      createOutline: vi.fn(),
      createOutlineWithEmbeddedOutline,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(createOutlineWithEmbeddedOutline).toHaveBeenCalledOnce();
    expect(mockInstance.setDocumentOutline).not.toHaveBeenCalled();
  });

  it('passes embedded Nutrient outline to the strategy before overriding it', async () => {
    const createOutlineWithEmbeddedOutline = vi
      .fn()
      .mockReturnValue([{ title: 'wrapped-document', pageIndex: 0 }]);

    mockInstance.getDocumentOutline.mockResolvedValue([
      {
        title: 'existing-outline-item',
        isExpanded: true,
        action: { pageIndex: 2 },
        children: [],
      },
    ]);

    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'embedded-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 2 }],
      createOutline: vi.fn(),
      createOutlineWithEmbeddedOutline,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(createOutlineWithEmbeddedOutline).toHaveBeenCalledWith(
      [{ fileName: 'embedded-outline.pdf' }],
      { base64Pdf: 'base64pdf', pageRanges: [{ start: 0, end: 2 }] },
      [
        {
          title: 'existing-outline-item',
          pageIndex: 2,
          isExpanded: true,
          children: undefined,
        },
      ]
    );
    expect(mockInstance.setDocumentOutline).toHaveBeenCalledOnce();
  });

  it('does not read embedded outline for non-transitory strategies', async () => {
    const createOutline = vi
      .fn()
      .mockReturnValue([{ title: 'normal-outline', pageIndex: 0 }]);

    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'normal-document.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 2 }],
      createOutline,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(mockInstance.getDocumentOutline).not.toHaveBeenCalled();
    expect(createOutline).toHaveBeenCalledWith(
      [{ fileName: 'normal-document.pdf' }],
      { base64Pdf: 'base64pdf', pageRanges: [{ start: 0, end: 2 }] }
    );
  });

  it('falls back when embedded outline extraction fails for transitory strategies', async () => {
    const createOutlineWithEmbeddedOutline = vi.fn().mockReturnValue(undefined);

    mockInstance.getDocumentOutline.mockRejectedValue(
      new Error('outline load failed')
    );

    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'embedded-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 2 }],
      createOutline: vi.fn(),
      createOutlineWithEmbeddedOutline,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(createOutlineWithEmbeddedOutline).toHaveBeenCalledWith(
      [{ fileName: 'embedded-outline.pdf' }],
      { base64Pdf: 'base64pdf', pageRanges: [{ start: 0, end: 2 }] },
      undefined
    );
    expect(warnSpy).toHaveBeenCalledWith(
      'Failed to extract embedded outline; continuing without embedded outline.',
      expect.any(Error)
    );
  });

  it('falls back when embedded outline data is malformed for transitory strategies', async () => {
    const createOutlineWithEmbeddedOutline = vi.fn().mockReturnValue(undefined);

    mockInstance.getDocumentOutline.mockResolvedValue([{ title: 123 }]);

    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'embedded-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 2 }],
      createOutline: vi.fn(),
      createOutlineWithEmbeddedOutline,
      cleanup: vi.fn(),
    };

    mount(FileViewer, {
      props: { strategy },
      global: {
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    expect(createOutlineWithEmbeddedOutline).toHaveBeenCalledWith(
      [{ fileName: 'embedded-outline.pdf' }],
      { base64Pdf: 'base64pdf', pageRanges: [{ start: 0, end: 2 }] },
      undefined
    );
    expect(warnSpy).toHaveBeenCalledWith(
      'Failed to extract embedded outline; continuing without embedded outline.',
      expect.any(TypeError)
    );
  });
});
