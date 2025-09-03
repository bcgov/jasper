import RequestAccess from '@/components/dashboard/RequestAccess.vue';
import { useCommonStore } from '@/stores';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { CustomAPIError } from '@/types/ApiResponse';
import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

// Mock UserService
const mockRequestAccess = vi.fn();
const mockGetMyUser = vi.fn();
const mockUserService = {
  requestAccess: mockRequestAccess,
  getMyUser: mockGetMyUser,
};

// Mock stores
vi.mock('@/stores');
vi.mock('@/services');

// Helper to mount component with provide
const mountComponent = async (options = {}) => {
  const wrapper = mount(RequestAccess, {
    global: {
      provide: {
        userService: mockUserService,
      },
    },
    ...options,
  });
  await flushPromises();
  return wrapper;
};

describe('RequestAccess.vue', () => {
  let snackStore: ReturnType<typeof useSnackbarStore>;
  beforeEach(() => {
    setActivePinia(createPinia());
    (useCommonStore as any).mockReturnValue({
      userInfo: { email: 'test@example.com' },
    });
    snackStore = useSnackbarStore();
  });

  it('renders correctly (snapshot)', async () => {
    mockGetMyUser.mockResolvedValue(undefined);
    const wrapper = await mountComponent();

    await vi.waitFor(() => {
      expect(wrapper.html()).toMatchSnapshot();
    });
  });

  it('disables input and button when isSubmitted is true', async () => {
    mockGetMyUser.mockResolvedValue({
      email: 'test@example.com',
      isPendingRegistration: true,
    });

    const wrapper = await mountComponent();

    expect(wrapper.find('v-text-field').attributes('disabled')).toBeDefined();
    expect(wrapper.find('v-btn').attributes('disabled')).toBeDefined();
    expect(wrapper.text()).toContain('Your request has been submitted!');
  });

  it('shows user disabled badge if user is inactive with roles', async () => {
    mockGetMyUser.mockResolvedValue({
      email: 'test@example.com',
      isActive: false,
      roles: [1, 2],
    });

    const wrapper = await mountComponent();
    await nextTick();

    expect(wrapper.text()).toContain('Your user has been disabled.');
  });

  it('shows user invalid badge if user is not pending, not disabled', async () => {
    mockGetMyUser.mockResolvedValue({
      email: 'test@example.com',
      isActive: true,
      isPendingRegistration: false,
      roles: [],
    });

    const wrapper = await mountComponent();
    await nextTick();

    expect(wrapper.text()).toContain(
      'Warning, you do not have valid access to JASPER.'
    );
  });

  it('calls requestAccess and sets isSubmitted on success', async () => {
    mockGetMyUser.mockResolvedValue(undefined);
    mockRequestAccess.mockResolvedValue({ email: 'test@example.com' });

    const wrapper = await mountComponent();
    await nextTick();
    wrapper.find('v-btn').trigger('click');
    await flushPromises();

    expect(mockRequestAccess).toHaveBeenCalledWith('test@example.com');
    expect(wrapper.text()).toContain('Your request has been submitted!');
  });

  it('calls requestAccess and displays errors returned from backend', async () => {
    mockGetMyUser.mockResolvedValue(undefined);
    const axiosError = new AxiosError('test error');
    mockRequestAccess.mockRejectedValue(
      new CustomAPIError(axiosError.message, axiosError)
    );
    snackStore.showSnackbar = vi.fn();

    const wrapper = await mountComponent();
    await nextTick();
    wrapper.find('v-btn').trigger('click');
    await nextTick();

    expect(mockRequestAccess).toHaveBeenCalledWith('test@example.com');
    expect(snackStore.showSnackbar).toHaveBeenCalled();
  });

  it('does not call requestAccess if email is empty', async () => {
    (useCommonStore as any).mockReturnValue({
      userInfo: { email: '' },
    });
    mockGetMyUser.mockResolvedValue(undefined);

    const wrapper = mount(RequestAccess, {
      global: { provide: { userService: mockUserService } },
    });
    await flushPromises();
    wrapper.find('v-btn').trigger('click');
    await nextTick();

    expect(mockRequestAccess).not.toHaveBeenCalled();
  });
});
