import { CustomAPIError } from './../../../src/utils/utils';
import RequestAccess from '@/components/dashboard/RequestAccess.vue';
import { useCommonStore } from '@/stores';
import { getByText } from '@/utils/testutils';
import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { useSnackbarStore } from '@/stores/SnackbarStore';

// Mock UserService
const mockRequestAccess = vi.fn();
const mockGetByEmail = vi.fn();
const mockUserService = {
  requestAccess: mockRequestAccess,
  getByEmail: mockGetByEmail,
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
    mockGetByEmail.mockResolvedValue(undefined);
    const wrapper = await mountComponent();

    expect(wrapper.html()).toMatchSnapshot();
  });

  it('disables input and button when isSubmitted is true', async () => {
    mockGetByEmail.mockResolvedValue({
      email: 'test@example.com',
      isPendingRegistration: true,
    });

    const wrapper = await mountComponent();

    expect(wrapper.find('b-form-input').attributes('disabled')).toBeDefined();
    expect(wrapper.find('b-button').attributes('disabled')).toBeDefined();
    expect(wrapper.text()).toContain('Your request has been submitted!');
  });

  it('shows user disabled badge if user is inactive with roles', async () => {
    mockGetByEmail.mockResolvedValue({
      email: 'test@example.com',
      isActive: false,
      roles: [1, 2],
    });

    const wrapper = await mountComponent();
    await nextTick();

    expect(wrapper.text()).toContain('Your user has been disabled.');
  });

  it('shows user invalid badge if user is not pending, not disabled', async () => {
    mockGetByEmail.mockResolvedValue({
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
    mockGetByEmail.mockResolvedValue(undefined);
    mockRequestAccess.mockResolvedValue({ email: 'test@example.com' });

    const wrapper = await mountComponent();
    await nextTick();
    await wrapper.find('b-button').trigger('click');
    await nextTick();

    expect(mockRequestAccess).toHaveBeenCalledWith('test@example.com');
    expect(wrapper.vm.isSubmitted).toBe(true);
    expect(wrapper.text()).toContain('Your request has been submitted!');
  });

  it('calls requestAccess and displays errors returned from backend', async () => {
    mockGetByEmail.mockResolvedValue(undefined);
    const axiosError = new AxiosError('test error');
    mockRequestAccess.mockRejectedValue(
      new CustomAPIError(axiosError.message, axiosError)
    );
    snackStore.showSnackbar = vi.fn();

    const wrapper = await mountComponent();
    await nextTick();
    await wrapper.find('b-button').trigger('click');
    await nextTick();

    expect(mockRequestAccess).toHaveBeenCalledWith('test@example.com');
    expect(wrapper.vm.isSubmitted).toBe(false);
    expect(snackStore.showSnackbar).toHaveBeenCalled();
  });

  it('does not call requestAccess if email is empty', async () => {
    (useCommonStore as any).mockReturnValue({
      userInfo: { email: '' },
    });
    mockGetByEmail.mockResolvedValue(undefined);

    const wrapper = mount(RequestAccess, {
      global: { provide: { userService: mockUserService } },
    });
    await flushPromises();
    getByText(wrapper, 'Submit Your Request').trigger('click');

    expect(mockRequestAccess).not.toHaveBeenCalled();
  });
});
