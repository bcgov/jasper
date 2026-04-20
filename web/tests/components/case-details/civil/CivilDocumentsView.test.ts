import shared from '@/components/shared';
import { BinderService } from '@/services';
import { useCommonStore } from '@/stores';
import { DocumentRequestType } from '@/types/shared';
import { shallowMount } from '@vue/test-utils';
import CivilDocumentsView from 'CMP/case-details/civil/CivilDocumentsView.vue';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/stores', () => ({
  useCivilFileStore: vi.fn(() => ({
    civilFileInformation: {
      fileNumber: '12345',
      detailsData: {
        courtClassCd: 'A',
        courtLevelCd: 'B',
        homeLocationAgencyName: 'Test Location',
      },
    },
  })),
  useCommonStore: vi.fn(() => ({
    roles: [{}],
    setRoles: vi.fn(() => ({})),
  })),
}));

const mockBinderService = {
  getBinders: vi.fn(),
  addBinder: vi.fn(),
  updateBinder: vi.fn(),
  deleteBinder: vi.fn(),
} as unknown as BinderService;

describe('CivilDocumentsView.vue', () => {
  let wrapper: any;
  let commonStore: any;
  const mockDocuments = [
    {
      civilDocumentId: '1',
      category: 'CSR',
      documentTypeDescription: 'Civil Document 1',
      filedDt: '2023-01-01',
      filedBy: [{ roleTypeCode: 'Role1' }],
      fileSeqNo: '1',
      issue: [{ issueTypeDesc: 'Issue1' }],
      documentSupport: [{ actCd: 'Act1' }],
      imageId: '123',
      lastAppearanceDt: '2023-01-02',
    },
    {
      civilDocumentId: '2',
      category: 'ROP',
      documentTypeDescription: 'Civil Document 2',
      filedDt: '2023-02-01',
      filedBy: [{ roleTypeCode: 'Role2' }],
      issue: [{ issueTypeDesc: 'Issue2' }],
      documentSupport: [{ actCd: 'Act2' }],
    },
    {
      civilDocumentId: '3',
      category: 'Pleadings',
      documentTypeDescription: 'Civil Document 3',
      filedDt: '2023-02-01',
      nextAppearanceDt: '2023-03-01',
      filedBy: [{ roleTypeCode: 'Role3' }],
      issue: [{ issueTypeDesc: 'Issue3' }],
      documentSupport: [{ actCd: 'Act3' }],
    },
    {
      civilDocumentId: '4',
      category: 'Affidavits',
      documentTypeDescription: 'Civil Document 4',
      filedDt: '2023-02-01',
      nextAppearanceDt: '2023-04-01',
      filedBy: [{ roleTypeCode: 'Role4' }],
      issue: [{ issueTypeDesc: 'Issue4' }],
      documentSupport: [{ actCd: 'Act4' }],
    },
    {
      civilDocumentId: '5',
      category: 'Transcript',
      documentTypeDescription: 'Transcript Document',
      filedDt: '2023-05-01',
      filedBy: [{ roleTypeCode: 'Role5' }],
      issue: [{ issueTypeDesc: 'Issue5' }],
      documentSupport: [{ actCd: 'Act5' }],
      transcriptOrderId: 12345,
      imageId: '456',
    },
  ];
  beforeEach(() => {
    wrapper = shallowMount(CivilDocumentsView, {
      props: { documents: mockDocuments },
      global: {
        provide: {
          binderService: mockBinderService,
        },
      },
    });
    commonStore = {
      setRoles: vi.fn(),
    };
    (useCommonStore as any).mockReturnValue(commonStore);
  });

  it('renders the component correctly', () => {
    (mockBinderService.getBinders as Mock).mockResolvedValue([]);

    expect(wrapper.exists()).toBe(true);
    expect(wrapper.find('v-select').exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'JudicialBinder' }).exists()).toBe(
      true
    );
    expect(wrapper.findComponent({ name: 'AllDocuments' }).exists()).toBe(true);
  });

  it('filters documents by selected type', async () => {
    wrapper.vm.selectedCategory = 'CSR';

    expect(wrapper.vm.filteredDocuments).toEqual([mockDocuments[0]]);
  });

  it('renders action-bar when two or more documents with imageIds are selected', async () => {
    wrapper.vm.selectedItems = [mockDocuments[0], mockDocuments[0]];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(true);
  });

  it('renders both action-bars when two or more documents are selected', async () => {
    wrapper.vm.selectedItems = [{}, {}];
    wrapper.vm.selectedBinderItems = [{}];

    await nextTick();

    expect(wrapper.findAllComponents({ name: 'ActionBar' })).toHaveLength(2);
  });

  it('renders one action-bar when one document is selected', async () => {
    wrapper.vm.selectedItems = [mockDocuments[0]];

    await nextTick();

    expect(wrapper.findAllComponents({ name: 'ActionBar' })).toHaveLength(1);
  });

  it('renders one action-bar when one document is selected', async () => {
    wrapper.vm.selectedItems = [mockDocuments[0]];

    await nextTick();

    expect(wrapper.findAllComponents({ name: 'ActionBar' })).toHaveLength(1);
  });

  it('removeSelectedJudicialDocuments should clear all selected binder items', async () => {
    wrapper.vm.selectedItems = [{}, {}];
    wrapper.vm.selectedBinderItems = [mockDocuments[0]];
    wrapper.vm.removeSelectedJudicialDocuments();

    await nextTick();

    expect(wrapper.vm.selectedBinderItems).toEqual([]);
  });

  it('inserts "Scheduled" option when a document has a next appearance date', async () => {
    expect(wrapper.vm.documentCategories[0]).toEqual({
      title: 'Scheduled',
      value: 'Scheduled',
    });
  });

  it(`renames 'CSR' to 'Court Summary' in the document categories`, async () => {
    expect(wrapper.vm.documentCategories[1]).toEqual({
      title: 'Court Summary',
      value: 'CSR',
    });
  });

  it(`renames 'Affidavits' to 'Affidavits/Financial Stmts' in the document categories`, async () => {
    expect(wrapper.vm.documentCategories[4]).toEqual({
      title: 'Affidavits/Financial Stmts',
      value: 'Affidavits',
    });
  });

  it('uses leaf document name to include filed date when opening merged civil documents', () => {
    const openMergedDocumentsSpy = vi
      .spyOn(shared, 'openMergedDocuments')
      .mockImplementation(() => {});

    wrapper.vm.openMergedDocuments([mockDocuments[0]]);

    expect(openMergedDocumentsSpy).toHaveBeenCalledWith([
      expect.objectContaining({
        groupKeyTwo: '',
        documentName:
          mockDocuments[0].fileSeqNo +
          ' - ' +
          mockDocuments[0].documentTypeDescription +
          ' - ' +
          '01 Jan 2023',
      }),
    ]);
  });

  describe('addDocumentToBinder', () => {
    beforeEach(() => {
      wrapper.vm.currentBinder = {
        id: 'binder-1',
        labels: {},
        documents: [],
      };
      (mockBinderService.updateBinder as Mock).mockImplementation((binder) => {
        return Promise.resolve({
          succeeded: true,
          payload: binder,
        });
      });
    });

    it('should add a transcript document to binder with orderId', async () => {
      await wrapper.vm.addDocumentToBinder(mockDocuments[4]);

      expect(wrapper.vm.currentBinder.documents).toHaveLength(1);
      expect(wrapper.vm.currentBinder.documents[0]).toEqual({
        documentId: '5',
        order: 0,
        documentType: DocumentRequestType.Transcript,
        fileName: 'Transcript Document',
        orderId: '12345',
      });
    });

    it('should add transcript without orderId if transcriptOrderId is missing', async () => {
      const transcriptWithoutOrderId = {
        ...mockDocuments[4],
        transcriptOrderId: undefined,
      };

      await wrapper.vm.addDocumentToBinder(transcriptWithoutOrderId);

      expect(wrapper.vm.currentBinder.documents).toHaveLength(1);
      expect(wrapper.vm.currentBinder.documents[0]).toEqual({
        documentId: '5',
        order: 0,
        documentType: DocumentRequestType.Transcript,
        fileName: 'Transcript Document',
      });
    });

    it('should set correct order for multiple documents', async () => {
      await wrapper.vm.addDocumentToBinder(mockDocuments[0]);
      await wrapper.vm.addDocumentToBinder(mockDocuments[1]);

      expect(wrapper.vm.currentBinder.documents).toHaveLength(2);
      expect(wrapper.vm.currentBinder.documents[0].order).toBe(0);
      expect(wrapper.vm.currentBinder.documents[1].order).toBe(1);
    });
  });

  describe('addSelectedItemsToBinder', () => {
    beforeEach(() => {
      wrapper.vm.currentBinder = {
        id: 'binder-1',
        labels: {},
        documents: [],
      };
      (mockBinderService.updateBinder as Mock).mockImplementation((binder) => {
        return Promise.resolve({
          succeeded: true,
          payload: binder,
        });
      });
    });

    it('should add multiple documents including transcripts with orderId', async () => {
      wrapper.vm.selectedItems = [mockDocuments[0], mockDocuments[4]];
      await wrapper.vm.addSelectedItemsToBinder();

      expect(wrapper.vm.currentBinder.documents).toHaveLength(2);
      expect(wrapper.vm.currentBinder.documents[0]).toEqual({
        documentId: '1',
        order: 0,
        documentType: DocumentRequestType.File,
        fileName: 'Civil Document 1',
      });
      expect(wrapper.vm.currentBinder.documents[1]).toEqual({
        documentId: '5',
        order: 1,
        documentType: DocumentRequestType.Transcript,
        fileName: 'Transcript Document',
        orderId: '12345',
      });
    });

    it('should skip documents already in binder', async () => {
      wrapper.vm.currentBinder.documents = [
        {
          documentId: '1',
          order: 0,
          documentType: DocumentRequestType.File,
          fileName: 'Civil Document 1',
        },
      ];
      wrapper.vm.selectedItems = [mockDocuments[0], mockDocuments[1]];
      await wrapper.vm.addSelectedItemsToBinder();

      expect(wrapper.vm.currentBinder.documents).toHaveLength(2);
      expect(wrapper.vm.currentBinder.documents[1].documentId).toBe('2');
    });

    it('should clear selectedItems after adding', async () => {
      wrapper.vm.selectedItems = [mockDocuments[0]];
      await wrapper.vm.addSelectedItemsToBinder();

      expect(wrapper.vm.selectedItems).toEqual([]);
    });

    it('should not save if all selected items are already in binder', async () => {
      wrapper.vm.currentBinder.documents = [
        {
          documentId: '1',
          order: 0,
          documentType: DocumentRequestType.File,
          fileName: 'Civil Document 1',
        },
      ];
      wrapper.vm.selectedItems = [mockDocuments[0]];

      const updateBinderSpy = vi.spyOn(mockBinderService, 'updateBinder');
      await wrapper.vm.addSelectedItemsToBinder();

      expect(updateBinderSpy).not.toHaveBeenCalled();
      expect(wrapper.vm.selectedItems).toEqual([]);
    });
  });
  describe('Category Filtering', () => {
    beforeEach(async () => {
      const mockBinders = [
        {
          binderId: 'binder-1',
          name: 'Test Binder',
          documents: [
            { documentId: '1', order: 1 },
            { documentId: '2', order: 2 },
            { documentId: '3', order: 3 },
            { documentId: '4', order: 4 },
          ],
        },
      ];
      (mockBinderService.getBinders as Mock).mockResolvedValue({
        succeeded: true,
        payload: mockBinders,
      });

      wrapper = shallowMount(CivilDocumentsView, {
        props: { documents: mockDocuments },
        global: {
          provide: {
            binderService: mockBinderService,
          },
        },
      });

      await nextTick();
    });

    it('does not filter judicial binder documents by category', async () => {
      wrapper.vm.selectedCategory = 'CSR';
      await nextTick();

      expect(wrapper.vm.filteredDocuments).toHaveLength(1);
      expect(wrapper.vm.filteredDocuments[0].civilDocumentId).toBe('1');

      expect(wrapper.vm.binderDocuments).toHaveLength(4);
      expect(
        wrapper.vm.binderDocuments.map((d: any) => d.civilDocumentId)
      ).toEqual(['1', '2', '3', '4']);
    });

    it('filters all documents table by category', async () => {
      wrapper.vm.selectedCategory = 'ROP';
      await nextTick();

      expect(wrapper.vm.filteredDocuments).toHaveLength(1);
      expect(wrapper.vm.filteredDocuments[0].category).toBe('ROP');
      expect(wrapper.vm.filteredDocuments[0].civilDocumentId).toBe('2');
    });
  });
});
