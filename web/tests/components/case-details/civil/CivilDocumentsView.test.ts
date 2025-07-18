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
      documentTypeCd: 'CSR',
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
      documentTypeCd: 'DOC',
      documentTypeDescription: 'Civil Document 2',
      filedDt: '2023-02-01',
      filedBy: [{ roleTypeCode: 'Role2' }],
      issue: [{ issueTypeDesc: 'Issue2' }],
      documentSupport: [{ actCd: 'Act2' }],
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
    wrapper.vm.selectedType = 'CSR';

    expect(wrapper.vm.filteredDocuments).toEqual([mockDocuments[0]]);
  });

  it('renders action-bar when two or more documents with imageIds are selected', async () => {
    wrapper.vm.selectedItems = [mockDocuments[0], mockDocuments[0]];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(true);
  });

  it('renders action-bar when two or more documents are selected', async () => {
    wrapper.vm.selectedItems = [{}, {}];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(true);
  });

  it('does not render action-bar when one document is selected', async () => {
    wrapper.vm.selectedItems = [mockDocuments[0]];

    await nextTick();

    expect(wrapper.findComponent({ name: 'ActionBar' }).exists()).toBe(false);
  });
});
