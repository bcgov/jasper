import CourtCalendar from '@/components/dashboard/court-calendar/CourtCalendar.vue';
import { CalendarDayActivity } from '@/types';
import { ActivityClassEnum } from '@/types/common';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

// --- mock calendar API (hoisted so the vi.mock factory can reference it) ---

const mockApi = vi.hoisted(() => ({
  removeAllEvents: vi.fn(),
  addEvent: vi.fn(),
  gotoDate: vi.fn(),
  changeView: vi.fn(),
}));

// Mock the whole @fullcalendar/vue3 module so calendarRef.value.getApi() works
vi.mock('@fullcalendar/vue3', async () => {
  const { defineComponent, h } = await import('vue');
  return {
    default: defineComponent({
      name: 'FullCalendar',
      props: ['options'],
      setup(_, { expose }) {
        expose({ getApi: () => mockApi });
        return () => h('div');
      },
    }),
  };
});

// --- helpers ---

const createEvent = (
  overrides: Partial<{
    start: Date;
    extendedProps: Record<string, unknown>;
  }> = {}
) => ({
  start: faker.date.recent(),
  extendedProps: { key: faker.lorem.word() },
  ...overrides,
});

const mountComponent = (
  props: Partial<{
    calendarView: string;
    selectedDate: Date;
    events: { start: Date; extendedProps: Record<string, unknown> }[];
  }> = {}
) =>
  mount(CourtCalendar, {
    props: {
      calendarView: 'dayGridMonth',
      selectedDate: new Date('2026-03-31'),
      events: [],
      ...props,
    },
  });

// --- tests ---

describe('CourtCalendar.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('calendarOptions', () => {
    it('sets initialView from the calendarView prop', () => {
      const wrapper = mountComponent({ calendarView: 'dayGridWeek' });
      const options = wrapper
        .findComponent({ name: 'FullCalendar' })
        .props('options');
      expect(options.initialView).toBe('dayGridWeek');
    });

    it('disables the header toolbar', () => {
      const wrapper = mountComponent();
      const options = wrapper
        .findComponent({ name: 'FullCalendar' })
        .props('options');
      expect(options.headerToolbar).toBe(false);
    });

    it('formats day headers as long weekday names', () => {
      const wrapper = mountComponent();
      const options = wrapper
        .findComponent({ name: 'FullCalendar' })
        .props('options');
      expect(options.dayHeaderFormat).toEqual({ weekday: 'long' });
    });

    it('enables dayMaxEventRows and auto content height', () => {
      const wrapper = mountComponent();
      const options = wrapper
        .findComponent({ name: 'FullCalendar' })
        .props('options');
      expect(options.dayMaxEventRows).toBe(true);
      expect(options.expandRows).toBe(false);
      expect(options.contentHeight).toBe('auto');
    });

    it('defines the dayGridTwoWeek custom view', () => {
      const wrapper = mountComponent();
      const options = wrapper
        .findComponent({ name: 'FullCalendar' })
        .props('options');
      expect(options.views.dayGridTwoWeek).toEqual({
        type: 'dayGrid',
        duration: { weeks: 2 },
      });
    });
  });

  describe('watchEffect — initial mount', () => {
    it('calls removeAllEvents on mount', async () => {
      mountComponent();
      await nextTick();
      expect(mockApi.removeAllEvents).toHaveBeenCalledOnce();
    });

    it('calls addEvent for each event on mount', async () => {
      const events = [createEvent(), createEvent()];
      mountComponent({ events });
      await nextTick();
      expect(mockApi.addEvent).toHaveBeenCalledTimes(2);
      expect(mockApi.addEvent).toHaveBeenCalledWith({ ...events[0] });
      expect(mockApi.addEvent).toHaveBeenCalledWith({ ...events[1] });
    });

    it('does not call addEvent when events is empty', async () => {
      mountComponent({ events: [] });
      await nextTick();
      expect(mockApi.addEvent).not.toHaveBeenCalled();
    });

    it('calls gotoDate with selectedDate on mount', async () => {
      const selectedDate = new Date('2026-03-15');
      mountComponent({ selectedDate });
      await nextTick();
      expect(mockApi.gotoDate).toHaveBeenCalledWith(selectedDate);
    });
  });

  describe('watchEffect — reactive updates', () => {
    it('reloads events when the events prop changes', async () => {
      const wrapper = mountComponent({ events: [createEvent()] });
      await nextTick();

      const newEvents = [createEvent(), createEvent(), createEvent()];
      await wrapper.setProps({ events: newEvents });
      await nextTick();

      // Second watchEffect run: removeAllEvents called twice total
      expect(mockApi.removeAllEvents).toHaveBeenCalledTimes(2);
      expect(mockApi.addEvent).toHaveBeenCalledTimes(4); // 1 + 3
    });

    it('navigates to the new date when selectedDate prop changes', async () => {
      const wrapper = mountComponent({ selectedDate: new Date('2026-01-01') });
      await nextTick();

      const newDate = new Date('2026-06-15');
      await wrapper.setProps({ selectedDate: newDate });
      await nextTick();

      expect(mockApi.gotoDate).toHaveBeenLastCalledWith(newDate);
      expect(mockApi.gotoDate).toHaveBeenCalledTimes(2);
    });
  });

  describe('changeView (exposed method)', () => {
    it('calls calendarApi.changeView with the given view string', async () => {
      const wrapper = mountComponent();
      await nextTick();

      (wrapper.vm as any).changeView('dayGridTwoWeek');
      expect(mockApi.changeView).toHaveBeenCalledWith('dayGridTwoWeek');
    });

    it('does not throw when calendarRef is not yet set', () => {
      // Before nextTick the ref binding may not have resolved
      const wrapper = mountComponent();
      expect(() => (wrapper.vm as any).changeView('dayGridWeek')).not.toThrow();
    });
  });

  describe('eventContent slot', () => {
    it('forwards the eventContent slot to FullCalendar', () => {
      const wrapper = mount(CourtCalendar, {
        props: {
          calendarView: 'dayGridMonth',
          selectedDate: new Date(),
          events: [],
        },
        slots: {
          eventContent: '<span data-testid="slot-content">event</span>',
        },
      });

      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });
      await nextTick();
      await nextTick();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (filtersComponent.exists()) {
        await filtersComponent.vm.$emit('update:selectedPresiders', [
          judgeId.toString(),
        ]);
        // selectedActivityClass defaults to 'all'
        await nextTick();
      }

      const events = getCalendarEvents(wrapper);
      expect(events.length).toBeGreaterThan(0);
      events.forEach((event: any) => {
        const activityClasses: string[] = event.extendedProps.activities.map(
          (a: CalendarDayActivity) => a.activityClassCode
        );
        expect(activityClasses).toContain('SIT');
        expect(activityClasses).toContain('NS');
      });
    });

    it('does not trigger a new API call when activity class filter changes (client-side filtering)', async () => {
      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });
      await nextTick();
      await nextTick();

      dashboardService.getCourtCalendar.mockClear();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (filtersComponent.exists()) {
        await filtersComponent.vm.$emit('update:selectedActivityClass', 'SIT');
        await nextTick();
      }

      expect(dashboardService.getCourtCalendar).not.toHaveBeenCalled();
    });

    it('when selectedActivityClass is NonSitting, only NS activities are shown and SIT activities are excluded', async () => {
      const judgeId = 101;

      dashboardService.getCourtCalendar = vi.fn().mockResolvedValue({
        payload: {
          days: [
            createMockCalendarDay([
              createMockActivity({
                judgeId,
                activityClassCode: ActivityClassEnum.Sitting,
              }),
              createMockActivity({
                judgeId,
                activityClassCode: ActivityClassEnum.NonSitting,
              }),
            ]),
          ],
          presiders: [],
          activities: [],
        },
      });

      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });
      await nextTick();
      await nextTick();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (filtersComponent.exists()) {
        await filtersComponent.vm.$emit(
          'update:selectedActivityClass',
          ActivityClassEnum.NonSitting
        );
        await nextTick();
      }

      const events = getCalendarEvents(wrapper);
      expect(events.length).toBeGreaterThan(0);
      events.forEach((event: any) => {
        const activityClasses: string[] = event.extendedProps.activities.map(
          (a: CalendarDayActivity) => a.activityClassCode
        );
        expect(activityClasses).toContain(ActivityClassEnum.NonSitting);
        expect(activityClasses).not.toContain(ActivityClassEnum.Sitting);
      });
    });

    it('when selectedActivityClass is Sitting, all non-NS activities are shown including activities with other codes', async () => {
      const judgeId = 101;

      dashboardService.getCourtCalendar = vi.fn().mockResolvedValue({
        payload: {
          days: [
            createMockCalendarDay([
              createMockActivity({
                judgeId,
                activityClassCode: ActivityClassEnum.Sitting,
              }),
              createMockActivity({
                judgeId,
                activityClassCode: ActivityClassEnum.NonSitting,
              }),
              createMockActivity({ judgeId, activityClassCode: 'OTHER' }),
            ]),
          ],
          presiders: [],
          activities: [],
        },
      });

      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });
      await nextTick();
      await nextTick();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (filtersComponent.exists()) {
        await filtersComponent.vm.$emit(
          'update:selectedActivityClass',
          ActivityClassEnum.Sitting
        );
        await nextTick();
      }

      const events = getCalendarEvents(wrapper);
      expect(events.length).toBeGreaterThan(0);
      events.forEach((event: any) => {
        const activityClasses: string[] = event.extendedProps.activities.map(
          (a: CalendarDayActivity) => a.activityClassCode
        );
        expect(activityClasses).toContain(ActivityClassEnum.Sitting);
        expect(activityClasses).toContain('OTHER');
        expect(activityClasses).not.toContain(ActivityClassEnum.NonSitting);
      });
    });

    it('restores all activities when presider selection is cleared', async () => {
      const judgeId1 = 101;

      dashboardService.getCourtCalendar = vi.fn().mockResolvedValue({
        payload: {
          days: [
            createMockCalendarDay([createMockActivity({ judgeId: judgeId1 })]),
          ],
          presiders: [],
          activities: [],
        },
      });

      const wrapper = mountComponent();
      expect(
        wrapper.findComponent({ name: 'FullCalendar' }).classes()
      ).toContain('mx-2');
    });
  });
});
