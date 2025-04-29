import PartiesPanel from '@/components/case-details/civil/parties/PartiesPanel.vue';
import { partyType } from '@/types/civil/jsonTypes';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('PartiesPanel.vue', () => {
  const partiesMock: partyType[] = [{} as partyType, {} as partyType];

  it('renders the correct PartiesPanel title', () => {
    const wrapper = shallowMount(PartiesPanel, {
      props: {
        parties: partiesMock,
        courtClassCd: '',
      },
    });

    expect(wrapper.find('h5').text()).toBe(`Parties (${partiesMock.length})`);
  });

  it('renders the correct number of PartiesPanel components', () => {
    const wrapper = shallowMount(PartiesPanel, {
      props: {
        parties: partiesMock,
        courtClassCd: '',
      },
    });

    const partyComponents = wrapper.findAllComponents({
      name: 'party',
    });
    expect(partyComponents).toHaveLength(partiesMock.length);
  });
});
