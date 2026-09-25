import { useCommonStore } from '@/stores';
import { ApplicationInfo, UserInfo } from '@/types/common';
import { CourtListCardInfo } from '@/types/courtlist';
import { mount } from '@vue/test-utils';
import CourtListCard from 'CMP/courtlist/CourtListCard.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

const mockRouterResolve = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({ resolve: mockRouterResolve }),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  mockRouterResolve.mockReset();
  mockRouterResolve.mockReturnValue({ href: '/transitory-documents' });
  window.open = vi.fn();
});

const setUserPermissions = (permissions: string[] = []) => {
  const commonStore = useCommonStore();
  commonStore.setUserInfo({
    userType: '',
    enableArchive: false,
    roles: [],
    permissions: permissions,
    subRole: '',
    isSupremeUser: '',
    isPendingRegistration: false,
    isActive: true,
    agencyCode: '',
    userId: '',
    judgeId: 0,
    judgeHomeLocationId: 0,
    email: '',
    userTitle: '',
  } as unknown as UserInfo);
  return commonStore;
};

const createWrapper = (
  permissions: string[] = [],
  options: {
    useCourtLocations?: boolean;
    openLocationInfo?: (opts: {
      locationId?: string;
      locationName?: string;
    }) => void;
  } = {}
) => {
  setUserPermissions(permissions);
  if (options.useCourtLocations !== undefined) {
    const commonStore = useCommonStore();
    commonStore.appInfo = {
      useCourtLocations: options.useCourtLocations,
    } as ApplicationInfo;
  }
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
      provide: {
        openLocationInfo: options.openLocationInfo,
      },
    },
  });
};

describe('CourtListCard.vue', () => {
  it('renders all court list details correctly', () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS']);
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
      const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS']);
      const commonStore = useCommonStore();
      commonStore.updateCourtRoomsAndLocations([
        { locationId: 2, name: 'Court B', infoLink: 'link2' },
        { locationId: id, name: 'Court A', infoLink: 'link' },
      ]);
      await nextTick();

      expect(wrapper.find('a[target="_blank"]').attributes('href')).toBe(
        'link'
      );
    }
  );

  it('shows shared folder button when user has permission', () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS']);
    expect(wrapper.find('[data-test="view-shared-folder-btn"]').exists()).toBe(
      true
    );
  });

  it('hides shared folder button when user lacks permission', () => {
    const wrapper = createWrapper([]);
    expect(wrapper.find('[data-test="view-shared-folder-btn"]').exists()).toBe(
      false
    );
  });

  it('opens the shared folder in a new tab', async () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS']);

    await wrapper.find('[data-test="view-shared-folder-btn"]').trigger('click');

    expect(mockRouterResolve).toHaveBeenCalledWith({
      name: 'TransitoryDocuments',
      params: {
        locationId: '1',
        roomCd: 'Room 101',
        date: '2024-10-11',
      },
      query: { location: 'Court A' },
    });
    expect(window.open).toHaveBeenCalledWith(
      '/transitory-documents',
      '_blank',
      'noopener'
    );
  });

  it('renders the info link with "See more" text when useCourtLocations is disabled', async () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS'], {
      useCourtLocations: false,
    });
    const commonStore = useCommonStore();
    commonStore.updateCourtRoomsAndLocations([
      { locationId: '1', name: 'Court A', infoLink: 'link' },
    ]);
    await nextTick();

    const link = wrapper.find('a[target="_blank"]');
    expect(link.exists()).toBe(true);
    expect(link.attributes('href')).toBe('link');
    expect(link.text()).toContain('See more about this location');
    expect(wrapper.find('button.link-button').exists()).toBe(false);
  });

  it('renders the location info button when useCourtLocations is enabled', () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS'], {
      useCourtLocations: true,
      openLocationInfo: vi.fn(),
    });

    const button = wrapper.find('button.link-button');
    expect(button.exists()).toBe(true);
    expect(button.text()).toContain('See more about this location');
    expect(wrapper.find('a[target="_blank"]').exists()).toBe(false);
  });

  it('calls openLocationInfo with location details when the info button is clicked', async () => {
    const openLocationInfo = vi.fn();
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS'], {
      useCourtLocations: true,
      openLocationInfo,
    });

    await wrapper.find('button.link-button').trigger('click');

    expect(openLocationInfo).toHaveBeenCalledWith({
      locationId: '1',
      locationName: 'Court A',
    });
  });

  it('matches the location by name when the id does not match', async () => {
    const wrapper = createWrapper(['LIST_TRANSITORY_DOCUMENTS'], {
      useCourtLocations: false,
    });
    const commonStore = useCommonStore();
    commonStore.updateCourtRoomsAndLocations([
      { locationId: '99', name: 'Court A', infoLink: 'name-match-link' },
    ]);
    await nextTick();

    expect(wrapper.find('a[target="_blank"]').attributes('href')).toBe(
      'name-match-link'
    );
  });
});
