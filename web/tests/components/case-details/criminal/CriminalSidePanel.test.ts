import { shallowMount } from '@vue/test-utils';
import CriminalSidePanel from 'CMP/case-details/criminal/CriminalSidePanel.vue';
import { describe, expect, it } from 'vitest';

describe('CriminalSidePanel.vue', () => {
  it('renders Summary component', () => {
    const wrapper = shallowMount(CriminalSidePanel, {
      props: { details: {} },
    });

    const summaryComponent = wrapper.findComponent({ name: 'CriminalSummary' });
    expect(summaryComponent.exists()).toBe(true);
  });

  it('renders AccusedPanel component', () => {
    const wrapper = shallowMount(CriminalSidePanel, {
      props: { details: {} },
    });

    const accusedComponent = wrapper.findComponent({ name: 'AccusedPanel' });
    expect(accusedComponent.exists()).toBe(true);
  });

  it('renders AdjudicatorRestrictionsPanel component', () => {
    const wrapper = shallowMount(CriminalSidePanel, {
      props: { details: {}, adjudicatorRestrictions: [{}] },
    });

    const arComponent = wrapper.findComponent({
      name: 'AdjudicatorRestrictionsPanel',
    });
    expect(arComponent.exists()).toBe(true);
  });
});
