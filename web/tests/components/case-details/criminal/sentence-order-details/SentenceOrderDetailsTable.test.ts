import SentenceOrderDetailsTable from '@/components/case-details/criminal/sentence-order-details/SentenceOrderDetailsTable.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('SentenceOrderDetailsTable.vue', () => {
  it('renders sentence order table', async () => {
    const wrapper: any = mount(SentenceOrderDetailsTable, {
      props: {
        participants: [],
      },
    });

    const expectedHeaderKeys = [
      'appearanceDate',
      'countNumber',
      'sectionTxt',
      'finding',
      'sentences',
      'terms',
      'amount',
      'dueDate',
      'effectiveDate',
    ];

    const headerKeys = wrapper.vm.headers.map((header: any) => header.key);

    await nextTick();

    expect(headerKeys).toEqual(expectedHeaderKeys);
  });
});
