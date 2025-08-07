import CalendarToolbar from '@/components/dashboard/CalendarToolbar.vue';
import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('CalendarToolbar.vue', () => {
  const mountComponent = (props: any) => {
    return mount(CalendarToolbar, {
      props: {
        ...props,
      },
    });
  };

  describe('MyCalendar tests', () => {
    it('render common and MyCalendar components when isCourtCalendar is false', () => {
      const date = new Date();

      const wrapper = mountComponent({
        selectedDate: date,
        isCourtCalendar: false,
      });

      expect(wrapper.find('[data-testid="title"]').text()).toEqual(
        `Schedule for ${formatDateInstanceToMMMYYYY(date)}`
      );
      expect(
        wrapper.find('[data-testid="calendar-toggle-link"]').text()
      ).toEqual(`View Other Calendars`);
      expect(wrapper.find('[data-testid="month-picker"]').exists()).toEqual(
        true
      );
      expect(wrapper.find('[data-testid="date-picker"]').exists()).toEqual(
        false
      );
      expect(wrapper.find('[data-testid="today-button"]').exists()).toEqual(
        true
      );
      expect(
        wrapper.find('[data-testid="calendar-view-picker"]').exists()
      ).toEqual(false);
    });

    it('sets selectedDate to be today when Today button is clicked', () => {
      const pastDate = new Date(2023, 0, 1); // January 1, 2023

      const wrapper = mountComponent({
        selectedDate: pastDate,
        isCourtCalendar: false,
      });

      const todayBtn = wrapper.find('[data-testid="today-button"]');
      todayBtn.trigger('click');

      const today = new Date();
      expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
        today.getFullYear()
      );
      expect(wrapper.vm.selectedDate?.getMonth()).toEqual(today.getMonth());
      expect(wrapper.vm.selectedDate?.getDate()).toEqual(today.getDate());
    });

    it('sets selectedDate to previous month when previous button is clicked', () => {
      const currentDate = new Date();

      const wrapper = mountComponent({
        selectedDate: currentDate,
        isCourtCalendar: false,
      });

      const previousBtn = wrapper.find('[data-testid="previous-button"]');
      previousBtn.trigger('click');

      expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
        currentDate.getFullYear() - 1
      );
      expect(wrapper.vm.selectedDate?.getMonth()).toEqual(
        currentDate.getMonth()
      );
      expect(wrapper.vm.selectedDate?.getDate()).toEqual(1);
    });

    it('sets selectedDate to previous month when next button is clicked', () => {
      const currentDate = new Date();

      const wrapper = mountComponent({
        selectedDate: currentDate,
        isCourtCalendar: false,
      });

      const previousBtn = wrapper.find('[data-testid="next-button"]');
      previousBtn.trigger('click');

      expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
        currentDate.getFullYear() + 1
      );
      expect(wrapper.vm.selectedDate?.getMonth()).toEqual(
        currentDate.getMonth()
      );
      expect(wrapper.vm.selectedDate?.getDate()).toEqual(1);
    });
  });

  describe('CourtCalendar tests', () => {
    it('renders common and CourtCalendar components when isCourtCalendar is true', () => {
      const date = new Date();

      const wrapper = mountComponent({
        selectedDate: date,
        isCourtCalendar: true,
      });

      expect(wrapper.find('[data-testid="title"]').text()).toEqual(
        `Schedule for ${formatDateInstanceToMMMYYYY(date)}`
      );
      expect(
        wrapper.find('[data-testid="calendar-toggle-link"]').text()
      ).toEqual(`View My Calendar`);
      expect(wrapper.find('[data-testid="month-picker"]').exists()).toEqual(
        false
      );
      expect(wrapper.find('[data-testid="date-picker"]').exists()).toEqual(
        true
      );
      expect(wrapper.find('[data-testid="today-button"]').exists()).toEqual(
        true
      );
      expect(
        wrapper.find('[data-testid="calendar-view-picker"]').exists()
      ).toEqual(true);
    });
  });
});
