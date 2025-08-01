import MyCalendarActions from '@/components/dashboard/my-calendar/MyCalendarActions.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('MyCalendarActions.vue', () => {
  it('renders MyCalendarActions', () => {
    const date = new Date();

    const wrapper = mount(MyCalendarActions, {
      props: {
        selectedDate: date,
      },
    });

    const dpEl = wrapper.find('v-date-picker');
    const menuEl = wrapper.find('v-menu');

    expect(dpEl.exists()).toBeTruthy();
    expect(menuEl.exists()).toBeTruthy();
  });
});
