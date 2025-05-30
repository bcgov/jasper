import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import NutrientContainer from 'CMP/pdf/NutrientContainer.vue';
import { setActivePinia, createPinia } from 'pinia'

const mockDocumentUrls = ['url1.pdf', 'url2.pdf', 'url3.pdf'];

const mockLoad = vi.fn();
const mockUnload = vi.fn();

// Mock store
vi.mock('@/stores/PDFViewerStore', () => {
  return {
    usePDFViewerStore: vi.fn(() => ({
      get documentUrls() {
        return mockDocumentUrls;
      },
      clearUrls: vi.fn(),
    })),
  };
});

globalThis.NutrientViewer = {
  ViewState: vi.fn().mockImplementation(() => ({})),
  SidebarMode: { THUMBNAILS: 'thumbnails' },
  load: mockLoad,
  unload: mockUnload
};

describe('NutrientContainer.vue', () => {
  let wrapper: any;

  beforeEach(() => {
    vi.clearAllMocks();
    setActivePinia(createPinia());

    // Mock global fetch to avoid real network requests
    globalThis.fetch = vi.fn().mockResolvedValue({
      arrayBuffer: () => Promise.resolve(new ArrayBuffer(8)),
      blob: () => Promise.resolve(new Blob([new ArrayBuffer(8)], { type: 'application/pdf' })),
    });

    // Mock NutrientViewer.load to resolve with a fake instance
    (globalThis.NutrientViewer.load as any).mockImplementation(({ document }: { document?: string | ArrayBuffer } = {}) => {
      if (typeof document !== 'string' && document instanceof ArrayBuffer) {
        return Promise.resolve({ totalPageCount: 3, exportPDFWithOperations: vi.fn().mockResolvedValue('mergedDocument') });
      }
      return Promise.resolve({ totalPageCount: 2, exportPDFWithOperations: vi.fn().mockResolvedValue('mergedDocument') });
    });

    wrapper = mount(NutrientContainer, {
      global: {
        stubs: ['v-progress-linear', 'v-skeleton-loader'],
      },
    });
  });

  afterEach(() => {
    wrapper.unmount();
  });

  it('renders loading indicators initially', async () => {
    expect(wrapper.findComponent({ name: 'v-progress-linear' }).exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'v-skeleton-loader' }).exists()).toBe(true);
  });

  it('expect load to be called initally with first document', () => {
    expect(globalThis.NutrientViewer.load).toHaveBeenCalledWith(
      expect.objectContaining({document: 'url1.pdf'})
    );
    expect.objectContaining({document: 'url1.pdf'});
  });

  it('unloads NutrientViewer on unmount', async () => {
    await flushPromises();
    wrapper.unmount();
    expect(mockUnload).toHaveBeenCalled();
  });

  it('shows pdf-container after loading', async () => {
    await flushPromises();
    expect(wrapper.find('.pdf-container').isVisible()).toBe(true);
  });
});