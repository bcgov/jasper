import { useCommonStore } from '@/stores';
import { CourtListCardInfo } from '@/types/courtlist';
import { mount } from '@vue/test-utils';
import CourtListCard from 'CMP/courtlist/CourtListCard.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

beforeEach(() => {
  setActivePinia(createPinia());
});

const createWrapper = () => {
  const card: CourtListCardInfo = {
    courtListLocation: 'Court A',
    courtListLocationID: 1,
    courtListRoom: 'Room 101',
    activity: 'Hearing',
    amPM: 'AM',
    fileCount: 5,
    presider: 'Judge Smith',
    courtClerk: 'John Doe',
    email: 'court@example.com',
    shortHandDate: '',
    totalCases: 0,
    totalTime: '',
    totalTimeUnit: '',
    criminalCases: 0,
    familyCases: 0,
    civilCases: 0,
  };
  return mount(CourtListCard, {
    props: {
      cardInfo: card,
      date: '2024-10-11',
    },
    global: {
      stubs: {
        TransitoryDocumentsDialog: true,
      },
    },
  });
};

describe('CourtListCard.vue', () => {
  it('renders all court list details correctly', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Court A');
    expect(wrapper.text()).toContain('Rooms: Room 101 (AM)');
    expect(wrapper.text()).toContain('Presider: Judge Smith');
    expect(wrapper.text()).toContain('Court clerk: John Doe');
    expect(wrapper.text()).toContain('Activity: Hearing');
    expect(wrapper.text()).toContain('Scheduled: 5 files');
    expect(wrapper.text()).toContain('court@example.com');
  });

  it.each([[2], [null]])(
    'should display the correct infoLink from store',
    async (id) => {
      const wrapper = createWrapper();
      const commonStore = useCommonStore();
      commonStore.updateCourtRoomsAndLocations([
        { locationId: 2, name: 'Court B', infoLink: 'link2' },
        { locationId: id, name: 'Court A', infoLink: 'link' },
      ]);
      await nextTick();

      expect(wrapper.findAll('a')[1].attributes('href')).toBe('link');
    }
  );
});
