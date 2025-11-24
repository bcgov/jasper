import { BinderService } from '@/services';
import { useCommonStore } from '@/stores';
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
      "title": "Scheduled",
      "value": "Scheduled",
    });
  });

  it(`renames 'CSR' to 'Court Summary' in the document categories`, async () => {
    expect(wrapper.vm.documentCategories[1]).toEqual({
      "title": "Court Summary",
      "value": "CSR",
    });
  });

  it(`renames 'Affidavits' to 'Affidavits/Financial Stmts' in the document categories`, async () => {
    expect(wrapper.vm.documentCategories[4]).toEqual({
      "title": "Affidavits/Financial Stmts",
      "value": "Affidavits",
    });
  });
});
