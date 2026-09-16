import ChipMultiSelect from '@/components/case-details/common/ChipMultiSelect.vue';
import { shallowMount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('ChipMultiSelect.vue', () => {
  const baseProps = {
    modelValue: [] as string[],
    items: [
      { title: 'Option A', value: 'a', count: 2 },
      { title: 'Option B', value: 'b' },
      { title: 'Option C', value: 'c', count: 0 },
    ],
  };

  it('uses generic defaults for select text', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: baseProps,
    });

    expect(wrapper.vm.getSelectAllTitle()).toBe('Select All');
  });

  it('includes select all count when provided', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        selectAllCount: 12,
      },
    });

    expect(wrapper.vm.getSelectAllTitle()).toBe('Select All (12)');
  });

  it('supports overriding select-all label', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        selectAllLabel: 'Select everything',
      },
    });

    expect(wrapper.vm.getSelectAllTitle()).toBe('Select everything');
  });

  it('formats item title with count only when count is present', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: baseProps,
    });

    expect(wrapper.vm.getItemTitle(baseProps.items[0])).toBe('Option A (2)');
    expect(wrapper.vm.getItemTitle(baseProps.items[1])).toBe('Option B');
    expect(wrapper.vm.getItemTitle(baseProps.items[2])).toBe('Option C (0)');
  });

  it('emits an empty selection when update value is undefined', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: baseProps,
    });

    wrapper.vm.onUpdate(undefined as unknown as string[]);

    expect(wrapper.emitted('update:modelValue')).toEqual([[[]]]);
  });

  it('selects all values when toggleSelectAll is called and not all selected', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: baseProps,
    });

    wrapper.vm.toggleSelectAll();

    expect(wrapper.emitted('update:modelValue')).toEqual([[['a', 'b', 'c']]]);
  });

  it('clears values when toggleSelectAll is called and all selected', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        modelValue: ['a', 'b', 'c'],
      },
    });

    wrapper.vm.toggleSelectAll();

    expect(wrapper.emitted('update:modelValue')).toEqual([[[]]]);
  });

  it('removeValue emits model without removed option', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        modelValue: ['a', 'b'],
      },
    });

    wrapper.vm.removeValue('a');

    expect(wrapper.emitted('update:modelValue')).toEqual([[['b']]]);
  });

  it('toggleValue removes value when selected', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        modelValue: ['b'],
      },
    });

    wrapper.vm.toggleValue('b');

    expect(wrapper.emitted('update:modelValue')).toEqual([[[]]]);
  });

  it('toggleValue adds value when not selected', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        modelValue: ['a'],
      },
    });

    wrapper.vm.toggleValue('c');

    expect(wrapper.emitted('update:modelValue')).toEqual([[['a', 'c']]]);
  });

  it('detects selected values through isSelected', () => {
    const wrapper = shallowMount(ChipMultiSelect, {
      props: {
        ...baseProps,
        modelValue: ['b'],
      },
    });

    expect(wrapper.vm.isSelected('b')).toBe(true);
    expect(wrapper.vm.isSelected('a')).toBe(false);
  });
});
