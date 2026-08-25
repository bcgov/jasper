import FileViewer from '@/components/documents/FileViewer.vue';
import { useCommonStore } from '@/stores';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  createMemoryHistory,
  createRouter,
  Router,
  RouteRecordRaw,
} from 'vue-router';

vi.mock('@/stores', () => ({
  useCommonStore: vi.fn(),
}));

vi.mock('@/components/documents/ReviewModal.vue', () => ({
  default: {
    name: 'ReviewModal',
    props: ['modelValue', 'canApprove', 'reviewOrder'],
    template: '<div />',
  },
}));

const mockedUseCommonStore = vi.mocked(useCommonStore);
const globalWithNutrientViewer = globalThis as typeof globalThis & {
  NutrientViewer: any;
};

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: { template: '<div />' },
  },
];

describe('FileViewer.vue', () => {
  const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
  let toolbarItems: any[] = [];
  const mockInstance = {
    getDocumentOutline: vi.fn(),
    setDocumentOutline: vi.fn(),
    setViewState: vi.fn((callback) => callback({ set: vi.fn() })),
    setToolbarItems: vi.fn((callback) => {
      toolbarItems = callback([]);
      return toolbarItems;
    }),
    setAnnotationPresets: vi.fn((callback) => callback({})),
    addEventListener: vi.fn(),
    exportPDF: vi.fn(),
    getAnnotations: vi.fn().mockResolvedValue({
      filter: () => ({ size: 0 }),
    }),
    totalPageCount: 0,
  };

  const mockOrderService = {};

  const createRouterForQuery = async (
    query: Record<string, string> = {}
  ): Promise<Router> => {
    const queryString = new URLSearchParams(query).toString();
    globalThis.history.replaceState(
      {},
      '',
      queryString ? `/?${queryString}` : '/'
    );

    const router = createRouter({
      history: createMemoryHistory(),
      routes,
    });

    await router.push({ path: '/', query });
    await router.isReady();

    return router;
  };

  const mountViewer = async (
    strategy: any,
    query: Record<string, string> = {}
  ) => {
    const router = await createRouterForQuery(query);

    const wrapper = mount(FileViewer, {
      props: { strategy },
      global: {
        plugins: [router],
        provide: {
          orderService: mockOrderService,
        },
      },
    });

    await flushPromises();

    return wrapper;
  };

  beforeEach(() => {
    mockedUseCommonStore.mockReturnValue({
      appInfo: { nutrientFeLicenseKey: 'license-key' },
    } as ReturnType<typeof useCommonStore>);

    mockInstance.setDocumentOutline.mockClear();
    mockInstance.setViewState.mockClear();
    mockInstance.setToolbarItems.mockClear();
    mockInstance.setAnnotationPresets.mockClear();
    mockInstance.addEventListener.mockClear();
    mockInstance.getAnnotations.mockClear();
    mockInstance.getAnnotations.mockResolvedValue({
      filter: () => ({ size: 0 }),
    });
    mockInstance.exportPDF.mockClear();
    mockInstance.totalPageCount = 0;
    mockInstance.getDocumentOutline.mockReset();
    mockInstance.getDocumentOutline.mockResolvedValue([]);
    warnSpy.mockClear();
    toolbarItems = [];
    vi.spyOn(window, 'open').mockImplementation(() => null);

    globalWithNutrientViewer.NutrientViewer = {
      load: vi.fn().mockResolvedValue(mockInstance),
      unload: vi.fn(),
      SidebarMode: { DOCUMENT_OUTLINE: 'DOCUMENT_OUTLINE' },
      Color: { RED: 'RED' },
      Actions: {
        GoToAction: class {
          constructor(public readonly config: unknown) {}
        },
      },
      OutlineElement: class {
        constructor(public readonly config: unknown) {}
      },
      Annotations: {
        ImageAnnotation: class {
          constructor(config: Record<string, unknown>) {
            Object.assign(this, config);
          }
        },
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

    await mountViewer(strategy);

    expect(mockInstance.setDocumentOutline).toHaveBeenCalledWith([]);
    expect(mockInstance.setViewState).toHaveBeenCalled();
  });

  it('keeps viewer strategy calls isolated across multiple mounts', async () => {
    const firstStrategy = {
      hasData: vi.fn(() => true),
      getRawData: vi.fn(() => [{ fileName: 'session-a.pdf' }]),
      processDataForAPI: vi.fn((rawData: unknown) => rawData),
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf-a',
        pageRanges: [{ start: 0, end: 1 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 1 }],
      createOutline: vi.fn().mockReturnValue([]),
      cleanup: vi.fn(),
    };

    const firstWrapper = await mountViewer(firstStrategy, {
      sessionId: 'session-a',
    });
    firstWrapper.unmount();

    expect(firstStrategy.hasData).toHaveBeenCalledOnce();
    expect(firstStrategy.getRawData).toHaveBeenCalledOnce();
    expect(firstStrategy.processDataForAPI).toHaveBeenCalledWith([
      { fileName: 'session-a.pdf' },
    ]);
    expect(firstStrategy.createOutline).toHaveBeenCalledWith(
      [{ fileName: 'session-a.pdf' }],
      { base64Pdf: 'base64pdf-a', pageRanges: [{ start: 0, end: 1 }] }
    );
    expect(firstStrategy.cleanup).toHaveBeenCalledOnce();

    const secondStrategy = {
      hasData: vi.fn(() => true),
      getRawData: vi.fn(() => [{ fileName: 'session-b.pdf' }]),
      processDataForAPI: vi.fn((rawData: unknown) => rawData),
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf-b',
        pageRanges: [{ start: 1, end: 2 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 1, end: 2 }],
      createOutline: vi.fn().mockReturnValue([]),
      cleanup: vi.fn(),
    };

    const secondWrapper = await mountViewer(secondStrategy, {
      sessionId: 'session-b',
    });
    secondWrapper.unmount();

    expect(secondStrategy.hasData).toHaveBeenCalledOnce();
    expect(secondStrategy.getRawData).toHaveBeenCalledOnce();
    expect(secondStrategy.processDataForAPI).toHaveBeenCalledWith([
      { fileName: 'session-b.pdf' },
    ]);
    expect(secondStrategy.createOutline).toHaveBeenCalledWith(
      [{ fileName: 'session-b.pdf' }],
      { base64Pdf: 'base64pdf-b', pageRanges: [{ start: 1, end: 2 }] }
    );
    expect(secondStrategy.cleanup).toHaveBeenCalledOnce();
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

    await mountViewer(strategy);

    expect(mockInstance.getDocumentOutline).not.toHaveBeenCalled();
    expect(createOutline).toHaveBeenCalledWith(
      [{ fileName: 'normal-document.pdf' }],
      { base64Pdf: 'base64pdf', pageRanges: [{ start: 0, end: 2 }] }
    );
  });

  it('expands every level of a custom outline by default', async () => {
    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 3 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 3 }],
      createOutline: () => [
        {
          title: 'Level one',
          children: [
            {
              title: 'Level two',
              children: [{ title: 'Level three', pageIndex: 2 }],
            },
          ],
        },
      ],
      cleanup: vi.fn(),
    };

    await mountViewer(strategy);

    const outline = mockInstance.setDocumentOutline.mock.calls[0][0];
    expect(outline[0].config.isExpanded).toBe(true);
    expect(outline[0].config.children[0].config.isExpanded).toBe(true);
    expect(
      outline[0].config.children[0].config.children[0].config.isExpanded
    ).toBe(true);
  });

  it('expands every level from a collapsed embedded source outline', async () => {
    const strategy = {
      hasData: () => true,
      getRawData: () => [{ fileName: 'source-outline.pdf' }],
      processDataForAPI: (rawData: unknown) => rawData,
      generatePDF: vi.fn().mockResolvedValue({
        base64Pdf: 'base64pdf',
        pageRanges: [{ start: 0, end: 3 }],
      }),
      extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
        apiResponse.base64Pdf,
      extractPageRanges: () => [{ start: 0, end: 3 }],
      createOutline: vi.fn(),
      createOutlineWithEmbeddedOutline: vi.fn(
        (_rawData: unknown, _apiResponse: unknown, embeddedOutline) =>
          embeddedOutline
      ),
      cleanup: vi.fn(),
    };

    mockInstance.getDocumentOutline.mockResolvedValue([
      {
        title: 'Source level one',
        isExpanded: false,
        children: [
          {
            title: 'Source level two',
            isExpanded: false,
            action: { pageIndex: 1 },
            children: [],
          },
        ],
      },
    ]);

    await mountViewer(strategy);

    const outline = mockInstance.setDocumentOutline.mock.calls[0][0];
    expect(outline[0].config.isExpanded).toBe(true);
    expect(outline[0].config.children[0].config.isExpanded).toBe(true);
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

    await mountViewer(strategy);

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

    await mountViewer(strategy);

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

    await mountViewer(strategy);

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

    await mountViewer(strategy);

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

  const createBaseStrategy = (
    rawData: unknown,
    overrides: Record<string, unknown> = {}
  ) => ({
    hasData: () => true,
    getRawData: () => rawData,
    processDataForAPI: (data: unknown) => data,
    generatePDF: vi.fn().mockResolvedValue({
      base64Pdf: 'base64pdf',
      pageRanges: [{ start: 0, end: 1 }],
    }),
    extractBase64PDF: (apiResponse: { base64Pdf: string }) =>
      apiResponse.base64Pdf,
    extractPageRanges: () => [{ start: 0, end: 1 }],
    createOutline: () => [],
    cleanup: vi.fn(),
    ...overrides,
  });

  it('delegates toolbar construction to the strategy with a viewer context', async () => {
    const rawData = [{ fileName: 'doc.pdf' }];
    const setToolbarItems = vi.fn(
      (items: unknown[], _context: unknown) => items
    );
    const strategy = createBaseStrategy(rawData, { setToolbarItems });

    await mountViewer(strategy);

    expect(setToolbarItems).toHaveBeenCalledTimes(1);
    const context = setToolbarItems.mock.calls[0][1] as Record<string, unknown>;
    expect(context.instance).toBe(mockInstance);
    expect(context.nutrientViewer).toBe(
      globalWithNutrientViewer.NutrientViewer
    );
    expect(context.rawData).toEqual(rawData);
    expect(typeof context.resolveInformationContext).toBe('function');
    expect(typeof context.openReviewModal).toBe('function');
    expect(typeof context.updateCanApprove).toBe('function');
  });

  it('inserts the default toolbar items when the strategy does not customize them', async () => {
    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }]);

    await mountViewer(strategy);

    expect(mockInstance.setToolbarItems).toHaveBeenCalledTimes(1);
    expect(toolbarItems).toEqual([
      { type: 'line' },
      { type: 'arrow' },
      { type: 'rectangle' },
      { type: 'ellipse' },
      { type: 'polygon' },
      { type: 'cloudy-polygon' },
      { type: 'polyline' },
      { type: 'ink' },
      { type: 'highlighter' },
      { type: 'text-highlighter' },
      { type: 'ink-eraser' },
      { type: 'content-editor' },
      { type: 'search' },
      { type: 'export-pdf' },
    ]);
  });

  it('opens the review modal when the toolbar context requests it', async () => {
    let capturedContext: any;
    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }], {
      setToolbarItems: (items: unknown[], context: unknown) => {
        capturedContext = context;
        return items;
      },
    });

    const wrapper = await mountViewer(strategy);

    expect(
      wrapper.findComponent({ name: 'ReviewModal' }).props('modelValue')
    ).toBe(false);

    capturedContext.openReviewModal();
    await flushPromises();

    expect(
      wrapper.findComponent({ name: 'ReviewModal' }).props('modelValue')
    ).toBe(true);
  });

  it('resolves information context from flat StoreDocument raw data', async () => {
    const rawData = [
      {
        physicalFileId: 'civil-file-123',
        request: {
          data: {
            isCriminal: false,
          },
        },
      },
    ];
    let capturedContext: any;
    const strategy = createBaseStrategy(rawData, {
      setToolbarItems: (items: unknown[], context: unknown) => {
        capturedContext = context;
        return items;
      },
    });

    await mountViewer(strategy);

    expect(capturedContext.resolveInformationContext(rawData)).toEqual({
      physicalFileId: 'civil-file-123',
      isCriminal: false,
    });
  });

  it('resolves information context from criminal bundle raw data', async () => {
    const rawData = [
      {
        appearance: {
          physicalFileId: 'criminal-file-123',
        },
      },
    ];
    let capturedContext: any;
    const strategy = createBaseStrategy(rawData, {
      setToolbarItems: (items: unknown[], context: unknown) => {
        capturedContext = context;
        return items;
      },
    });

    await mountViewer(strategy);

    expect(capturedContext.resolveInformationContext(rawData)).toEqual({
      physicalFileId: 'criminal-file-123',
      isCriminal: true,
    });
  });

  it('can approve when a required approval annotation is present', async () => {
    mockInstance.totalPageCount = 1;
    mockInstance.getAnnotations.mockResolvedValue({
      filter: (predicate: (annotation: unknown) => boolean) => ({
        size: [
          new globalWithNutrientViewer.NutrientViewer.Annotations.ImageAnnotation(
            { contentType: 'image/png', description: 'Signature' }
          ),
        ].filter(predicate).length,
      }),
    });

    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }], {
      getRequiredApprovalAnnotations: () => ['Signature'],
    });

    const wrapper = await mountViewer(strategy);

    expect(
      wrapper.findComponent({ name: 'ReviewModal' }).props('canApprove')
    ).toBe(true);
  });

  it('cannot approve when the annotation does not match a required description', async () => {
    mockInstance.totalPageCount = 1;
    mockInstance.getAnnotations.mockResolvedValue({
      filter: (predicate: (annotation: unknown) => boolean) => ({
        size: [
          new globalWithNutrientViewer.NutrientViewer.Annotations.ImageAnnotation(
            { contentType: 'image/png', description: 'Initials' }
          ),
        ].filter(predicate).length,
      }),
    });

    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }], {
      getRequiredApprovalAnnotations: () => ['Signature'],
    });

    const wrapper = await mountViewer(strategy);

    expect(
      wrapper.findComponent({ name: 'ReviewModal' }).props('canApprove')
    ).toBe(false);
  });

  it('can approve on any image annotation when no descriptions are required', async () => {
    mockInstance.totalPageCount = 1;
    mockInstance.getAnnotations.mockResolvedValue({
      filter: (predicate: (annotation: unknown) => boolean) => ({
        size: [
          new globalWithNutrientViewer.NutrientViewer.Annotations.ImageAnnotation(
            { contentType: 'image/png', description: 'Anything' }
          ),
        ].filter(predicate).length,
      }),
    });

    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }]);

    const wrapper = await mountViewer(strategy);

    expect(
      wrapper.findComponent({ name: 'ReviewModal' }).props('canApprove')
    ).toBe(true);
  });

  it('registers annotation change listeners to refresh the approval state', async () => {
    const strategy = createBaseStrategy([{ fileName: 'doc.pdf' }]);

    await mountViewer(strategy);

    expect(mockInstance.addEventListener).toHaveBeenCalledWith(
      'annotations.create',
      expect.any(Function)
    );
    expect(mockInstance.addEventListener).toHaveBeenCalledWith(
      'annotations.update',
      expect.any(Function)
    );
    expect(mockInstance.addEventListener).toHaveBeenCalledWith(
      'annotations.delete',
      expect.any(Function)
    );
  });
});
