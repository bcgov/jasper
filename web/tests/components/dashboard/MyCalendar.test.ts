import MyCalendar from '@/components/dashboard/MyCalendar.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('MyCalendar.vue', () => {
  it('renders FullCalendar', () => {
    const wrapper = mount(MyCalendar, {
      props: {
        data: [],
        selectedDate: new Date(),
        isLoading: false,
      },
    });

    const fcComp = wrapper.find('.fc');

    expect(fcComp.exists()).toBeTruthy();
  });

  it('renders skeleton loader', () => {
    const wrapper = mount(MyCalendar, {
      props: {
        data: [],
        selectedDate: new Date(),
        isLoading: true,
      },
    });

    const fcComp = wrapper.find('.fc');

    expect(fcComp.exists()).toBeFalsy();
  });
});
