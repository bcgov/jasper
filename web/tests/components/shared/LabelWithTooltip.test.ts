import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
import { mount, shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('LabelWithTooltip', () => {
  it('renders single value without tooltip', () => {
    const wrapper = mount(LabelWithTooltip, {
      props: {
        values: ['Item 1'],
      },
    });

    expect(wrapper.find('v-tooltip').attributes('disabled')).toBe(
      true.toString()
    );
  });

  it('renders the tooltip with value from the second item', () => {
    const wrapper = shallowMount(LabelWithTooltip, {
      props: {
        values: ['Item 1', 'Item 2'],
      },
    });

    expect(wrapper.find('v-tooltip').text()).toBe('Item 2');
  });
});
