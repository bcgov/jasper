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
  const mockInstance = {
    getDocumentOutline: vi.fn(),
    setDocumentOutline: vi.fn(),
    setViewState: vi.fn((callback) => callback({ set: vi.fn() })),
    setToolbarItems: vi.fn((callback) => callback([])),
    addEventListener: vi.fn(),
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
      createOutline: () => [],
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
});
