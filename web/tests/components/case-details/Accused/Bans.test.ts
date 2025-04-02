import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import Bans from 'CMP/case-details/Accused/Bans.vue';

describe('Bans.vue', () => {
  const bansMock = [
    {
      banStatuteId: 1,
      banTypeCd: 'Type1',
      banOrderedDate: '2025-01-01 00:00:00.0',
      banTypeAct: 'Act1',
      banTypeSection: 'Section1',
      banTypeSubSection: 'Sub1',
      banTypeDescription: 'Description1',
      banCommentText: 'Comment1',
    },
    {
      banStatuteId: 2,
      banTypeCd: 'Type2',
      banOrderedDate: '2005-06-16 00:00:00.0',
      banTypeAct: 'Act2',
      banTypeSection: 'Section2',
      banTypeSubSection: 'Sub2',
      banTypeDescription: 'Description2',
      banCommentText: 'Comment2',

    },
  ];

  it('renders the correct number of rows in the table', () => {
    const wrapper = mount(Bans, {
      props: {
        bans: bansMock,
        modelValue: true,
      },
    });

    const rows = wrapper.findAll('tbody tr');
    expect(rows.length).toBe(bansMock.length);
  });

  it('renders the correct data in the table', () => {
    const wrapper = mount(Bans, {
      props: {
        bans: bansMock,
        modelValue: true,
      },
    });

    const firstRowCells = wrapper.findAll('tbody tr').at(0)?.findAll('td');
    expect(firstRowCells?.at(0)?.text()).toBe(bansMock[0].banTypeDescription);
    expect(firstRowCells?.at(1)?.text()).toBe('01-Jan-2025');
    expect(firstRowCells?.at(2)?.text()).toBe(bansMock[0].banTypeAct);
    expect(firstRowCells?.at(3)?.text()).toBe(bansMock[0].banTypeSection);
    expect(firstRowCells?.at(4)?.text()).toBe(bansMock[0].banTypeSubSection);
    expect(firstRowCells?.at(5)?.text()).toBe(bansMock[0].banCommentText);
  });
});