<template>
  <v-skeleton-loader
    v-if="isCalendarLoading"
    type="date-picker"
    :loading="isCalendarLoading"
  ></v-skeleton-loader>
  <FullCalendar
    class="mx-2"
    v-else
    :options="calendarOptions"
    ref="calendarRef"
  >
    <template v-slot:eventContent="{ event }">
      <CourtCalendarDay
        :activities="event.extendedProps.activities"
        :date="event.start"
      />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { DashboardService } from '@/services';
  import { Activity, CalendarDay, Presider } from '@/types';
  import { CalendarViewEnum } from '@/types/common';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { CalendarOptions } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import { computed, inject, onMounted, ref, watch, watchEffect } from 'vue';

  const dashboardService = inject<DashboardService>('dashboardService');

  if (!dashboardService) {
    throw new Error('Service is not available!');
  }

  const props = defineProps<{
    judgeId: number | undefined;
  }>();

  const selectedDate = defineModel<Date>('selectedDate');
  const calendarView = defineModel<string>('calendarView');
  const isCalendarLoading = defineModel<boolean>('isCalendarLoading');

  if (!selectedDate.value) {
    throw new Error('selectedDate is required');
  }

  const calendarRef = ref();
  const calendarData = ref<CalendarDay[]>([]);
  const presiders = ref<Presider[]>([]);
  const activities = ref<Activity[]>([]);

  const startDay = ref(new Date(selectedDate.value));
  const endDay = ref(new Date(selectedDate.value));
  const locationIds = ref('');

  const updateCalendar = async () => {
    if (!calendarView.value) {
      throw new Error('calendarView is required');
    }

    calculateDateRange(calendarView.value);
    await loadCalendarData();

    const calendarApi = calendarRef.value?.getApi();
    calendarApi.changeView(calendarView.value);
  };

  const loadCalendarData = async () => {
    isCalendarLoading.value = true;
    try {
      const { payload } = await dashboardService.getCourtCalendar(
        formatDateInstanceToDDMMMYYYY(startDay.value),
        formatDateInstanceToDDMMMYYYY(endDay.value),
        locationIds.value,
        props.judgeId
      );
      calendarData.value = [...payload.days];
      presiders.value = [...payload.presiders];
      activities.value = [...payload.activities];
    } catch (error) {
      console.error('Failed to load calendar data:', error);
    } finally {
      isCalendarLoading.value = false;
    }
  };

  const calendarEvents = computed(() =>
    calendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as CalendarDay,
    }))
  );

  const calendarEventsWithActivities = computed(() =>
    calendarData.value.filter((d) => d.activities.length > 0 && d.showCourtList)
  );

  const calendarOptions: CalendarOptions = {
    initialView: calendarView.value,
    plugins: [dayGridPlugin],
    headerToolbar: false,
    dayHeaderFormat: { weekday: 'long' },
    dayMaxEventRows: true,
    expandRows: false,
    contentHeight: 'auto',
    views: {
      dayGridTwoWeek: {
        type: 'dayGrid',
        duration: { weeks: 2 },
      },
    },
  };

  onMounted(updateCalendar);

  watch(selectedDate, updateCalendar);

  watch(calendarView, updateCalendar);

  watch(() => props.judgeId, updateCalendar);

  watchEffect(() => {
    const calendarApi = calendarRef.value?.getApi();
    if (calendarApi) {
      calendarApi.removeAllEvents();

      calendarEvents.value.forEach((e) => {
        return calendarApi.addEvent({ ...e });
      });
      calendarApi.gotoDate(selectedDate.value);
    }
  });

  const calculateDateRange = (calendarView: string) => {
    if (!selectedDate.value) {
      throw new Error('selectedDate is required');
    }

    // Update the start and end days based on the calendar view
    switch (calendarView) {
      case CalendarViewEnum.MonthView: {
        // First and last day of the month
        startDay.value = new Date(
          selectedDate.value.getFullYear(),
          selectedDate.value.getMonth(),
          1
        );
        endDay.value = new Date(
          selectedDate.value.getFullYear(),
          selectedDate.value.getMonth() + 1,
          0
        );
        break;
      }
      case CalendarViewEnum.TwoWeekView: {
        // Two weeks starting from the first Sunday before the selected date
        const sunday = new Date(selectedDate.value);
        sunday.setDate(
          selectedDate.value.getDate() - selectedDate.value.getDay()
        );

        const twoWeeksLater = new Date(sunday);
        twoWeeksLater.setDate(sunday.getDate() + 13);

        startDay.value = sunday;
        endDay.value = twoWeeksLater;
        break;
      }
      case CalendarViewEnum.WeekView: {
        // One week starting from the first Sunday before the selected date
        const sunday = new Date(selectedDate.value);
        sunday.setDate(
          selectedDate.value.getDate() - selectedDate.value.getDay()
        );

        const saturday = new Date(sunday);
        saturday.setDate(sunday.getDate() + 6);

        startDay.value = sunday;
        endDay.value = saturday;
        break;
      }
    }
  };
</script>
<style scoped>
  :deep(.court-list) {
    margin-right: 4px;
  }

  /* Header Styles */
  :deep(.fc-col-header-cell-cushion),
  :deep(.fc-col-header-cell-cushion:hover) {
    color: var(--text-blue-800);
    font-size: 0.875rem;
    font-weight: normal;
    text-transform: uppercase !important;
    text-decoration: none;
  }

  :deep(.fc-event) {
    display: block;
  }

  /* Day Styles */
  :deep(.fc-daygrid-day-top) {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
  }

  :deep(.fc-daygrid-day-number),
  :deep(.fc-daygrid-day-number:hover) {
    font-weight: bold;
    color: var(--text-blue-800);
    text-decoration: none;
  }

  :deep(.fc-daygrid-day-frame) {
    padding: 0.3125rem;
  }

  :deep(.fc-daygrid-day) {
    background-color: var(--bg-white-500) !important;
  }

  :deep(.fc-daygrid-day:hover) {
    background-color: var(--bg-blue-100) !important;
  }

  :deep(.fc-daygrid-dot-event:hover) {
    background-color: transparent;
  }

  /* Today Styles */
  :deep(.fc-day-today .fc-daygrid-day-frame) {
    position: relative;
    background-color: var(--bg-blue-50) !important;
  }

  :deep(.fc-day-today .fc-daygrid-day-frame)::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    height: 5px;
    width: 100%;
    background-color: var(--bg-blue-500);
  }

  /* Weekend Styles */
  :deep(.fc-day-sun .fc-daygrid-day-frame),
  :deep(.fc-day-sat .fc-daygrid-day-frame) {
    background-color: var(--bg-gray-400) !important;
  }

  :deep(.fc-day-sun .fc-daygrid-day-frame:hover),
  :deep(.fc-day-sat .fc-daygrid-day-frame:hover) {
    background-color: var(--bg-blue-100) !important;
  }
</style>
