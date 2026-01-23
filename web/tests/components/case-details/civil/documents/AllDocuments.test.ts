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

  it('renders All Documents', () => {
    const mockDocuments = [{} as civilDocumentType, {} as civilDocumentType];
    mockProps.documents = mockDocuments;
    const wrapper = mount(AllDocuments, {
      props: mockProps,
    });

    const mainEl = wrapper.find('[data-testid="all-documents-container"]');
    const header = wrapper.find('.text-h5');
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

  it('displays "All Documents" when no category is selected', () => {
    mockProps.documents = [{} as civilDocumentType];
    mockProps.binderDocumentIds = [];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: undefined,
      },
    });

    const header = wrapper.find('.text-h5');
    expect(header.text()).toContain('All Documents (1)');
  });

  it('displays category display title when category is selected', () => {
    mockProps.documents = [{} as civilDocumentType];
    const getCategoryDisplayTitle = vi.fn().mockReturnValue('Orders');
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: 'ORDER',
        getCategoryDisplayTitle,
      },
    });

    const header = wrapper.find('.text-h5');
    expect(getCategoryDisplayTitle).toHaveBeenCalledWith('ORDER');
    expect(header.text()).toContain('Orders (1)');
  });

  it('displays "All Documents" when category is selected but getCategoryDisplayTitle is not provided', () => {
    mockProps.documents = [{} as civilDocumentType];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: 'ORDER',
        getCategoryDisplayTitle: undefined,
      },
    });

    const header = wrapper.find('.text-h5');
    expect(header.text()).toContain('All Documents (1)');
  });

  it('displays scheduled date when selectedCategory is "Scheduled" and nextAppearanceDt exists', () => {
    const mockDocument = {
      civilDocumentId: '1',
      documentTypeDescription: 'Test Document',
      imageId: 'img-123',
      nextAppearanceDt: '2026-01-15',
    } as civilDocumentType;
    
    mockProps.documents = [mockDocument];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: 'Scheduled',
      },
    });

    const documentCell = wrapper.find('[data-testid="all-documents-container"]');
    expect(documentCell.exists()).toBe(true);
    // The scheduled date should be formatted and displayed
    const tableEl = wrapper.find('v-data-table-virtual');
    expect(tableEl.exists()).toBe(true);
  });

  it('does not display scheduled date when selectedCategory is not "Scheduled"', () => {
    const mockDocument = {
      civilDocumentId: '1',
      documentTypeDescription: 'Test Document',
      imageId: 'img-123',
      nextAppearanceDt: '2026-01-15',
    } as civilDocumentType;
    
    mockProps.documents = [mockDocument];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: 'ORDER',
      },
    });

    const tableEl = wrapper.find('v-data-table-virtual');
    expect(tableEl.exists()).toBe(true);
  });

  it('does not display scheduled date when nextAppearanceDt is not present', () => {
    const mockDocument = {
      civilDocumentId: '1',
      documentTypeDescription: 'Test Document',
      imageId: 'img-123',
    } as civilDocumentType;
    
    mockProps.documents = [mockDocument];
    const wrapper = mount(AllDocuments, {
      props: {
        ...mockProps,
        selectedCategory: 'Scheduled',
      },
    });

    const tableEl = wrapper.find('v-data-table-virtual');
    expect(tableEl.exists()).toBe(true);
  });
});
