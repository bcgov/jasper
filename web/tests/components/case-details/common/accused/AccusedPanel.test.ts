import AccusedPanel from '@/components/case-details/common/accused/AccusedPanel.vue';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('AccusedPanel.vue', () => {
  const accusedMock = [
    { partId: 1, lastNm: 'Smith' },
    { partId: 2, lastNm: 'Johnson' },
  ];
  const appearancesMock = [
    { lastNm: 'Smith', details: 'Appearance 1' },
    { lastNm: 'Johnson', details: 'Appearance 2' },
    { lastNm: 'Smith', details: 'Appearance 3' },
  ];

  it.each([
    ['Adult', 'Accused'],
    ['Youth', 'Youth'],
  ])(
    'renders the correct title for Adult activity class',
    (activityClass, output) => {
      const wrapper = shallowMount(AccusedPanel, {
        props: {
          accused: accusedMock,
          activityClass: activityClass,
          appearances: appearancesMock,
        },
      });
      expect(wrapper.find('h5').text()).toBe(`${output} (2)`);
    }
  );

  it('renders the correct title for Youth activity class', () => {
    const wrapper = shallowMount(AccusedPanel, {
      props: {
        accused: accusedMock,
        activityClass: 'Youth',
        appearances: appearancesMock,
      },
    });
    expect(wrapper.find('h5').text()).toBe('Youth (2)');
  });

  it('renders the correct number of Accused components', () => {
    const wrapper = shallowMount(AccusedPanel, {
      props: {
        accused: accusedMock,
        activityClass: 'Adult',
        appearances: appearancesMock,
      },
    });
    const accusedComponents = wrapper.findAllComponents({ name: 'Accused' });
    expect(accusedComponents).toHaveLength(2);
  });
});
