import CourtCalendar from '@/components/dashboard/court-calendar/CourtCalendar.vue';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

// --- mock calendar controller (hoisted so the vi.mock factory can reference it) ---

const mockController = vi.hoisted(() => ({
  gotoDate: vi.fn(),
  changeView: vi.fn(),
}));

// Mock @fullcalendar/vue3 so useCalendarController() returns our spies and
// FullCalendar renders a lightweight stub that still exposes the options prop
// and forwards the eventContent slot.
vi.mock('@fullcalendar/vue3', async () => {
  const { defineComponent, h } = await import('vue');
  return {
    default: defineComponent({
      name: 'FullCalendar',
      props: ['options'],
      setup(_, { slots }) {
        return () =>
          h('div', slots.eventContent ? slots.eventContent({ event: {} }) : []);
      },
    }),
    useCalendarController: () => mockController,
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
    selectedDate: Date | undefined;
    events: { start: Date; extendedProps: Record<string, unknown> }[];
    baseCalendarOptions: Record<string, unknown>;
  }> = {}
) =>
  mount(CourtCalendar, {
    props: {
      calendarView: 'dayGridMonth',
      selectedDate: new Date('2026-03-31'),
      events: [],
      baseCalendarOptions: {},
      ...props,
    },
  });

const getOptions = (wrapper: ReturnType<typeof mountComponent>) =>
  wrapper.findComponent({ name: 'FullCalendar' }).props('options') as Record<
    string,
    any
  >;

// --- tests ---

describe('CourtCalendar.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('calendarOptions', () => {
    it('sets initialView from the calendarView prop', () => {
      const wrapper = mountComponent({ calendarView: 'dayGridWeek' });
      expect(getOptions(wrapper).initialView).toBe('dayGridWeek');
    });

    it('sets initialDate from the selectedDate prop', () => {
      const selectedDate = new Date('2026-03-15');
      const wrapper = mountComponent({ selectedDate });
      expect(getOptions(wrapper).initialDate).toBe(selectedDate);
    });

    it('passes the events prop through to the calendar options', () => {
      const events = [createEvent(), createEvent()];
      const wrapper = mountComponent({ events });
      expect(getOptions(wrapper).events).toEqual(events);
    });

    it('merges baseCalendarOptions into the calendar options', () => {
      const wrapper = mountComponent({
        baseCalendarOptions: { editable: true, weekends: false },
      });
      const options = getOptions(wrapper);
      expect(options.editable).toBe(true);
      expect(options.weekends).toBe(false);
    });

    it('wires the calendar controller into the options', () => {
      const wrapper = mountComponent();
      expect(getOptions(wrapper).controller).toBe(mockController);
    });

    it('registers the dayGrid and classic theme plugins', () => {
      const wrapper = mountComponent();
      expect(getOptions(wrapper).plugins).toHaveLength(2);
    });

    it('defines the dayGridTwoWeek custom view', () => {
      const wrapper = mountComponent();
      expect(getOptions(wrapper).views.dayGridTwoWeek).toEqual({
        type: 'dayGrid',
        duration: { weeks: 2 },
      });
    });
  });

  describe('selectedDate watcher', () => {
    it('does not call gotoDate on initial mount', async () => {
      mountComponent({ selectedDate: new Date('2026-03-15') });
      await nextTick();
      expect(mockController.gotoDate).not.toHaveBeenCalled();
    });

    it('navigates to the new date when selectedDate prop changes', async () => {
      const wrapper = mountComponent({ selectedDate: new Date('2026-01-01') });
      await nextTick();

      const newDate = new Date('2026-06-15');
      await wrapper.setProps({ selectedDate: newDate });
      await nextTick();

      expect(mockController.gotoDate).toHaveBeenCalledOnce();
      expect(mockController.gotoDate).toHaveBeenCalledWith(newDate);
    });

    it('does not call gotoDate when selectedDate changes to undefined', async () => {
      const wrapper = mountComponent({ selectedDate: new Date('2026-01-01') });
      await nextTick();

      await wrapper.setProps({ selectedDate: undefined });
      await nextTick();

      expect(mockController.gotoDate).not.toHaveBeenCalled();
    });
  });

  describe('events reactivity', () => {
    it('updates the calendar options when the events prop changes', async () => {
      const wrapper = mountComponent({ events: [createEvent()] });
      await nextTick();

      const newEvents = [createEvent(), createEvent(), createEvent()];
      await wrapper.setProps({ events: newEvents });
      await nextTick();

      expect(getOptions(wrapper).events).toEqual(newEvents);
    });
  });

  describe('changeView (exposed method)', () => {
    it('delegates to the controller changeView with the given view string', async () => {
      const wrapper = mountComponent();
      await nextTick();

      (wrapper.vm as any).changeView('dayGridTwoWeek');
      expect(mockController.changeView).toHaveBeenCalledWith('dayGridTwoWeek');
    });
  });

  describe('eventContent slot', () => {
    it('forwards the eventContent slot to FullCalendar', () => {
      const wrapper = mount(CourtCalendar, {
        props: {
          calendarView: 'dayGridMonth',
          selectedDate: new Date('2026-03-31'),
          events: [],
          baseCalendarOptions: {},
        },
        slots: {
          eventContent: '<span data-testid="slot-content">event</span>',
        },
      });
      expect(wrapper.find('[data-testid="slot-content"]').exists()).toBe(true);
    });
  });

  describe('layout', () => {
    it('applies mx-2 class to the FullCalendar element', () => {
      const wrapper = mountComponent();
      expect(
        wrapper.findComponent({ name: 'FullCalendar' }).classes()
      ).toContain('mx-2');
    });
  });
});
