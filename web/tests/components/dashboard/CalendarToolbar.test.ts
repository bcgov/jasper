import CalendarToolbar from '@/components/dashboard/CalendarToolbar.vue';
import { CalendarViewEnum } from '@/types/common';
import { formatDateInstanceToMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import { DateTime } from 'luxon';
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

    it('clicking previous button inside month picker should update the selectedDate to previous year', () => {
      const currentDate = new Date();

      const wrapper = mountComponent({
        selectedDate: currentDate,
        isCourtCalendar: false,
      });

      const previousBtn = wrapper.find('[data-testid="previous-year"]');
      previousBtn.trigger('click');

      expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
        currentDate.getFullYear() - 1
      );
      expect(wrapper.vm.selectedDate?.getMonth()).toEqual(
        currentDate.getMonth()
      );

      expect(wrapper.vm.selectedDate?.getDate()).toEqual(currentDate.getDate());
    });

    it('clicking next button inside month picker should update the selectedDate to next year', () => {
      const currentDate = new Date();

      const wrapper = mountComponent({
        selectedDate: currentDate,
        isCourtCalendar: false,
      });

      const previousBtn = wrapper.find('[data-testid="next-year"]');
      previousBtn.trigger('click');

      expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
        currentDate.getFullYear() + 1
      );
      expect(wrapper.vm.selectedDate?.getMonth()).toEqual(
        currentDate.getMonth()
      );
      expect(wrapper.vm.selectedDate?.getDate()).toEqual(currentDate.getDate());
    });
  });

  // Helper function to test date navigation
  const testDateNavigation = (
    buttonTestId: string,
    offset: number,
    direction: 'previous' | 'next',
    calendarView: CalendarViewEnum = CalendarViewEnum.TwoWeekView
  ) => {
    const currentDate = new Date();

    const wrapper = mountComponent({
      selectedDate: currentDate,
      isCourtCalendar: true,
      calendarView,
    });

    const button = wrapper.find(`[data-testid="${buttonTestId}"]`);
    button.trigger('click');

    // Use the same logic as the component's changeDate function
    let expectedDateTime = DateTime.fromJSDate(currentDate);

    switch (calendarView) {
      case CalendarViewEnum.WeekView:
        expectedDateTime = expectedDateTime.plus({ weeks: offset });
        break;
      case CalendarViewEnum.TwoWeekView:
        expectedDateTime = expectedDateTime.plus({ weeks: offset * 2 });
        break;
      case CalendarViewEnum.MonthView:
        expectedDateTime = expectedDateTime.plus({ months: offset });
        break;
      default:
        expectedDateTime = expectedDateTime.plus({ years: offset });
        break;
    }

    const expectedDate = expectedDateTime.toJSDate();

    expect(wrapper.vm.selectedDate?.getFullYear()).toEqual(
      expectedDate.getFullYear()
    );
    expect(wrapper.vm.selectedDate?.getMonth()).toEqual(
      expectedDate.getMonth()
    );
    expect(wrapper.vm.selectedDate?.getDate()).toEqual(expectedDate.getDate());
  };

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

    it('clicking previous button while in 2-week view should update the selectedDate to previous 2 weeks', () => {
      testDateNavigation(
        'previous',
        -1,
        'previous',
        CalendarViewEnum.TwoWeekView
      );
    });

    it('clicking next button while in 2-week view should update the selectedDate to next 2 weeks', () => {
      testDateNavigation('next', 1, 'next', CalendarViewEnum.TwoWeekView);
    });

    it('clicking previous button while in week view should update the selectedDate to previous week', () => {
      testDateNavigation('previous', -1, 'previous', CalendarViewEnum.WeekView);
    });

    it('clicking next button while in week view should update the selectedDate to next week', () => {
      testDateNavigation('next', 1, 'next', CalendarViewEnum.WeekView);
    });

    it('clicking previous button while in month view should update the selectedDate to previous month', () => {
      testDateNavigation(
        'previous',
        -1,
        'previous',
        CalendarViewEnum.MonthView
      );
    });

    it('clicking next button while in month view should update the selectedDate to next month', () => {
      testDateNavigation('next', 1, 'next', CalendarViewEnum.MonthView);
    });
  });
});
