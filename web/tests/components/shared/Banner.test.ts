import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import Banner from 'CMP/shared/Banner.vue';

describe('Banner.vue', () => {
    it('renders the title correctly', () => {
        const wrapper = mount(Banner, {
            props: {
                title: 'Test Title',
                color: 'white',
                bgColor: 'blue',
            },
        });
        expect(wrapper.find('h3').text()).toBe('Test Title');
    });

    it('applies the correct styles', () => {
        const wrapper = mount(Banner, {
            props: {
                title: 'Test Title',
                color: 'orange',
                bgColor: 'red',
            },
        });
        const banner = wrapper.find('v-banner');
        expect(banner.attributes('style')).toContain('background-color: red;');
        expect(banner.attributes('style')).toContain('color: orange;');
    });
});