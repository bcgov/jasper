import { shallowMount } from '@vue/test-utils';
import CivilSidePanel from 'CMP/case-details/CivilSidePanel.vue';
import { describe, expect, it } from 'vitest';

describe('CivilSidePanel.vue', () => {
  it('renders AdjudicatorRestrictionsPanel component', () => {
    const wrapper = shallowMount(CivilSidePanel, {
      props: { details: {}, adjudicatorRestrictions: [{}] },
    });

    const arComponent = wrapper.findComponent({
      name: 'AdjudicatorRestrictionsPanel',
    });
    expect(arComponent.exists()).toBe(true);
  });
});
