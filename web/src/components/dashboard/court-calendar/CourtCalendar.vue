<template>
  <CourtCalendarToolbar
    v-model:selectedDate="selectedDate"
    v-model:isCourtCalendar="isCourtCalendar"
    v-model:calendarView="calendarView"
  />
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
    <!-- This will be worked on separately -->
    <!-- <template v-slot:eventContent="{ event }">
      <MyCalendarDay
        :date="event.extendedProps.date"
        :isWeekend="event.extendedProps.isWeekend"
        :activities="event.extendedProps.activities"
      />
    </template> -->
  </FullCalendar>
</template>
<script setup lang="ts">
  import { DashboardService } from '@/services';
  import { Activity, CalendarDay, Presider } from '@/types';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { CalendarOptions } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import { computed, inject, onMounted, ref, watch, watchEffect } from 'vue';
  import CourtCalendarToolbar from './CourtCalendarToolbar.vue';

  const dashboardService = inject<DashboardService>('dashboardService');

  const TWO_WEEK_CALENDAR_VIEW = 'dayGridTwoWeek';
  const ONE_WEEK_CALENDAR_VIEW = 'dayGridWeek';
  const MONTH_CALENDAR_VIEW = 'dayGridMonth';

  if (!dashboardService) {
    throw new Error('Service is not available!');
  }

  const isCourtCalendar = defineModel<boolean>('isCourtCalendar');

  const isCalendarLoading = ref(true);
  const calendarRef = ref();
  const calendarData = ref<CalendarDay[]>([]);
  const presiders = ref<Presider[]>([]);
  const activities = ref<Activity[]>([]);
  const selectedDate = ref(new Date());
  const calendarView = ref(TWO_WEEK_CALENDAR_VIEW);
  const startDay = ref(new Date(selectedDate.value));
  const endDay = ref(new Date(selectedDate.value));
  const locationIds = ref('');

  const updateCalendar = async () => {
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
        locationIds.value
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
    // Update the start and end days based on the calendar view
    switch (calendarView) {
      case MONTH_CALENDAR_VIEW: {
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
      case TWO_WEEK_CALENDAR_VIEW: {
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
      case ONE_WEEK_CALENDAR_VIEW: {
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
