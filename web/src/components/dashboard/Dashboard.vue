<template>
  <CourtToday :judgeId="judgeId" v-if="!isCourtCalendar" />
  <CalendarToolbar
    v-model:selectedDate="selectedDate"
    v-model:isCourtCalendar="isCourtCalendar"
    v-model:calendarView="calendarView"
    :isCalendarLoading="isCalendarLoading"
  />
  <CourtCalendarView
    v-if="isCourtCalendar"
    v-model:selectedDate="selectedDate"
    v-model:calendarView="calendarView"
    v-model:isCalendarLoading="isCalendarLoading"
    :judgeId="judgeId"
    :baseCalendarOptions="baseCalendarOptions"
  />
  <MyCalendar
    v-else
    :judgeId="judgeId"
    v-model:selectedDate="selectedDate"
    v-model:isCalendarLoading="isCalendarLoading"
    :baseCalendarOptions="baseCalendarOptions"
  />
  <DashboardPanels :judgeId="judgeId" />
</template>
<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { CalendarViewEnum } from '@/types/common';
  import { CalendarOptions } from '@fullcalendar/vue3';
  import { ref, watch } from 'vue';
  import CalendarToolbar from './CalendarToolbar.vue';
  import CourtCalendarView from './court-calendar/CourtCalendarView.vue';
  import CourtToday from './CourtToday.vue';
  import MyCalendar from './my-calendar/MyCalendar.vue';
  import DashboardPanels from './panels/DashboardPanels.vue';

  const commonStore = useCommonStore();
  const judgeId = ref(commonStore.userInfo?.judgeId);
  const isCourtCalendar = ref(false);
  const selectedDate = ref(new Date());
  const calendarView = ref(CalendarViewEnum.MonthView);
  const isCalendarLoading = ref(true);
  const baseCalendarOptions: CalendarOptions = {
    headerToolbar: false,
    dayHeaderFormat: { weekday: 'long' },
    dayHeaderInnerClass: 'day-header',
    dayMaxEvents: false,
    dayCellClass: (info) => {
      const classes = ['day-cell'];
      if (info.isToday) {
        classes.push('day-cell-today');
      }
      if (info.dow === 0 || info.dow === 6) {
        classes.push('day-cell-weekend');
      }
      return classes.join(' ');
    },
    dayCellTopClass: 'day-cell-top',
    dayCellInnerClass: 'day-cell-inner',
    expandRows: false,
    contentHeight: 'auto',
    aspectRatio: 3,
  };

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal) => (judgeId.value = newVal)
  );

  watch(isCourtCalendar, (newVal) => {
    calendarView.value = newVal
      ? CalendarViewEnum.TwoWeekView
      : CalendarViewEnum.MonthView;
    selectedDate.value = new Date();
  });
</script>

<style>
  /* FullCalendar styles */
  .day-header,
  .day-header:hover {
    font-size: 0.875rem;
    font-weight: normal;
    text-transform: uppercase;
    text-decoration: none;
    color: var(--text-blue-800);
  }

  .day-cell-top {
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    font-weight: bold;
    color: var(--text-blue-800);
    text-decoration: none;
  }

  .day-cell-top > div {
    display: flex;
    align-items: center;
    width: 100%;
    font-size: 1rem;
  }

  .day-cell {
    padding: 0.3125rem !important;
    background-color: var(--bg-white-500) !important;
  }

  .day-cell:hover {
    padding: 0.3125rem !important;
    background-color: var(--bg-blue-100) !important;
  }

  .day-cell-today {
    background-color: var(--bg-blue-50) !important;
    box-shadow: inset 0 4px 0 0 var(--bg-blue-500);
  }

  .day-cell-weekend {
    background-color: var(--bg-gray-400) !important;
  }

  .day-cell-weekend:hover {
    background-color: var(--bg-blue-100) !important;
  }

  .day-cell-inner:hover div div {
    background-color: transparent !important;
  }

  /* Hide the event graphic/dot (first child of each event) */
  .day-cell-inner > div > div > div:first-child {
    display: none !important;
  }

  .day-cell-inner > div > div > div:last-child {
    margin-left: 0.25rem;
    margin-right: 0.25rem;
    font-size: 0.875rem;
  }

  /* Zero-height anchor prepended to each expandable day cell. MyCalendarDayExpanded
     reads this element's position to place the (body-teleported) expanded panel. */
  .fc-expand-wrapper {
    position: relative;
    height: 0;
  }
</style>
