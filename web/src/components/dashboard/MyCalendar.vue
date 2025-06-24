<template>
  <FullCalendar class="m-3" :options="calendarOptions" ref="calendarRef">
    <template v-slot:eventContent="{ event }">
      <MyCalendarDay :activities="event.extendedProps.activities" />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { CalendarDayV2 } from '@/types';
  import { CalendarOptions } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';

  import { ref, watchEffect, computed } from 'vue';

  const props = defineProps<{
    data: CalendarDayV2[];
    selectedDate: Date;
  }>();

  const calendarEvents = computed(() =>
    props.data.map((d) => ({ start: new Date(d.date), ...d }))
  );

  const calendarOptions: CalendarOptions = {
    initialView: 'dayGridMonth',
    plugins: [dayGridPlugin],
    headerToolbar: false,
    dayHeaderFormat: { weekday: 'long' },
    events: calendarEvents.value,
  };

  const calendarRef = ref();

  watchEffect(() => {
    const calendarApi = calendarRef.value?.getApi();
    if (calendarApi) {
      calendarApi.removeAllEvents();

      props.data.forEach((e) =>
        calendarApi.addEvent({ start: new Date(e.date), ...e })
      );
      calendarApi.gotoDate(props.selectedDate);
    }
  });
</script>
<style scoped>
  /* FullCalendar Styles */
  :deep(.fc-col-header-cell) {
    border-top: none !important;
    border-left: none !important;
    border-right: none !important;
  }

  :deep(.fc-col-header-cell-cushion),
  :deep(.fc-col-header-cell-cushion:hover) {
    color: var(--text-blue-800);
    font-size: 0.875rem;
    font-weight: normal;
    text-transform: uppercase !important;
    text-decoration: none;
  }

  :deep(.fc-daygrid-day-top) {
    flex-direction: row;
  }

  :deep(.fc-daygrid-day-number),
  :deep(.fc-daygrid-day-number:hover) {
    font-weight: bold;
    color: var(--text-gray-400) !important;
    text-decoration: none;
  }

  :deep(.fc-day-sun .fc-daygrid-day-frame),
  :deep(.fc-day-sat .fc-daygrid-day-frame) {
    background-color: var(--bg-gray-400) !important;
  }

  :deep(.fc-daygrid-day) {
    background-color: var(--bg-white-500) !important;
  }
</style>
