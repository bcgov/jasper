import { describe, it, expect, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia'
import DocumentsView from 'CMP/case-details/DocumentsView.vue';

describe('DocumentsView.vue', () => {
  let wrapper: ReturnType<typeof mount>;

  const mockParticipants = [
    {
      fullName: 'John Doe',
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
    },
    {
      fullName: 'Jane Smith',
      profSeqNo: 2,
      document: [
        {
          issueDate: '2023-02-01',
          documentTypeDescription: 'Type B',
          category: 'other',
          documentPageCount: 3,
          imageId: '456',
        },
      ],
    },
  ];

  beforeEach(() => {
    setActivePinia(createPinia());
    wrapper = mount(DocumentsView, {
      props: {
        participants: mockParticipants,
      },
    });
  });

  it('renders only documents section', () => {
    const sections = wrapper.findAll('v-card-text .text-h5');
    expect(sections).toHaveLength(1);
    expect(sections[0].text()).toBe('Documents');
  });
});