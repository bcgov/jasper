<template>
  <FullCalendar class="mx-2" :options="calendarOptions">
    <template v-slot:eventContent="{ event }">
      <slot name="eventContent" :event="event" />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import FullCalendar, {
    CalendarOptions,
    useCalendarController,
  } from '@fullcalendar/vue3';
  import dayGridPlugin from '@fullcalendar/vue3/daygrid';
  import classicThemePlugin from '@fullcalendar/vue3/themes/classic';
  import { computed, watch } from 'vue';

  const controller = useCalendarController();

  const props = defineProps<{
    calendarView: string | undefined;
    selectedDate: Date | undefined;
    events: { start: Date; extendedProps: Record<string, unknown> }[];
    baseCalendarOptions: CalendarOptions;
  }>();

  const calendarOptions = computed<CalendarOptions>(() => ({
    ...props.baseCalendarOptions,
    controller,
    initialView: props.calendarView,
    initialDate: props.selectedDate,
    plugins: [classicThemePlugin, dayGridPlugin],
    events: props.events,
    views: {
      dayGridTwoWeek: {
        type: 'dayGrid',
        duration: { weeks: 2 },
      },
    },
  }));

  watch(
    () => props.selectedDate,
    (date) => {
      if (date) {
        controller.gotoDate(date);
      }
    }
  );

  const changeView = (view: string) => {
    controller.changeView(view);
  };

  defineExpose({ changeView });
</script>
