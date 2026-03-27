<template>
  <FullCalendar class="mx-2" :options="calendarOptions" ref="calendarRef">
    <template v-slot:eventContent="{ event }">
      <CourtCalendarActivityDay
        :locations="event.extendedProps.locations"
        :date="event.start"
      />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { CalendarOptions } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import CourtCalendarActivityDay from './CourtCalendarActivityDay.vue';
  import { ref, watchEffect } from 'vue';

  const calendarRef = ref();

  const props = defineProps<{
    calendarView: string;
    selectedDate: Date;
  }>();

  const calendarOptions: CalendarOptions = {
    initialView: props.calendarView,
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
