import { shallowMount } from '@vue/test-utils';
import DocumentsView from 'CMP/case-details/common/DocumentsView.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';

describe('DocumentsView.vue', () => {
  let wrapper: any;
  let mockParticipantOne: any;
  let mockParticipantTwo: any;
  let mockParticipants: any[];

  beforeEach(() => {
    mockParticipantOne = {
      fullName: 'John Doe',
      lastNm: 'Doe',
      givenNm: 'John',
      profSeqNo: 1,
      document: [
        {
          issueDate: '2023-01-01',
          documentTypeDescription: 'Type A',
          category: 'rop',
          documentPageCount: 5,
          imageId: '123',
        },
      ],
    };
    mockParticipantTwo = {
      fullName: 'Jane Smith',
      profSeqNo: 2,
      lastNm: 'Smith',
      givenNm: 'Jane',
      document: [
        {
          issueDate: '2023-02-01',
          documentTypeDescription: 'Type B',
          category: 'other',
          documentPageCount: 3,
          imageId: '456',
        },
      ],
    };
    mockParticipants = [mockParticipantOne, mockParticipantTwo];
    setActivePinia(createPinia());
    wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
  });

  it('renders only documents section', () => {
    const sections = wrapper.findAll('v-card-text .text-h5');
    const tables = wrapper.findAll('v-data-table-virtual');

    expect(tables).toHaveLength(1);
    expect(sections).toHaveLength(1);
    expect(sections[0].text()).toContain('All Documents');
  });

  it('renders correct all documents header with correct count', () => {
    mockParticipants.push(mockParticipantOne);
    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
    const sections = wrapper.findAll('v-card-text .text-h5');
    expect(sections).toHaveLength(1);
    expect(sections[0].text()).toBe('All Documents (3)');
  });

  it('does not render table when no documents', () => {
    mockParticipants.length = 0;
    mockParticipantOne.document = [];
    mockParticipants.push(mockParticipantOne);

    const wrapper = shallowMount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
    const sections = wrapper.findAll('v-card-text .text-h5');
    const tables = wrapper.findAll('v-data-table-virtual');
    expect(tables).toHaveLength(0);
    expect(sections).toHaveLength(0);
  });

  it('computes unfilteredDocuments correctly', async () => {
    wrapper.vm.selectedCategory = 'ROP';

    const unfilteredDocuments = wrapper.vm.unfilteredDocuments;
    expect(unfilteredDocuments).toHaveLength(2);
  });

  it('filters documents by category', async () => {
    wrapper.vm.selectedCategory = 'ROP';

    const documents = wrapper.vm.documents;
    expect(documents).toHaveLength(1);
    expect(documents[0].category).toBe('rop');
  });

  it.each([
    ['rop', 'ROP'],
    ['other', 'other'],
  ])('formats category correctly', (input, expected) => {
    const formattedCategory = wrapper.vm.formatCategory({
      category: input,
    });
    expect(formattedCategory).toBe(expected);
  });

  it.each([
    [{ category: 'rop', documentTypeDescription: '' }, 'Record of Proceedings'],
    [{ category: 'other', documentTypeDescription: 'Other' }, 'Other'],
  ])('formats document type correctly for %o', (input, expected) => {
    const formattedType = wrapper.vm.formatType(input);
    expect(formattedType).toBe(expected);
  });
});
