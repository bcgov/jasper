
import { describe, it, expect } from 'vitest';
import { shallowMount } from '@vue/test-utils';
import CriminalSidePanel from 'CMP/case-details/CriminalSidePanel.vue';

describe('CriminalSidePanel.vue', () => {
    it('renders Summary component', () => {
        const wrapper = shallowMount(CriminalSidePanel, {
            props: { details: {} }
        });

        const summaryComponent = wrapper.findComponent({ name: 'Summary' });
        expect(summaryComponent.exists()).toBe(true);
    });

    it('renders AccusedPanel component', () => {
        const wrapper = shallowMount(CriminalSidePanel, {
            props: { details: {} }
        });

        const summaryComponent = wrapper.findComponent({ name: 'AccusedPanel' });
        expect(summaryComponent.exists()).toBe(true);
    });
});