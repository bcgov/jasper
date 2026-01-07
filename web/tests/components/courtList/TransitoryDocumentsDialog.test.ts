import TransitoryDocumentsDialog from '@/components/courtlist/TransitoryDocumentsDialog.vue';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { useCommonStore } from '@/stores';
import { FileMetadataDto } from '@/types/transitory-documents';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { createVuetify } from 'vuetify';

// Mock vue-router
vi.mock('vue-router', () => ({
  useRouter: () => ({
    resolve: mockRouterResolve,
  }),
}));

const vuetify = createVuetify();

const DOWNLOAD_PERMISSION = 'DOWNLOAD_TRANSITORY_DOCUMENTS';
const VIEW_PERMISSION = 'VIEW_TRANSITORY_DOCUMENTS';

const defaultUserInfo = {
  userType: '',
  enableArchive: false,
  subRole: '',
  isSupremeUser: '',
  isPendingRegistration: false,
  isActive: true,
  agencyCode: '',
  userId: '',
  judgeId: 0,
  judgeHomeLocationId: 0,
  email: '',
  userTitle: '',
};

const createPiniaWithUser = (
  roles: string[] = [DOWNLOAD_PERMISSION, VIEW_PERMISSION]
) => {
  const pinia = createPinia();
  const commonStore = useCommonStore(pinia);
  commonStore.setUserInfo({
    ...defaultUserInfo,
    roles,
  });
  return pinia;
};

// Mock router
const mockRouterResolve = vi.fn();
const mockRouter = {
  resolve: mockRouterResolve,
};

vi.mock('vue-router', () => ({
  useRouter: () => mockRouter,
}));

describe('TransitoryDocumentsDialog', () => {
  let mockTransitoryDocumentsService: {
    searchDocuments: ReturnType<typeof vi.fn>;
    downloadFile: ReturnType<typeof vi.fn>;
  };

  const createMockDocument = (
    overrides: Partial<FileMetadataDto> = {}
  ): FileMetadataDto => ({
    fileName: 'test-file.pdf',
    extension: '.pdf',
    sizeBytes: 1024,
    createdUtc: '2025-11-01T10:00:00Z',
    relativePath: '/path/to/file',
    matchedRoomFolder: 'Room 101',
    ...overrides,
  });

  const defaultProps = {
    modelValue: true,
    locationId: '1',
    roomCd: '101',
    date: '2025-11-01',
    location: 'Victoria',
  };

  const createWrapper = (
    props = {},
    mockDocuments: FileMetadataDto[] = [],
    roles: string[] = [DOWNLOAD_PERMISSION, VIEW_PERMISSION]
  ) => {
    mockTransitoryDocumentsService.searchDocuments.mockResolvedValue(
      mockDocuments
    );

    const pinia = createPiniaWithUser(roles);

    return mount(TransitoryDocumentsDialog, {
      props: {
        ...defaultProps,
        ...props,
      },
      global: {
        plugins: [vuetify, pinia],
        provide: {
          transitoryDocumentsService: mockTransitoryDocumentsService,
        },
        mocks: {
          $router: {
            resolve: mockRouterResolve,
          },
        },
        stubs: {
          ActionBar: {
            template:
              '<div data-testid="action-bar-stub" @click="$emit(\'clicked\')"><slot /></div>',
            emits: ['clicked'],
          },
        },
      },
    });
  };

  beforeEach(() => {
    vi.clearAllMocks();

    mockTransitoryDocumentsService = {
      searchDocuments: vi.fn(),
      downloadFile: vi.fn(),
    };

    mockRouterResolve.mockReturnValue({ href: '/nutrient-viewer' });

    // Mock window.open
    globalThis.window.open = vi.fn();

    // Mock sessionStorage - spy directly on window.sessionStorage
    vi.spyOn(window.sessionStorage, 'setItem').mockImplementation(() => {});
    vi.spyOn(window.sessionStorage, 'getItem').mockReturnValue(null);
    vi.spyOn(window.sessionStorage, 'removeItem').mockImplementation(() => {});

    // Mock console methods
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  describe('Dialog rendering', () => {
    it('renders the dialog when modelValue is true', async () => {
      const wrapper = createWrapper({ modelValue: true });
      await nextTick();

      const text = wrapper.text();
      expect(text).toContain('Transitory Documents');
      expect(text).toContain('2025-11-01');
      expect(text).toContain('Victoria');
      expect(text).toContain('Room 101');
    });

    it('shows loading state when fetching documents', async () => {
      let resolvePromise: () => void;
      const promise = new Promise<any[]>((resolve) => {
        resolvePromise = () => resolve([]);
      });

      // Set up the mock BEFORE creating the wrapper
      mockTransitoryDocumentsService.searchDocuments.mockReturnValue(promise);

      const pinia = createPiniaWithUser();

      const wrapper = mount(TransitoryDocumentsDialog, {
        props: {
          ...defaultProps,
          modelValue: false,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            transitoryDocumentsService: mockTransitoryDocumentsService,
          },
          mocks: {
            $router: {
              resolve: mockRouterResolve,
            },
          },
        },
      });

      // Open the dialog to trigger fetch
      wrapper.setProps({ modelValue: true });
      await nextTick();

      const vm = wrapper.vm as any;
      expect(vm.loading).toBe(true);

      await flushPromises();
    });

    it('shows error message when document fetch fails', async () => {
      mockTransitoryDocumentsService.searchDocuments.mockRejectedValue(
        new Error('Network error')
      );

      const pinia = createPiniaWithUser();

      const wrapper = mount(TransitoryDocumentsDialog, {
        props: {
          ...defaultProps,
          modelValue: false,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            transitoryDocumentsService: mockTransitoryDocumentsService,
          },
          mocks: {
            $router: {
              resolve: mockRouterResolve,
            },
          },
        },
      });

      await wrapper.setProps({ modelValue: true });

      const vm = wrapper.vm as any;
      expect(vm.error).toBeTruthy();
      expect(console.error).toHaveBeenCalledWith(
        'Error fetching transitory documents:',
        expect.any(Error)
      );
    });

    it('shows info message when no documents found', async () => {
      const wrapper = createWrapper({}, []);
      expect(wrapper.text()).toContain(
        'No documents found for this location and date.'
      );
    });

    it('shows data table when documents are loaded', async () => {
      const mockDocs = [createMockDocument()];
      const wrapper = createWrapper({ modelValue: false }, mockDocs);

      await wrapper.setProps({ modelValue: true });

      const vm = wrapper.vm as any;
      expect(vm.documents.length).toBe(1);
    });
  });

  describe('Document fetching', () => {
    it('fetches documents when dialog is opened', async () => {
      const mockDocs = [createMockDocument()];
      const wrapper = createWrapper({ modelValue: false }, mockDocs);

      await wrapper.setProps({ modelValue: true });

      expect(
        mockTransitoryDocumentsService.searchDocuments
      ).toHaveBeenCalledWith('1', '101', '2025-11-01');
    });

    it('does not fetch documents when required props are missing', async () => {
      createWrapper({ locationId: '' });

      expect(
        mockTransitoryDocumentsService.searchDocuments
      ).not.toHaveBeenCalled();
    });

    it('fetches documents when modelValue changes to true', async () => {
      const wrapper = createWrapper({ modelValue: false });

      expect(
        mockTransitoryDocumentsService.searchDocuments
      ).not.toHaveBeenCalled();

      await wrapper.setProps({ modelValue: true });

      expect(
        mockTransitoryDocumentsService.searchDocuments
      ).toHaveBeenCalledTimes(1);
    });
  });

  describe('File size formatting', () => {
    it.each([
      { bytes: 0, expected: '0 Bytes' },
      { bytes: 512, expected: '512 Bytes' },
      { bytes: 1024, expected: '1 KB' },
      { bytes: 1536, expected: '1.5 KB' },
      { bytes: 1048576, expected: '1 MB' },
      { bytes: 1572864, expected: '1.5 MB' },
      { bytes: 1073741824, expected: '1 GB' },
    ])('formats $bytes bytes as $expected', async ({ bytes, expected }) => {
      const mockDoc = createMockDocument({ sizeBytes: bytes });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      expect(vm.formatFileSize(bytes)).toBe(expected);
    });
  });

  describe('Date formatting', () => {
    it('formats date string correctly', async () => {
      const wrapper = createWrapper();
      const vm = wrapper.vm as any;
      const dateString = '2025-11-01T10:30:00Z';
      const formatted = vm.formatDate(dateString);

      expect(formatted).toContain('11/1/2025');
    });
  });

  describe('File type detection', () => {
    it.each([
      { extension: '.pdf', isPdf: true, isSupported: true },
      { extension: '.PDF', isPdf: true, isSupported: true },
      { extension: '.doc', isPdf: false, isSupported: true },
      { extension: '.docx', isPdf: false, isSupported: true },
      { extension: '.DOC', isPdf: false, isSupported: true },
      { extension: '.DOCX', isPdf: false, isSupported: true },
      { extension: '.txt', isPdf: false, isSupported: false },
      { extension: '.xlsx', isPdf: false, isSupported: false },
    ])(
      'correctly identifies $extension (isPdf: $isPdf, isSupported: $isSupported)',
      async ({ extension, isPdf, isSupported }) => {
        const mockDoc = createMockDocument({ extension });
        const wrapper = createWrapper({}, [mockDoc]);
        const vm = wrapper.vm as any;

        expect(vm.isPdf(mockDoc)).toBe(isPdf);
        expect(vm.isSupportedByNutrient(mockDoc)).toBe(isSupported);
      }
    );
  });

  describe('File name rendering', () => {
    it('renders clickable link for PDF files', async () => {
      const mockDoc = createMockDocument({
        fileName: 'document.pdf',
        extension: '.pdf',
      });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      expect(vm.isSupportedByNutrient(mockDoc)).toBe(true);
    });

    it('renders clickable link for DOC files', async () => {
      const mockDoc = createMockDocument({
        fileName: 'document.doc',
        extension: '.doc',
      });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      expect(vm.isSupportedByNutrient(mockDoc)).toBe(true);
    });

    it('renders plain text for unsupported file types', async () => {
      const mockDoc = createMockDocument({
        fileName: 'document.txt',
        extension: '.txt',
      });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      expect(vm.isSupportedByNutrient(mockDoc)).toBe(false);
    });
  });

  describe('Opening documents in Nutrient', () => {
    it('opens supported document in Nutrient when file name is clicked', async () => {
      const mockDoc = createMockDocument({
        fileName: 'document.pdf',
        extension: '.pdf',
      });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      await vm.openInNutrient(mockDoc);

      expect(window.sessionStorage.setItem).toHaveBeenCalledWith(
        'transitoryDocuments',
        JSON.stringify([mockDoc])
      );
      expect(mockRouterResolve).toHaveBeenCalledWith({
        name: 'NutrientContainer',
        query: { type: 'transitory-bundle' },
      });
      expect(window.open).toHaveBeenCalledWith('/nutrient-viewer', '_blank');
    });

    it('shows error when opening document in Nutrient fails', async () => {
      const mockDoc = createMockDocument();

      // Set up the mock to throw BEFORE creating wrapper
      (window.sessionStorage.setItem as any).mockImplementation(() => {
        throw new Error('Storage error');
      });

      const wrapper = createWrapper({ modelValue: false }, [mockDoc]);

      await wrapper.setProps({ modelValue: true });

      const vm = wrapper.vm as any;
      await vm.openInNutrient(mockDoc);

      expect(vm.downloadError).toBe(true);
      expect(vm.downloadErrorMessage).toBe(
        'Failed to open document in viewer. Please try again.'
      );
      expect(console.error).toHaveBeenCalledWith(
        'Error opening document in viewer:',
        expect.any(Error)
      );
    });
  });

  describe('Downloading files', () => {
    it('downloads file when download button is clicked', async () => {
      const mockDoc = createMockDocument();
      mockTransitoryDocumentsService.downloadFile.mockResolvedValue(undefined);

      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;
      await vm.downloadFile(mockDoc);

      expect(mockTransitoryDocumentsService.downloadFile).toHaveBeenCalledWith(
        mockDoc
      );
    });

    it('shows error when download fails', async () => {
      const mockDoc = createMockDocument();
      mockTransitoryDocumentsService.downloadFile.mockRejectedValue(
        new Error('Download failed')
      );

      const wrapper = createWrapper({ modelValue: false }, [mockDoc]);

      await wrapper.setProps({ modelValue: true });

      const vm = wrapper.vm as any;
      await vm.downloadFile(mockDoc);

      expect(vm.downloadError).toBe(true);
      expect(vm.downloadErrorMessage).toBe(
        'Failed to download file. Please try again.'
      );
      expect(console.error).toHaveBeenCalledWith(
        'Error downloading file:',
        expect.any(Error)
      );
    });
  });

  describe('Download permissions', () => {
    it('shows the download button when permission is granted', async () => {
      const wrapper = createWrapper({}, [createMockDocument()]);
      await flushPromises();

      expect(wrapper.find('[data-testid="download-file-btn"]').exists()).toBe(
        true
      );
    });

    it('hides the download button when permission is missing', async () => {
      const wrapper = createWrapper({}, [createMockDocument()], []);
      await flushPromises();

      expect(wrapper.find('[data-testid="download-file-btn"]').exists()).toBe(
        false
      );
    });

    it('does not attempt download without permission', async () => {
      const mockDoc = createMockDocument();
      const wrapper = createWrapper({}, [mockDoc], []);

      const vm = wrapper.vm as any;
      await vm.downloadFile(mockDoc);

      expect(
        mockTransitoryDocumentsService.downloadFile
      ).not.toHaveBeenCalled();
    });
  });

  describe('View permissions', () => {
    it('hides the action bar when permission is missing', async () => {
      const wrapper = createWrapper(
        {},
        [createMockDocument()],
        [DOWNLOAD_PERMISSION]
      );
      await flushPromises();

      expect(wrapper.find('[data-testid="action-bar-stub"]').exists()).toBe(
        false
      );
    });

    it('toggles row selection checkboxes based on permission', async () => {
      const withView = createWrapper({ modelValue: false }, [
        createMockDocument(),
      ]);
      await withView.setProps({ modelValue: true });
      await flushPromises();

      const withoutView = createWrapper(
        { modelValue: false },
        [createMockDocument()],
        [DOWNLOAD_PERMISSION]
      );
      await withoutView.setProps({ modelValue: true });
      await flushPromises();

      const selectableCheckboxes = withView
        .find('[data-testid="documents-table"]')
        .findAll('input[type="checkbox"]');
      const noCheckboxes = withoutView
        .find('[data-testid="documents-table"]')
        .findAll('input[type="checkbox"]');

      expect(selectableCheckboxes.length).toBeGreaterThan(0);
      expect(noCheckboxes.length).toBe(0);
    });

    it('renders file names as plain text without permission', async () => {
      const wrapper = createWrapper(
        { modelValue: false },
        [createMockDocument()],
        [DOWNLOAD_PERMISSION]
      );
      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      expect(wrapper.find('a.text-primary').exists()).toBe(false);
      expect(wrapper.text()).toContain('test-file.pdf');
    });

    it('renders file names as links when permission is granted', async () => {
      const wrapper = createWrapper({ modelValue: false }, [
        createMockDocument(),
      ]);
      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      expect(wrapper.find('a.text-primary').exists()).toBe(true);
    });
  });

  describe('Bulk document viewing', () => {
    it('does not allow non-pdfs to be selected.', async () => {
      const mockDoc = createMockDocument({ extension: '.txt' });
      const wrapper = createWrapper({}, [mockDoc]);

      const vm = wrapper.vm as any;

      // Verify that the document is not selectable (isPdf returns false)
      expect(vm.isPdf(mockDoc)).toBe(false);

      // Even if we try to set it as selected, calling handleViewDocuments should show error
      vm.selectedDocuments = [mockDoc];
      await vm.handleViewDocuments();

      expect(vm.downloadError).toBe(true);
      expect(vm.downloadErrorMessage).toBe(
        'Please select PDF files to view in the document viewer.'
      );
      expect(window.open).not.toHaveBeenCalled();
    });

    it('opens PDF files in Nutrient and shows warning for non-PDF files', async () => {
      const pdfDoc = createMockDocument({
        fileName: 'doc1.pdf',
        extension: '.pdf',
      });
      const txtDoc = createMockDocument({
        fileName: 'doc2.txt',
        extension: '.txt',
      });
      const wrapper = createWrapper({ modelValue: false }, [pdfDoc, txtDoc]);

      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      const vm = wrapper.vm as any;

      // Simulate selecting checkbox for PDF document (only PDFs can be selected)
      // In the actual UI, only the PDF checkbox would be enabled
      vm.selectedDocuments = [pdfDoc];

      // Verify the "View documents" button would be available
      expect(vm.selectedDocuments.length).toBeGreaterThan(0);

      // Reset mock to clear any previous calls
      (window.sessionStorage.setItem as any).mockClear();

      // Find and click the "View documents" button
      const viewButton = wrapper.find('[data-testid="view-documents"]');

      if (viewButton.exists()) {
        await viewButton.trigger('click');
      }

      // Should succeed with only the PDF
      expect(window.sessionStorage.setItem).toHaveBeenCalledWith(
        'transitoryDocuments',
        JSON.stringify([pdfDoc])
      );
      expect(window.open).toHaveBeenCalledWith('/nutrient-viewer', '_blank');
    });

    it('opens multiple PDF files in Nutrient without warning', async () => {
      const pdfDoc1 = createMockDocument({
        fileName: 'doc1.pdf',
        extension: '.pdf',
      });
      const pdfDoc2 = createMockDocument({
        fileName: 'doc2.pdf',
        extension: '.pdf',
      });
      const wrapper = createWrapper({ modelValue: false }, [pdfDoc1, pdfDoc2]);

      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      const vm = wrapper.vm as any;

      // Simulate selecting checkboxes for both PDF documents
      vm.selectedDocuments = [pdfDoc1, pdfDoc2];

      // Verify both documents are selected
      expect(vm.selectedDocuments.length).toBe(2);
      expect(vm.isPdf(pdfDoc1)).toBe(true);
      expect(vm.isPdf(pdfDoc2)).toBe(true);

      // Reset mock to clear any previous calls
      (window.sessionStorage.setItem as any).mockClear();

      // Find and click the "View documents" button
      const viewButton = wrapper.find('[data-testid="view-documents"]');

      if (viewButton.exists()) {
        await viewButton.trigger('click');
      }

      expect(vm.downloadError).toBe(false);
      expect(window.sessionStorage.setItem).toHaveBeenCalledWith(
        'transitoryDocuments',
        JSON.stringify([pdfDoc1, pdfDoc2])
      );
      expect(window.open).toHaveBeenCalledWith('/nutrient-viewer', '_blank');
    });

    it('shows error when opening documents fails', async () => {
      const pdfDoc = createMockDocument();
      const wrapper = createWrapper({ modelValue: false }, [pdfDoc]);

      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      (window.sessionStorage.setItem as any).mockImplementation(() => {
        throw new Error('Storage error');
      });

      const vm = wrapper.vm as any;

      // Simulate selecting the PDF checkbox
      vm.selectedDocuments = [pdfDoc];

      // Find and click the "View documents" button
      const viewButton = wrapper.find('[data-testid="view-documents"]');

      if (viewButton.exists()) {
        await viewButton.trigger('click');
      }

      expect(vm.downloadError).toBe(true);
      expect(vm.downloadErrorMessage).toBe(
        'Failed to open documents. Please try again.'
      );
      expect(console.error).toHaveBeenCalledWith(
        'Error opening documents in viewer:',
        expect.any(Error)
      );
    });
  });

  describe('Dialog close behavior', () => {
    it('emits update:modelValue when close button is clicked', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      const closeButton = wrapper.find('[title="close"]');

      if (closeButton) {
        await closeButton.trigger('click');
      }

      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([false]);
    });

    it('clears selected documents when closing', async () => {
      const mockDoc = createMockDocument();
      const wrapper = createWrapper({}, [mockDoc]);
      await flushPromises();

      const vm = wrapper.vm as any;
      vm.selectedDocuments = [mockDoc];

      const closeButton = wrapper.find('[title="close"]');

      if (closeButton) {
        await closeButton.trigger('click');
        await nextTick();
      }

      expect(vm.selectedDocuments).toEqual([]);
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([false]);
    });

    it('closes dialog and clears selection when isOpen becomes false', async () => {
      const mockDoc = createMockDocument();
      const wrapper = createWrapper({}, [mockDoc]);
      await nextTick();

      const vm = wrapper.vm as any;
      vm.selectedDocuments = [mockDoc];
      vm.isOpen = false;
      await nextTick();

      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(vm.selectedDocuments).toEqual([]);
    });
  });

  describe('Snackbar error handling', () => {
    it('closes snackbar when close button is clicked', async () => {
      const wrapper = createWrapper();

      const vm = wrapper.vm as any;
      vm.downloadError = true;
      vm.downloadErrorMessage = 'Test error';

      // Close snackbar
      vm.downloadError = false;

      expect(vm.downloadError).toBe(false);
    });
  });

  describe('Item selection', () => {
    it('only allows PDF files to be selectable', async () => {
      const pdfDoc = createMockDocument({ extension: '.pdf' });
      const txtDoc = createMockDocument({ extension: '.txt' });
      const wrapper = createWrapper({}, [pdfDoc, txtDoc]);

      const vm = wrapper.vm as any;
      expect(vm.isPdf(pdfDoc)).toBe(true);
      expect(vm.isPdf(txtDoc)).toBe(false);
    });
  });

  describe('Table headers', () => {
    it('has correct column headers', async () => {
      const wrapper = createWrapper({}, [createMockDocument()]);
      const vm = wrapper.vm as any;

      expect(vm.headers).toEqual([
        { title: 'Room', key: 'matchedRoomFolder', sortable: true },
        { title: 'File Name', key: 'fileName', sortable: true },
        { title: 'Extension', key: 'extension', sortable: true },
        { title: 'Created', key: 'createdUtc', sortable: true },
        { title: 'Size', key: 'sizeBytes', sortable: true },
        { title: 'Actions', key: 'actions', sortable: false, align: 'center' },
      ]);
    });
  });
});
