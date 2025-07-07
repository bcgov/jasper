<template>
  <v-skeleton-loader
    v-if="isLoading"
    type="date-picker"
    :loading="isLoading"
  ></v-skeleton-loader>
  <FullCalendar
    v-else="isLoading"
    class="m-3"
    :options="calendarOptions"
    ref="calendarRef"
  >
    <template v-slot:eventContent="{ event }">
      <MyCalendarDay
        :isWeekend="event.extendedProps.isWeekend"
        :activities="event.extendedProps.activities"
      />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { CalendarDay } from '@/types';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { CalendarOptions, DayCellMountArg } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import { mdiListBoxOutline } from '@mdi/js';
  import { computed, ref, watchEffect } from 'vue';
  import MyCalendarDay from './MyCalendarDay.vue';

  const props = defineProps<{
    data: CalendarDay[];
    selectedDate: Date;
    isLoading: boolean;
  }>();

  const calendarEvents = computed(() =>
    props.data.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      },
    }))
  );

  const dayCellDidMount = (info: DayCellMountArg) => {
    // Appends the Court List icon next to the day's date
    const date = formatDateInstanceToDDMMMYYYY(info.date);
    const data = props.data.find((d) => d.date === date);
    const dayTop = info.el.querySelector('.fc-daygrid-day-top');

    if (
      !data ||
      data.activities.length === 0 ||
      !data.showCourtList ||
      !dayTop
    ) {
      return;
    }

    const link = document.createElement('a');
    link.className = 'court-list';
    link.href = `/court-list?date=${date}`;
    link.title = 'View Court List';

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('width', '18');
    svg.setAttribute('height', '18');

    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute('d', mdiListBoxOutline);

    svg.appendChild(path);
    link.appendChild(svg);
    dayTop.appendChild(link);
  };

  const calendarOptions: CalendarOptions = {
    initialView: 'dayGridMonth',
    plugins: [dayGridPlugin],
    headerToolbar: false,
    dayHeaderFormat: { weekday: 'long' },
    dayCellDidMount,
  };

  const calendarRef = ref();

  watchEffect(() => {
    const calendarApi = calendarRef.value?.getApi();
    if (calendarApi) {
      calendarApi.removeAllEvents();

      calendarEvents.value.forEach((e) => {
        return calendarApi.addEvent({ ...e });
      });
      calendarApi.gotoDate(props.selectedDate);
    }
  });
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
    color: var(--text-gray-400);
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
