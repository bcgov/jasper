import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import CaseHeader from 'CMP/case-details/CaseHeader.vue';
import { mdiCalendar, mdiScaleBalance, mdiTextBoxOutline } from '@mdi/js';

describe('CaseHeader.vue', () => {
  const mockDetails = {
    appearances: {
      apprDetail: [{ id: 1, date: '2023-01-01', description: 'Test Appearance' }],
    },
  };
  let wrapper: any;

  beforeEach(() => {
    wrapper = mount(CaseHeader, {
      props: { details: mockDetails },
    });
  });

  it('renders tabs with correct icons and labels', () => {
    const tabs = wrapper.findAll('v-tab');
    expect(tabs).toHaveLength(3);

    expect(tabs[0].text()).toContain('Documents');
    expect(tabs[0].attributes('prepend-icon')).toBe(mdiTextBoxOutline);

    expect(tabs[1].text()).toContain('Appearances');
    expect(tabs[1].attributes('prepend-icon')).toBe(mdiCalendar);

    expect(tabs[2].text()).toContain('Sentence/order details');
    expect(tabs[2].attributes('prepend-icon')).toBe(mdiScaleBalance);

  });

  it('renders the AppearancesView component when the "appearances" tab is selected', async () => {
    wrapper.vm.selectedTab = 'appearances';

    const appearancesView = wrapper.findComponent({ name: 'AppearancesView' });
    expect(appearancesView.exists()).toBe(true);
    expect(appearancesView.props('appearances')).toEqual(mockDetails.appearances.apprDetail);
  });

  it('applies active-tab class to the selected tab', async () => {
    wrapper.vm.selectedTab = 'appearances';

    const activeTab = wrapper.find('.active-tab');
    expect(activeTab.exists()).toBe(true);
    expect(activeTab.text()).toContain('Appearances');
  });
});