import { CaseService } from '@/services/CaseService';
import { HttpService } from '@/services/HttpService';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { Case, CustomAxiosConfig } from '@/types';
import { CustomAPIError } from '@/types/ApiResponse';
import { mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import CaseDataTable from 'CMP/dashboard/panels/CaseDataTable.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('HttpService response interceptor', () => {
  const conflictMessage =
    'This case has changed. Refresh before editing its due date.';
  const row = { id: 'case', updatedDate: '2026-11-03T12:00:00Z' } as Case;
  let replace: ReturnType<typeof vi.fn>;
  let notify: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    setActivePinia(createPinia());
    replace = vi.fn();
    vi.stubGlobal('location', { href: 'https://jasper.example/', replace });
    vi.spyOn(console, 'error').mockImplementation(() => {});
    notify = vi
      .spyOn(useSnackbarStore(), 'showSnackbar')
      .mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  // Replace only the network adapter: request merging and both real interceptors still run.
  const httpReturning = (status: number) => {
    const http = new HttpService('/');
    http.client.defaults.adapter = async (config) => {
      const response = {
        config,
        status,
        statusText: 'Test response',
        headers: {},
        data:
          status === 200
            ? { succeeded: true, payload: [row], errors: [] }
            : { errors: [conflictMessage] },
      };
      if (status !== 200) {
        throw new AxiosError(
          'Request failed',
          AxiosError.ERR_BAD_RESPONSE,
          config,
          undefined,
          response
        );
      }
      return response;
    };
    return http;
  };

  it.each([400, 403, 409, 500])(
    'keeps due date HTTP %i errors local and rejects the response',
    async (status) => {
      const service = new CaseService(httpReturning(status));
      await expect(
        service.setDueDateOverride(row, '2026-12-01')
      ).rejects.toMatchObject({
        originalError: {
          response: { status, data: { errors: [conflictMessage] } },
        },
      });
      expect(replace).not.toHaveBeenCalled();
      expect(notify).not.toHaveBeenCalled();
    }
  );

  it('delivers a real 409 to the table and retains the unsaved date', async () => {
    const service = new CaseService(httpReturning(409));
    const wrapper = mount(CaseDataTable, {
      props: {
        data: [row],
        viewCaseDetails: vi.fn(),
        canEditDueDate: () => true,
        saveDueDate: async (item, date) => {
          await service.setDueDateOverride(item, date);
        },
      },
    });
    const vm = wrapper.vm as unknown as {
      drafts: Record<string, string>;
      errors: Record<string, string>;
      saveOverride: (item: Case) => Promise<void>;
    };
    vm.drafts[row.id] = '2026-12-01';
    await vm.saveOverride(row);
    expect(vm.errors[row.id]).toBe(conflictMessage);
    expect(vm.drafts[row.id]).toBe('2026-12-01');
    expect(replace).not.toHaveBeenCalled();
    expect(notify).not.toHaveBeenCalled();
  });

  it('retains logout behaviour for an ordinary 409', async () => {
    await expect(httpReturning(409).get('api/other')).rejects.toBeInstanceOf(
      CustomAPIError
    );
    expect(replace).toHaveBeenCalledWith(
      `${import.meta.env.BASE_URL}api/auth/logout?redirectUri=/`
    );
    expect(notify).not.toHaveBeenCalled();
  });

  it('retains authentication redirects for a due date 401', async () => {
    await expect(
      new CaseService(httpReturning(401)).setDueDateOverride(row, null)
    ).rejects.toBeInstanceOf(CustomAPIError);
    expect(replace).toHaveBeenCalledWith(
      expect.stringContaining('api/auth/login?redirectUri=')
    );
  });

  it('uses the typed due-date request options through the real client', async () => {
    const config: CustomAxiosConfig = {
      suppressConflictLogout: true,
      suppressErrorNotification: true,
    };
    await expect(
      httpReturning(409).put(
        'api/cases/case/due-date-override',
        {},
        {},
        'json',
        config
      )
    ).rejects.toBeInstanceOf(CustomAPIError);
    expect(replace).not.toHaveBeenCalled();
  });

  it('returns ordinary success responses', async () => {
    await expect(
      new CaseService(httpReturning(200)).setDueDateOverride(row, null)
    ).resolves.toEqual({ succeeded: true, payload: [row], errors: [] });
    expect(replace).not.toHaveBeenCalled();
    expect(notify).not.toHaveBeenCalled();
  });

  it('retains generic notifications for ordinary server failures', async () => {
    await expect(httpReturning(500).get('api/other')).rejects.toBeInstanceOf(
      CustomAPIError
    );
    expect(notify).toHaveBeenCalledOnce();
    expect(replace).not.toHaveBeenCalled();
  });

  it('notification suppression alone does not bypass conflict logout', async () => {
    await expect(
      httpReturning(409).get(
        'api/other',
        {},
        { suppressErrorNotification: true }
      )
    ).rejects.toBeInstanceOf(CustomAPIError);
    expect(replace).toHaveBeenCalledWith(
      `${import.meta.env.BASE_URL}api/auth/logout?redirectUri=/`
    );
    expect(notify).not.toHaveBeenCalled();
  });

  it('retains ordinary forbidden handling without a redirect or notification', async () => {
    await expect(httpReturning(403).get('api/other')).rejects.toBeInstanceOf(
      CustomAPIError
    );
    expect(replace).not.toHaveBeenCalled();
    expect(notify).not.toHaveBeenCalled();
  });
});
