import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import ActionBar from 'CMP/shared/table/ActionBar.vue';

describe('ActionBar.vue', () => {
    it('renders correctly when there are selected items', () => {
        const wrapper = mount(ActionBar, {
            props: {
                selected: [1, 2, 3],
            },
        });
        expect(wrapper.find('v-app-bar').exists()).toBe(true);
        expect(wrapper.find('v-app-bar-title').text()).toBe('3 selected');
    });

    it('does not render when there are no selected items', () => {
        const wrapper = mount(ActionBar, {
            props: {
                selected: [],
            },
        });
        expect(wrapper.find('v-app-bar').exists()).toBe(false);
    });
});