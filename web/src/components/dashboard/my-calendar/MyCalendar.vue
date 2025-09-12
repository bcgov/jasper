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
      <MyCalendarDay
        :date="event.extendedProps.date"
        :isWeekend="event.extendedProps.isWeekend"
        :activities="event.extendedProps.activities"
      />
    </template>
  </FullCalendar>
  <!--
    This component is teleported into a specific calendar cell so
    it appears inside or over that day, even though it's rendered
    outside FullCalendar. Only events with activities will have
    the expanded panel.
  -->
  <template v-for="day in calendarEventsWithActivities">
    <MyCalendarDayExpanded
      :expandedDate
      :day="day"
      :close="closeExpandedPanel"
    />
  </template>
</template>
<script setup lang="ts">
  import { DashboardService } from '@/services';
  import { CalendarDay } from '@/types';
  import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
  import { CalendarOptions, DayCellMountArg } from '@fullcalendar/core';
  import dayGridPlugin from '@fullcalendar/daygrid';
  import FullCalendar from '@fullcalendar/vue3';
  import { mdiListBoxOutline } from '@mdi/js';
  import { computed, inject, onMounted, ref, watch, watchEffect } from 'vue';
  import MyCalendarDay from './MyCalendarDay.vue';

  const dashboardService = inject<DashboardService>('dashboardService');

  if (!dashboardService) {
    throw new Error('Service is not available!');
  }

  const props = defineProps<{
    judgeId: number | undefined;
  }>();

  const selectedDate = defineModel<Date>('selectedDate')!;
  const isCalendarLoading = defineModel<boolean>('isCalendarLoading');

  if (!selectedDate.value) {
    throw new Error('selectedDate is required');
  }

  const calendarData = ref<CalendarDay[]>([]);
  const expandedDate = ref<string | null>(null);
  const calendarRef = ref();

  let startDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth(),
    1
  );
  let endDay = new Date(
    selectedDate.value.getFullYear(),
    selectedDate.value.getMonth() + 1,
    0
  );

  onMounted(async () => {
    await loadCalendarData();
  });

  watch(selectedDate, async (newDate) => {
    if (newDate) {
      startDay = new Date(newDate.getFullYear(), newDate.getMonth(), 1);
      endDay = new Date(newDate.getFullYear(), newDate.getMonth() + 1, 0);
    }
    await loadCalendarData();
  });

  watch(
    () => props.judgeId,
    async () => {
      await loadCalendarData();
    }
  );

  const loadCalendarData = async () => {
    isCalendarLoading.value = true;
    try {
      const { payload } = await dashboardService.getMySchedule(
        formatDateInstanceToDDMMMYYYY(startDay),
        formatDateInstanceToDDMMMYYYY(endDay),
        props.judgeId
      );
      calendarData.value = [...payload];
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

  const dayCellDidMount = (info: DayCellMountArg) => {
    // Appends the Court List icon next to the day's date
    const date = formatDateInstanceToDDMMMYYYY(info.date);
    const data = calendarData.value.find((d) => d.date === date);
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

    // Attach a click event for the expanded panel
    const wrapper = document.createElement('div');
    wrapper.classList.add('fc-expand-wrapper');
    wrapper.setAttribute('data-date', date);

    info.el.style.position = 'relative';

    info.el.appendChild(wrapper);
    info.el.classList.add('cursor-pointer');

    info.el.addEventListener('click', () => {
      expandedDate.value = expandedDate.value === date ? null : date;
    });
  };

  const calendarOptions: CalendarOptions = {
    initialView: 'dayGridMonth',
    plugins: [dayGridPlugin],
    headerToolbar: false,
    dayHeaderFormat: { weekday: 'long' },
    dayCellDidMount,
    contentHeight: 'auto',
    dayMaxEventRows: true,
    expandRows: false,
  };

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

  const closeExpandedPanel = (e: MouseEvent) => {
    // Determine whether the expanded panel is going to be closed.
    // Expanded panel should only close when the click is from a date
    // without an activitiy (e.g. Weekend, Non-sitting, Sitting).
    const target = e.target as HTMLElement;
    if (!target) {
      expandedDate.value = null;
      return;
    }

    // Find the nearest Calendar cell
    const dayGridCell = target.closest('.fc-daygrid-day');
    if (!dayGridCell) {
      expandedDate.value = null;
      return;
    }

    // Traverse down and retrieve the element that has a data-formatted-date attr
    const dateEl = dayGridCell.querySelector(
      '[data-formatted-date]'
    ) as HTMLElement | null;
    if (!dateEl || !dateEl.dataset.formattedDate) {
      expandedDate.value = null;
      return;
    }

    // If the date is in the calendarEventsWithActivities,
    // then the click happened on a cell that has an expanded panel.
    // If not found, we can safely close the panel.
    const date = dateEl.dataset.formattedDate;
    const hasActivity = calendarEventsWithActivities.value.find(
      (e) => e.date === date
    );
    if (!hasActivity) {
      expandedDate.value = null;
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
