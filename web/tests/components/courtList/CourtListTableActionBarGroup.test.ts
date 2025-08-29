import { CourtListAppearance } from '@/types/courtlist';
import { mount } from '@vue/test-utils';
import CourtListTableActionBarGroup from 'CMP/courtlist/CourtListTableActionBarGroup.vue';
import { describe, expect, it } from 'vitest';

const mockAppearances: CourtListAppearance[] = [
  {
    id: 1,
    courtClassCd: 'A',
  } as unknown as CourtListAppearance,
  {
    id: 2,
    courtClassCd: 'Y',
  } as unknown as CourtListAppearance,
  {
    id: 3,
    courtClassCd: 'C',
  } as unknown as CourtListAppearance,
  {
    id: 4,
    courtClassCd: 'F',
  } as unknown as CourtListAppearance,
];

describe('CourtListTableActionBarGroup.vue', () => {
  it('renders grouped ActionBars for each court class', () => {
    const wrapper = mount(CourtListTableActionBarGroup, {
      props: { selected: mockAppearances },
    });

    expect(wrapper.text()).toContain('Criminal - Adult file/s');
    expect(wrapper.text()).toContain('Youth file/s');
    expect(wrapper.text()).toContain('Small Claims file/s');
    expect(wrapper.text()).toContain('Family file/s');
  });

  it('groups appearances by correct court class', () => {
    const wrapper = mount(CourtListTableActionBarGroup, {
      props: { selected: mockAppearances },
    });

    const actionBars = wrapper.findAllComponents({ name: 'action-bar' });
    expect(actionBars.length).toBe(4);
  });

  it('renders nothing if selected is empty', () => {
    const wrapper = mount(CourtListTableActionBarGroup, {
      props: { selected: [] },
      global: {
        stubs: {
          ActionBar: true,
          'v-btn': true,
        },
      },
    });
    expect(wrapper.html()).not.toContain('file/s');
  });

  it('clicking the first View Case Details button should emit an event and payload from the first mock data', () => {
    const wrapper = mount(CourtListTableActionBarGroup, {
      props: { selected: mockAppearances },
    });

    const firstViewButton = wrapper.find('[data-testid="view-case-details"]');
    firstViewButton.trigger('click');

    expect(wrapper.emitted('view-case-details')).toBeTruthy();
    expect(wrapper.emitted('view-case-details')![0][0]).toEqual([
      mockAppearances[0],
    ]);
  });
});
