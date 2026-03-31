import { useCommonStore } from '@/stores';
import { ApplicationConfigurationKey } from '@/stores/CommonStore';
import { ApplicationInfo, UserInfo } from '@/types/common';
import { flushPromises, mount } from '@vue/test-utils';
import ReleaseNotesMenuItem from 'CMP/shared/ReleaseNotesMenuItem.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

type MockUserService = {
  getMyUser: ReturnType<typeof vi.fn>;
  markReleaseNotesViewed: ReturnType<typeof vi.fn>;
};

const makeAppInfo = (overrides: Partial<ApplicationInfo> = {}) => ({
  version: '1.0.0',
  nutrientFeLicenseKey: '',
  environment: 'test',
  configuration: [
    {
      id: 'config-1',
      key: ApplicationConfigurationKey.ReleaseNotesUrl,
      values: ['https://example.com/release-notes'],
    },
  ],
  ...overrides,
});

const makeUserInfo = (overrides: Partial<UserInfo> = {}): UserInfo => ({
  userType: 'Judge',
  enableArchive: false,
  roles: [],
  subRole: '',
  isSupremeUser: 'false',
  isActive: true,
  agencyCode: 'ABC',
  userId: 'user-1',
  judgeId: 1,
  judgeHomeLocationId: 1,
  email: 'test@example.com',
  userTitle: 'Judge Example',
  ...overrides,
});

describe('ReleaseNotesMenuItem.vue', () => {
  let mockUserService: MockUserService;
  let openSpy: ReturnType<typeof vi.fn>;
  let pinia;

  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);
    mockUserService = {
      getMyUser: vi.fn(),
      markReleaseNotesViewed: vi.fn(),
    };
    openSpy = vi.fn();
    vi.stubGlobal('open', openSpy);
  });

  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
  });

  const createWrapper = (props = {}, provide: Record<string, unknown> = {}) => {
    return mount(ReleaseNotesMenuItem, {
      props,
      global: {
        plugins: [pinia],
        provide,
      },
    });
  };

  it('does not render when releaseNotesUrl is empty', () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo({ configuration: [] });

    const wrapper = createWrapper();

    expect(wrapper.text()).not.toContain("What's new in JASPER?");
    expect(wrapper.find('v-list-item').exists()).toBe(false);
  });

  it('renders without emphasis when release notes are up to date', async () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo({ version: '2.0.0' });
    commonStore.userInfo = makeUserInfo({
      releaseNotes: { lastViewedVersion: '2.0.0' },
    });

    const wrapper = createWrapper();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("What's new in JASPER?");
    expect(wrapper.find('.release-notes-emphasis').exists()).toBe(false);
    expect(wrapper.find('.release-notes-icon-emphasis').exists()).toBe(false);
  });

  it('renders emphasis when release notes are unviewed', async () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo({ version: '2.0.0' });
    commonStore.userInfo = makeUserInfo({
      releaseNotes: { lastViewedVersion: '1.9.0' },
    });

    const wrapper = createWrapper();
    await wrapper.vm.$nextTick();

    expect(wrapper.find('.release-notes-emphasis').exists()).toBe(true);
  });

  it('opens release notes and updates user info after mark succeeds', async () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo({ version: '3.1.0' });
    commonStore.userInfo = makeUserInfo({
      releaseNotes: { lastViewedVersion: '3.0.0' },
    });

    const refreshedUser = makeUserInfo({
      releaseNotes: { lastViewedVersion: '3.1.0' },
    });

    mockUserService.markReleaseNotesViewed.mockResolvedValue(undefined);
    mockUserService.getMyUser.mockResolvedValue(refreshedUser);

    const wrapper = createWrapper(
      {},
      {
        userService: mockUserService,
      }
    );

    await wrapper.find('v-list-item').trigger('click');
    await flushPromises();

    expect(openSpy).toHaveBeenCalledWith(
      'https://example.com/release-notes',
      '_blank',
      'noopener,noreferrer'
    );
    expect(mockUserService.markReleaseNotesViewed).toHaveBeenCalledWith(
      '3.1.0'
    );
    expect(mockUserService.getMyUser).toHaveBeenCalled();
    expect(commonStore.userInfo?.releaseNotes?.lastViewedVersion).toBe('3.1.0');
  });

  it('does not mark viewed when app version is missing', async () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo({ version: '' });
    commonStore.userInfo = makeUserInfo();

    const wrapper = createWrapper(
      {},
      {
        userService: mockUserService,
      }
    );

    await wrapper.find('v-list-item').trigger('click');
    await flushPromises();

    expect(openSpy).toHaveBeenCalled();
    expect(mockUserService.markReleaseNotesViewed).not.toHaveBeenCalled();
  });

  it('logs when markReleaseNotesViewed fails', async () => {
    const commonStore = useCommonStore();
    commonStore.appInfo = makeAppInfo();
    commonStore.userInfo = makeUserInfo();

    const errorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined);

    mockUserService.markReleaseNotesViewed.mockRejectedValue(new Error('fail'));

    const wrapper = createWrapper(
      {},
      {
        userService: mockUserService,
      }
    );

    await wrapper.find('v-list-item').trigger('click');
    await flushPromises();

    expect(errorSpy).toHaveBeenCalled();
    expect(mockUserService.getMyUser).not.toHaveBeenCalled();

    errorSpy.mockRestore();
  });
});
