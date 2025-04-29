import ChildrenPanel from '@/components/case-details/civil/children/ChildrenPanel.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('ChildrenPanel.vue', () => {
  const childrensMock: partyType[] = [{} as partyType, {} as partyType];

  it('renders the correct ChildrenPanel title', () => {
    const wrapper = shallowMount(ChildrenPanel, {
      props: {
        children: childrensMock,
      },
    });

    expect(wrapper.find('h5').text()).toBe(
      `Children (${childrensMock.length})`
    );
  });

  it('renders the correct number of ChildrenPanel components', () => {
    const wrapper = shallowMount(ChildrenPanel, {
      props: {
        children: childrensMock,
      },
    });

    const partyComponents = wrapper.findAllComponents({
      name: 'child',
    });
    expect(partyComponents).toHaveLength(childrensMock.length);
  });
});
