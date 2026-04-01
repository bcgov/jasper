<template>
  <FullCalendar class="mx-2" :options="calendarOptions" ref="calendarRef">
    <template v-slot:eventContent="{ event }">
      <slot name="eventContent" :event="event" />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { CalendarOptions } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import { ref, watchEffect } from 'vue';

  const calendarRef = ref();

  const props = defineProps<{
    calendarView: string | undefined;
    selectedDate: Date | undefined;
    events: { start: Date; extendedProps: Record<string, unknown> }[];
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
      props.events.forEach((e) => calendarApi.addEvent({ ...e }));
      calendarApi.gotoDate(props.selectedDate);
    }
  });

  const changeView = (view: string) => {
    calendarRef.value?.getApi()?.changeView(view);
  };

  defineExpose({ changeView });
</script>
