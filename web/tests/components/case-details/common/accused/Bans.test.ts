import Bans from '@/components/case-details/common/accused/Bans.vue';
import { banType } from '@/types/criminal/jsonTypes';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('Bans.vue', () => {
  const bansMock = [
    {
      banStatuteId: '1',
      banStatuteDesc: 'Statute1',
      banTypeCd: 'Type1',
      banOrderedDate: '2025-01-01 00:00:00.0',
      banTypeAct: 'Act1',
      banTypeSection: 'Section1',
      banTypeSubSection: 'Sub1',
      banTypeDescription: 'Description1',
      banCommentText: 'Comment1',
    } as banType,
    {
      banStatuteId: '2',
      banTypeCd: 'Type2',
      banOrderedDate: '2005-06-16 00:00:00.0',
      banTypeAct: 'Act2',
      banTypeSection: 'Section2',
      banTypeSubSection: 'Sub2',
      banTypeDescription: 'Description2',
      banCommentText: 'Comment2',
    } as banType,
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
    expect(firstRowCells?.at(5)?.text()).toBe(bansMock[0].banStatuteDesc);

    const secondRowCells = wrapper.findAll('tbody tr').at(1)?.findAll('td');
    expect(secondRowCells?.at(0)?.text()).toBe(bansMock[1].banTypeDescription);
    expect(secondRowCells?.at(1)?.text()).toBe('16-Jun-2005');
    expect(secondRowCells?.at(2)?.text()).toBe(bansMock[1].banTypeAct);
    expect(secondRowCells?.at(3)?.text()).toBe(bansMock[1].banTypeSection);
    expect(secondRowCells?.at(4)?.text()).toBe(bansMock[1].banTypeSubSection);
    expect(secondRowCells?.at(5)?.text()).toBe(bansMock[1].banStatuteId);
  });
});
