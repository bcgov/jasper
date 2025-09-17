import { mount } from '@vue/test-utils';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import ScheduledDocuments from 'CMP/case-details/civil/appearances/ScheduledDocuments.vue';

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
        openDocumentsPdf: vi.fn(),
    },
    openDocumentsPdf: vi.fn(),
}));

vi.mock('@/stores', () => ({
    useCommonStore: () => ({
        courtRoomsAndLocations: [
            { agencyIdentifierCd: 'AG1', name: 'Courtroom 1' },
            { agencyIdentifierCd: 'AG2', name: 'Courtroom 2' },
        ],
    }),
}));

vi.mock('@/filters', () => ({
    beautifyDate: (date: string) => `Beautified: ${date}`,
}));

vi.mock('@/utils/dateUtils', () => ({
    formatDateToDDMMMYYYY: (date: string) => `Formatted: ${date}`,
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
                            <slot name="item.documentTypeDescription" :item="items && items[0] ? items[0] : {}"></slot>
                        </div>`,
                        props: ['headers', 'items', 'itemValue'],
                    },
                },
            },
        });
    };

    beforeEach(() => {
        wrapper = mountComponent(mockDocuments);
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
});
