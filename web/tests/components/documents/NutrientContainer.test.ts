import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import NutrientContainer from 'CMP/documents/NutrientContainer.vue';
import { setActivePinia, createPinia } from 'pinia'
import { FilesService } from '@/services/FilesService';

const mockDocuments = [{ test: "1" }, { test: "2" }, { test: "3" }];
const mockLoad = vi.fn().mockImplementation(() => ({setDocumentOutline: vi.fn().mockImplementation(() => ({}))}));
const mockUnload = vi.fn();
let filesService: any;
let mockResponse: any = {
  base64Pdf: 'testBase64String',
};

// Mock store
vi.mock('@/stores/PDFViewerStore', () => {
  return {
    usePDFViewerStore: vi.fn(() => ({
      get documents() {
        return mockDocuments;
      },
      clearDocuments: vi.fn(),
    })),
  };
});
vi.mock('@/services/FilesService');

globalThis.NutrientViewer = {
  ViewState: vi.fn().mockImplementation(() => ({})),
  SidebarMode: { THUMBNAILS: 'thumbnails' },
  Immutable: {
    List: vi.fn().mockImplementation((items: any[]) => items),
  },
  Actions: {
    GoToAction: vi.fn().mockImplementation(() => ({})),
  },
  OutlineElement: vi.fn().mockImplementation(() => ({})),
  load: mockLoad,
  unload: mockUnload
};

describe('NutrientContainer.vue', () => {
  let wrapper: any;

  beforeEach(() => {
    vi.clearAllMocks();
    setActivePinia(createPinia());
    filesService = {
      generatePdf: vi.fn().mockResolvedValue(mockResponse)
    };

    // Mock global fetch to avoid real network requests
    globalThis.fetch = vi.fn().mockResolvedValue({
      arrayBuffer: () => Promise.resolve(new ArrayBuffer(8)),
      blob: () => Promise.resolve(new Blob([new ArrayBuffer(8)], { type: 'application/pdf' })),
    });

    // Mock NutrientViewer.load to resolve with a fake instance
    (globalThis.NutrientViewer.load as any).mockImplementation(({ document }: { document?: string | ArrayBuffer } = {}) => {
      const mergedInstance = {
        totalPageCount: typeof document !== 'string' && document instanceof ArrayBuffer ? 3 : 2,
        exportPDFWithOperations: vi.fn().mockResolvedValue('mergedDocument'),
        setDocumentOutline: vi.fn()
      };
      return Promise.resolve(mergedInstance);
    });

    wrapper = mount(NutrientContainer, {
      global: {
        stubs: ['v-progress-linear', 'v-skeleton-loader'],
        provide: {
          filesService
        }
      },
    });
  });

  afterEach(() => {
    wrapper.unmount();
  });

  it('does not render loaders when fully mounted', async () => {
    await flushPromises();
    expect(wrapper.findComponent({ name: 'v-progress-linear' }).exists()).toBe(false);
    expect(wrapper.findComponent({ name: 'v-skeleton-loader' }).exists()).toBe(false);
  });

  it('unloads NutrientViewer on unmount', async () => {
    await flushPromises();
    wrapper.unmount();
    expect(mockUnload).toHaveBeenCalled();
  });

  it('expect load to be called with correct b64 content', () => {
    expect(globalThis.NutrientViewer.load).toHaveBeenCalledWith(
      expect.objectContaining({ document: `data:application/pdf;base64,${mockResponse.base64Pdf}` })
    );
  });

  it('shows pdf-container after loading', async () => {
    await flushPromises();
    expect(wrapper.find('.pdf-container').isVisible()).toBe(true);
  });
});