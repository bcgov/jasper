import { mount } from '@vue/test-utils';
import ScheduledDocuments from 'CMP/case-details/civil/appearances/ScheduledDocuments.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockDocuments = [
  {
    appearanceId: '1',
    fileSeqNo: 1,
    documentTypeDescription: 'Affidavit',
    imageId: 'img1',
    documentSupport: [{ actCd: 'ACT1' }, { actCd: 'ACT2' }],
    filedDt: '2024-06-01',
    filedByName: 'John Doe',
    runtime: 'Completed',
    issue: [{ issueDsc: 'Issue 1' }],
    category: 'civil',
    DateGranted: '2024-06-01',
    civilDocumentId: 'doc1',
    partId: 'part1',
  },
  {
    appearanceId: '2',
    fileSeqNo: 2,
    documentTypeDescription: 'Order',
    imageId: null,
    documentSupport: [],
    filedDt: '2024-06-02',
    filedByName: 'Jane Smith',
    runtime: 'Pending',
    issue: [],
    category: 'csr',
    DateGranted: '2024-06-02',
    civilDocumentId: 'doc2',
    partId: 'part2',
  },
];

vi.mock('@/components/shared', () => ({
  default: {
    openCivilDocument: vi.fn(),
  },
}));

vi.mock('@/stores', () => ({
  useCommonStore: () => ({
    courtRoomsAndLocations: [
      { agencyIdentifierCd: 'AG1', name: 'Courtroom 1' },
      { agencyIdentifierCd: 'AG2', name: 'Courtroom 2' },
    ],
  }),
}));

vi.mock('@/components/documents/DocumentUtils', () => ({
  prepareCivilDocumentData: vi.fn(() => ({
    fileId: undefined,
    fileNumberText: undefined,
    courtLevel: undefined,
    location: undefined,
  })),
  getCivilDocumentType: vi.fn(() => 'Civil'),
}));

describe('ScheduledDocuments.vue', () => {
  let wrapper: ReturnType<typeof mount>;
  const mountComponent = (documents = mockDocuments) => {
    return mount(ScheduledDocuments, {
      props: {
        documents,
        fileId: 'file123',
        fileNumberTxt: 'FN123',
        courtLevel: 'Provincial',
        agencyId: 'AG1',
      },
      global: {
        stubs: {
          LabelWithTooltip: {
            template: '<div class="label-tooltip"></div>',
            props: ['values', 'location'],
          },
          'v-data-table-virtual': {
            template: `<div>
                            <div v-for="item in items" :key="item.appearanceId">
                                <slot name="item.documentTypeDescription" :item="item"></slot>
                            </div>
                        </div>`,
            props: ['headers', 'items', 'itemValue'],
          },
        },
      },
    });
  };

  beforeEach(() => {
    vi.clearAllMocks();
    const testDocuments = [
      {
        ...mockDocuments[0],
        imageId: 'test-image-id',
      },
    ];
    wrapper = mountComponent(testDocuments);
  });

  it('renders document type as link if imageId exists', () => {
    const links = wrapper.findAll('a');
    expect(links.length).toBeGreaterThan(0);
    expect(links[0].text()).toContain('Affidavit');
  });

  it('does not render document type as link if no imageId exists', () => {
    mockDocuments[0].imageId = null;
    wrapper = mountComponent(mockDocuments);
    const links = wrapper.findAll('a');
    expect(links.length).toBe(0);
  });

  it('should pass correct arguments when link is clicked', async () => {
    const { default: shared } = await import('@/components/shared');
    const openCivilDocumentSpy = vi.spyOn(shared, 'openCivilDocument');

    // Find and click the link - ensure it exists
    const link = wrapper.find('a[href="javascript:void(0)"]');
    expect(link.exists()).toBe(true);
    await link.trigger('click');

    expect(openCivilDocumentSpy).toHaveBeenCalledWith(
      { ...mockDocuments[0], imageId: 'test-image-id' },
      'file123',
      'FN123',
      'Provincial',
      'AG1',
      [
        { agencyIdentifierCd: 'AG1', name: 'Courtroom 1' },
        { agencyIdentifierCd: 'AG2', name: 'Courtroom 2' },
      ]
    );
  });
});
