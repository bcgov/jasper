import MyCalendar from '@/components/dashboard/MyCalendar.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('MyCalendar.vue', () => {
  it('renders FullCalendar', () => {
    const wrapper = mount(MyCalendar, {
      props: {
        data: [],
        selectedDate: new Date(),
      },
    });

    const fcComp = wrapper.find('.fc');

    expect(fcComp.exists()).toBeTruthy();
  });
});
