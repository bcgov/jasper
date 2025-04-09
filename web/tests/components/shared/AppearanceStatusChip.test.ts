import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import AppearanceStatusChip from 'CMP/shared/AppearanceStatusChip.vue';

describe('AppearanceStatusChip.vue', () => {
it.each([
    { status: 'SCHD', expectedText: 'Scheduled', expectedColor: '#195b96' },
    { status: 'CNCL', expectedText: 'Canceled', expectedColor: '#d82a2f' },
    { status: 'UNCF', expectedText: 'Unconfirmed', expectedColor: '#2e8540' },
])('renders the correct text and color for status %s', ({ status, expectedText, expectedColor }) => {
    const wrapper = mount(AppearanceStatusChip, {
        props: { status },
    });
    const chip = wrapper.findComponent('v-chip');
    expect(chip.text()).toBe(expectedText);
    expect(chip.attributes('color')).toBe(expectedColor);
    });
});