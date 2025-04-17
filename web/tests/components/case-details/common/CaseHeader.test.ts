import { mdiCalendar, mdiScaleBalance, mdiTextBoxOutline } from '@mdi/js';
import { shallowMount } from '@vue/test-utils';
import CaseHeader from 'CMP/case-details/common/CaseHeader.vue';
import { beforeEach, describe, expect, it } from 'vitest';

describe('CaseHeader.vue', () => {
  const mockDetails = {
    appearances: {
      apprDetail: [
        { id: 1, date: '2023-01-01', description: 'Test Appearance' },
      ],
    },
  };
  let wrapper: any;

  beforeEach(() => {
    wrapper = shallowMount(CaseHeader, {
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

  it('applies active-tab class to the selected tab', async () => {
    wrapper.vm.selectedTab = 'documents';

    const activeTab = wrapper.find('.active-tab');
    expect(activeTab.exists()).toBe(true);
    expect(activeTab.text()).toContain('Documents');
  });
});
