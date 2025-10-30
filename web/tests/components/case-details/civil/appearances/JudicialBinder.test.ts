import { mount } from '@vue/test-utils';
import JudicialBinder from 'CMP/case-details/civil/appearances/JudicialBinder.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockDocuments = [
  {
    appearanceId: '1',
    fileSeqNo: 1,
    documentTypeDescription: 'Affidavit',
    imageId: 'img1',
    documentSupport: [{ actCd: 'ACT1' }, { actCd: 'ACT2' }],
    filedDt: '2024-06-01',
    filedBy: [{ filedByName: 'John Doe' }, { filedByName: 'Jane Smith' }],
    runtime: 'Completed',
    issue: [{ issueDsc: 'Issue 1' }, { issueDsc: 'Issue 2' }],
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
    filedBy: [],
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
    getBaseCivilDocumentTableHeaders: vi.fn(() => [
      { title: 'SEQ', key: 'fileSeqNo' },
      { title: 'DOCUMENT TYPE', key: 'documentTypeDescription' },
      { title: 'ACT', key: 'activity' },
      { title: 'DATE FILED', key: 'filedDt' },
      { title: 'FILED BY', key: 'filedBy' },
      { title: 'ISSUES', key: 'issue' },
    ]),
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

describe('JudicialBinder.vue', () => {
  let wrapper: ReturnType<typeof mount>;

  const mountComponent = (documents = mockDocuments) => {
    return mount(JudicialBinder, {
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
                                <slot name="item.activity" :item="item"></slot>
                                <slot name="item.filedBy" :item="item"></slot>
                                <slot name="item.issue" :item="item"></slot>
                            </div>
                        </div>`,
            props: ['headers', 'items', 'height'],
          },
        },
      },
    });
  };

  beforeEach(() => {
    vi.clearAllMocks();
    wrapper = mountComponent();
  });

  it('calls openCivilDocument when clicking on document link', async () => {
    const { default: shared } = await import('@/components/shared');
    const openCivilDocumentSpy = vi.spyOn(shared, 'openCivilDocument');

    const link = wrapper.find('a[href="javascript:void(0)"]');
    expect(link.exists()).toBe(true);
    await link.trigger('click');

    expect(openCivilDocumentSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        appearanceId: '1',
        documentTypeDescription: 'Affidavit',
        imageId: 'img1',
      }),
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

  it('uses courtRoomsAndLocations from commonStore', async () => {
    const { default: shared } = await import('@/components/shared');
    const openCivilDocumentSpy = vi.spyOn(shared, 'openCivilDocument');

    const link = wrapper.find('a[href="javascript:void(0)"]');
    await link.trigger('click');

    const [, , , , , locations] = openCivilDocumentSpy.mock.calls[0];
    expect(locations).toEqual([
      { agencyIdentifierCd: 'AG1', name: 'Courtroom 1' },
      { agencyIdentifierCd: 'AG2', name: 'Courtroom 2' },
    ]);
  });

  it('does not call openCivilDocument when document has no imageId', async () => {
    const { default: shared } = await import('@/components/shared');
    const openCivilDocumentSpy = vi.spyOn(shared, 'openCivilDocument');

    // Try to find and click the span (not a link) for the document without imageId
    const spans = wrapper.findAll('span');
    const orderSpan = spans.find((span) => span.text() === 'Order');

    if (orderSpan) {
      await orderSpan.trigger('click');
    }

    // Should not have been called since it's not a link
    expect(openCivilDocumentSpy).not.toHaveBeenCalledWith(
      expect.objectContaining({
        appearanceId: '2',
      }),
      expect.anything(),
      expect.anything(),
      expect.anything(),
      expect.anything(),
      expect.anything()
    );
  });

  it('passes all required props to openCivilDocument', async () => {
    const { default: shared } = await import('@/components/shared');
    const openCivilDocumentSpy = vi.spyOn(shared, 'openCivilDocument');

    const link = wrapper.find('a[href="javascript:void(0)"]');
    await link.trigger('click');

    expect(openCivilDocumentSpy).toHaveBeenCalledWith(
      expect.any(Object), // document
      'file123', // fileId
      'FN123', // fileNumberTxt
      'Provincial', // courtLevel
      'AG1', // agencyId
      expect.any(Array) // locations
    );
  });
});
