import AllDocuments from '@/components/case-details/civil/documents/AllDocuments.vue';
import { civilDocumentType } from '@/types/civil/jsonTypes';
import { mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/utils/dateUtils', () => ({
  formatDateToDDMMMYYYY: vi.fn(() => '01-Jan-2025'),
}));

describe('AllDocuments.vue', () => {
  const mockProps = {
    selectedItems: [],
    documents: [] as civilDocumentType[],
    courtClassCdStyle: '',
    hasBinder: false,
    rolesLoading: false,
    roles: [],
    baseHeaders: [],
    binderDocumentIds: [] as string[],
    addDocumentToBinder: vi.fn(),
    openIndividualDocument: vi.fn(),
  };

  it('does not render All Documents when there are no documents', () => {
    const wrapper = mount(AllDocuments, {
      props: mockProps,
    });

    const mainEl = wrapper.find('[data-testid="all-documents-container"]');

    expect(mainEl.exists()).toBe(false);
  });

  it('renders header when filters are active even if there are no documents', () => {
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        documents: [],
        hasActiveFilters: true,
      },
    });

    const mainEl = wrapper.find('[data-testid="all-documents-container"]');
    const header = wrapper.find('.text-headline-small');

    expect(mainEl.exists()).toBe(true);
    expect(header.text()).toContain('All Documents (0)');
  });

  it('renders All Documents', () => {
    const mockDocuments = [{} as civilDocumentType, {} as civilDocumentType];
    mockProps.documents = mockDocuments;
    const wrapper = mount(AllDocuments, {
      props: mockProps,
    });

    const mainEl = wrapper.find('[data-testid="all-documents-container"]');
    const header = wrapper.find('.text-headline-small');
    const alertEl = wrapper.find('v-alert');
    const tableEl = wrapper.find('v-data-table-virtual');

    expect(mainEl.exists()).toBe(true);
    expect(header.text()).toContain(`All Documents (${mockDocuments.length})`);
    expect(alertEl.exists()).toBe(true);
    expect(tableEl.exists()).toBe(true);
  });

  it('hides the judicial binder alert when there is an existing binder', () => {
    mockProps.documents = [{} as civilDocumentType, {} as civilDocumentType];
    mockProps.binderDocumentIds = [''];
    const wrapper = mount(AllDocuments, {
      props: mockProps,
    });

    const mainEl = wrapper.find('[data-testid="all-documents-container"]');
    const alertEl = wrapper.find('v-alert');
    const tableEl = wrapper.find('v-data-table-virtual');

    expect(mainEl.exists()).toBe(true);
    expect(alertEl.exists()).toBe(false);
    expect(tableEl.exists()).toBe(true);
  });

  it('displays sectionTitle when provided', () => {
    mockProps.documents = [{} as civilDocumentType];
    mockProps.binderDocumentIds = [];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        sectionTitle: 'Orders',
      },
    });

    const header = wrapper.find('.text-headline-small');
    expect(header.text()).toContain('Orders (1)');
  });

  it('displays "All Documents" when sectionTitle is not provided', () => {
    mockProps.documents = [{} as civilDocumentType];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        sectionTitle: undefined,
      },
    });

    const header = wrapper.find('.text-headline-small');
    expect(header.text()).toContain('All Documents (1)');
  });

  it.each(['asc', 'desc'] as const)(
    'keeps pinned documents last when sorting %s',
    (order) => {
      const regularDocument = {
        civilDocumentId: 'regular',
        category: 'ROP',
        documentTypeDescription: 'A document',
      } as civilDocumentType;
      const courtSummary = {
        civilDocumentId: 'summary',
        category: 'CSR',
        documentTypeDescription: 'Z document',
      } as civilDocumentType;
      const wrapper = mount(AllDocuments, {
        props: {
          ...mockProps,
          documents: [regularDocument, courtSummary],
          baseHeaders: [
            { title: 'DOCUMENT TYPE', key: 'documentTypeDescription' },
          ],
          sortBy: [{ key: 'documentTypeDescription', order }],
          pinToBottom: (document) => document.category === 'CSR',
        },
      });

      expect(wrapper.find('v-data-table-virtual').attributes('must-sort')).toBe(
        'true'
      );
      const comparator = wrapper.vm.headers.find(
        (header) => header.key === 'documentTypeDescription'
      ).sortRaw;
      const comparison =
        order === 'desc'
          ? comparator(courtSummary, regularDocument)
          : comparator(regularDocument, courtSummary);

      expect(comparison).toBeLessThan(0);
    }
  );
});
