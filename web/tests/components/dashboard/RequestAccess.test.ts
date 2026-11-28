import RequestAccess from '@/components/dashboard/RequestAccess.vue';
import { SESSION_STATES } from '@/constants/sessionStates';
import { useCommonStore } from '@/stores/CommonStore';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { CustomAPIError } from '@/types/ApiResponse';
import type { UserInfo } from '@/types/common';
import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import { createPinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createMemoryHistory, createRouter } from 'vue-router';

describe('RequestAccess', () => {
  const submit = vi.fn();
  const getMyUser = vi.fn();
  const record = {
    userId: 'user-1',
    email: 'test@example.com',
    isActive: false,
    roles: [],
    isPendingRegistration: true,
  } as unknown as UserInfo;

  function open(user: UserInfo | null = record) {
    const pinia = createPinia();
    const store = useCommonStore(pinia);
    store.$patch({ userInfo: user, sessionState: SESSION_STATES.ACCESS_REQUIRED });
    const router = createRouter({ history: createMemoryHistory(), routes: [] });
    const wrapper = mount(RequestAccess, {
      global: {
        plugins: [pinia, router],
        provide: {
          userService: { requestAccess: submit, getMyUser },
          authService: {},
          applicationService: {},
        },
        stubs: {
          VBtn: { template: '<button><slot /></button>' },
        },
      },
    });
    return { wrapper, snackbar: useSnackbarStore(pinia) };
  }

  beforeEach(() => {
    submit.mockReset();
    getMyUser.mockReset();
  });

  it('allows a pending user to submit and shows confirmation only on this visit', async () => {
    let finishSubmission!: (response: { id: string }) => void;
    submit.mockImplementation(() => new Promise((resolve) => {
      finishSubmission = resolve;
    }));
    const { wrapper } = open();
    const button = wrapper.get('button');

    expect(button.attributes('disabled')).toBeUndefined();
    expect(wrapper.text()).not.toContain('Your request has been submitted!');
    expect(getMyUser).not.toHaveBeenCalled();

    await button.trigger('click');
    expect(button.attributes('disabled')).toBeDefined();
    await button.trigger('click');
    expect(submit).toHaveBeenCalledTimes(1);

    finishSubmission({ id: record.userId });
    await flushPromises();
    expect(wrapper.text()).toContain('Your request has been submitted!');
    expect(button.attributes('disabled')).toBeDefined();
    wrapper.unmount();

    const nextVisit = open().wrapper;
    expect(nextVisit.get('button').attributes('disabled')).toBeUndefined();
    expect(nextVisit.text()).not.toContain('Your request has been submitted!');
    nextVisit.unmount();
  });

  it.each([
    { data: ['First error.', 'Second error.'], message: 'First error. Second error.' },
    { data: 'Account error.', message: 'Account error.' },
    { data: { error: ['Validation failed.'] }, message: 'Validation failed.' },
    { data: { errors: { email: ['Email is required.'] } }, message: 'Email is required.' },
    { data: '', message: 'Please try again or contact your JASPER administrator.' },
  ])('shows a visible error and enables submission again ($message)', async ({ data, message }) => {
    const cause = new AxiosError('Failed');
    cause.response = { data } as NonNullable<AxiosError['response']>;
    submit.mockRejectedValueOnce(new CustomAPIError(cause.message, cause));
    const { wrapper, snackbar } = open();

    await wrapper.get('button').trigger('click');
    await flushPromises();
    expect(snackbar.title).toBe('Unable to submit access request');
    expect(snackbar.message).toBe(message);
    expect(snackbar.isVisible).toBe(true);
    expect(snackbar.color).toBe('error');
    expect(wrapper.get('button').attributes('disabled')).toBeUndefined();
    expect(wrapper.text()).not.toContain('Your request has been submitted!');
    wrapper.unmount();
  });

  it.each([
    { user: { ...record, roles: ['Judge'] }, warning: 'Your user has been disabled.' },
    { user: { ...record, isActive: true }, warning: 'Warning, you do not have valid access to JASPER.' },
    { user: null, warning: 'Warning, you do not have valid access to JASPER.' },
    { user: { ...record, email: '' }, warning: 'Warning, you do not have valid access to JASPER.' },
  ])('blocks invalid or disabled accounts and preserves the warning ($warning)', async ({ user, warning }) => {
    const { wrapper } = open(user);

    expect(wrapper.text()).toContain(warning);
    const button = wrapper.get('button');
    expect(button.attributes('disabled')).toBeDefined();
    await button.trigger('click');
    expect(submit).not.toHaveBeenCalled();
    expect(getMyUser).not.toHaveBeenCalled();
    wrapper.unmount();
  });
});
