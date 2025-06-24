import MonthPicker from '@/components/dashboard/MonthPicker.vue';
import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('MonthPicker.vue', () => {
  it('renders MonthPicker', () => {
    const date = new Date();

    const wrapper = mount(MonthPicker, {
      props: {
        selectedDate: date,
      },
    });

    const titleEl = wrapper.find('[data-testid="title"]');
    const dpEl = wrapper.find('v-date-picker');
    const menuEl = wrapper.find('v-menu');

    expect(titleEl.text()).toBe(
      `Schedule for ${formatDateInstanceToMMMYYYY(date)}`
    );
    expect(dpEl.exists()).toBeTruthy();
    expect(menuEl.exists()).toBeTruthy();
  });
});
