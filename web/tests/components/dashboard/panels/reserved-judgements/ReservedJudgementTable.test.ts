import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ReservedJudgementTable from 'CMP/dashboard/panels/reserved-judgements/ReservedJudgementTable.vue';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';

const mockData = [
  {
    fileNumber: '12345',
    appearanceDate: '2024-06-01',
    ageInDays: 10,
  },
  {
    fileNumber: '67890',
    appearanceDate: '2024-05-20',
    ageInDays: 22,
  },
];

describe('ReservedJudgementTable.vue', () => {
  it('renders table headers correctly', () => {
    const wrapper = mount(ReservedJudgementTable, {
      props: { data: mockData as any },
    });
    const headerTitles = wrapper.vm.headers.map((h: any) => h.title);
    expect(headerTitles).toContain('FILE #');
    expect(headerTitles).toContain('LAST APPEARANCE');
    expect(headerTitles).toContain('CASE AGE (days)');
  });

  it('formats LAST APPEARANCE date using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(ReservedJudgementTable, {
      props: { data: mockData as any },
    });
    const lastAppearanceHeader = wrapper.vm.headers.find((h: any) => h.key === 'appearanceDate');
    expect(lastAppearanceHeader).toBeDefined();
    const formatted = lastAppearanceHeader.value(mockData[0]);
    expect(formatted).toBe(formatDateInstanceToDDMMMYYYY(new Date(mockData[0].appearanceDate)));
  });

  it('sets default sortBy ref', () => {
    const wrapper = mount(ReservedJudgementTable, {
      props: { data: mockData as any },
    });
    expect(wrapper.vm.sortBy[0].key).toBe('cc');
    expect(wrapper.vm.sortBy[0].order).toBe('desc');
  });
});