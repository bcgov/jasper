<template>
  <FullCalendar class="mx-2" :options="calendarOptions" ref="calendarRef">
    <template v-slot:eventContent="{ event }">
      <slot name="eventContent" :event="event" />
    </template>
  </FullCalendar>
</template>
<script setup lang="ts">
  import { Activity, CalendarDay, CourtCalendarDay, Presider } from '@/types';
  import { ActivityClassEnum } from '@/types/common';
  import { LocationInfo } from '@/types/courtlist';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
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

  const selectedDate = defineModel<Date>('selectedDate');
  const calendarView = defineModel<string>('calendarView');
  const isCalendarLoading = defineModel<boolean>('isCalendarLoading');
  const isLocationFilterLoading = ref(true);
  const isPresidersView = ref(false);

  if (!selectedDate.value) {
    throw new Error('selectedDate is required');
  }

  const calendarRef = ref();
  const presidersCalendarData = ref<CalendarDay[]>([]);
  const activitiesCalendarData = ref<CourtCalendarDay[]>([]);
  const locations = ref<LocationInfo[]>([]);
  const presiders = ref<Presider[]>([]);
  const activities = ref<Activity[]>([]);
  const selectedLocationIds = ref<string[]>([]);
  const selectedPresiderIds = ref<string[]>([]);
  const selectedActivityClass = ref<string>('all');
  const startDay = ref(new Date(selectedDate.value));
  const endDay = ref(new Date(selectedDate.value));
  const locationIds = computed(() => selectedLocationIds.value.join(','));

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
      if (isPresidersView.value) {
        await loadPresidersCalendar();
      } else {
        await loadActivitiesCalendar();
      }
    } catch (error) {
      console.error('Failed to load calendar data:', error);
    } finally {
      isCalendarLoading.value = false;
    }
  };

  const loadPresidersCalendar = async () => {
    const { payload } = await dashboardService.getCourtCalendarPresiders(
      formatDateInstanceToDDMMMYYYY(startDay.value),
      formatDateInstanceToDDMMMYYYY(endDay.value),
      locationIds.value,
      props.judgeId
    );
    presidersCalendarData.value = [...payload.days];
    presiders.value = [...payload.presiders];
    activities.value = [...payload.activities];
  };

  const loadActivitiesCalendar = async () => {
    const { payload } = await dashboardService.getCourtCalendarActivities(
      formatDateInstanceToDDMMMYYYY(startDay.value),
      formatDateInstanceToDDMMMYYYY(endDay.value),
      locationIds.value
    );
    activitiesCalendarData.value = [...payload.days];
    activities.value = [...payload.activities];
  };

  const filteredCalendarData = computed(() =>
    presidersCalendarData.value.map((day) => ({
      ...day,
      activities: day.activities.filter(
        (a) =>
          (selectedPresiderIds.value.length === 0 ||
            selectedPresiderIds.value.includes(a.judgeId.toString())) &&
          // If 'all' is selected, include all activities.
          // If 'non-sitting' is selected, include only non-sitting activities.
          // If 'sitting' is selected, exclude non-sitting activities.
          (selectedActivityClass.value === 'all' ||
            (selectedActivityClass.value === ActivityClassEnum.NonSitting
              ? a.activityClassCode === ActivityClassEnum.NonSitting
              : a.activityClassCode !== ActivityClassEnum.NonSitting))
      ),
    }))
  );

  const calendarEvents = computed(() =>
    filteredCalendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as CalendarDay,
    }))
  );

  const filteredActivitiesCalendarData = computed(() =>
    activitiesCalendarData.value.map((day) => ({
      ...day,
      locations: day.locations,
    }))
  );

  const activitiesCalendarEvents = computed(() =>
    activitiesCalendarData.value.map((d) => ({
      start: new Date(d.date),
      extendedProps: {
        ...d,
      } as CourtCalendarDay,
    }))
  );

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
