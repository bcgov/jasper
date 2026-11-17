import { useCommonStore, useCourtFileSearchStore } from '@/stores';
import { PERMISSIONS } from '@/constants/permissions';
import { AssignedCaseResponse, Case } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { CourtClassEnum, UserInfo } from '@/types/common';
import { getEnumName } from '@/utils/utils';
import { flushPromises, mount } from '@vue/test-utils';
import DashboardPanels from 'CMP/dashboard/panels/DashboardPanels.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

const mockData: ApiResponse<AssignedCaseResponse> = {
  succeeded: true,
  payload: {
    dueDateEditingJudgeId: 1,
    reservedJudgments: [
      {
        id: 'case1',
        judgeId: 1,
        appearanceId: 'appearance-1',
        updatedDate: '2026-11-03T12:00:00Z',
        courtClass: 'CC',
        physicalFileId: 'phys-1',
        courtFileNumber: 'CF-12345',
        fileNumber: 'CF-12345',
      } as Case,
      {
        id: 'case2',
        judgeId: 1,
        appearanceId: 'appearance-2',
        updatedDate: '2026-11-03T12:00:00Z',
        courtClass: 'SC',
        physicalFileId: 'phys-2',
        courtFileNumber: 'CF-67890',
        fileNumber: 'CF-67890',
      } as Case,
    ],
    scheduledContinuations: [
      {
        id: 'case3',
        courtClass: 'CC',
        physicalFileId: 'phys-3',
        courtFileNumber: 'CF-11111',
        fileNumber: 'CF-11111',
      } as Case,
      {
        id: 'case4',
        courtClass: 'SC',
        physicalFileId: 'phys-4',
        courtFileNumber: 'CF-22222',
        fileNumber: 'CF-22222',
      } as Case,
    ],
    others: [
      {
        id: 'case5',
        courtClass: 'CC',
        physicalFileId: 'phys-5',
        courtFileNumber: 'CF-33333',
        fileNumber: 'CF-33333',
      } as Case,
    ],
    futureAssigned: [
      {
        id: 'case6',
        courtClass: 'SC',
        physicalFileId: 'phys-6',
        courtFileNumber: 'CF-44444',
        fileNumber: 'CF-44444',
      } as Case,
    ],
  },
  errors: [],
};

const createCaseService = () => ({
  getAssignedCases: vi.fn().mockImplementation(async () => structuredClone(mockData)),
  setDueDateOverride: vi.fn(),
});

describe('DashboardPanels.vue', () => {
  let caseService: any;
  let windowOpenSpy: any;

  beforeEach(() => {
    setActivePinia(createPinia());
    caseService = createCaseService();
    windowOpenSpy = vi.spyOn(globalThis, 'open').mockImplementation(() => null);
  });

  afterEach(() => {
    windowOpenSpy.mockRestore();
  });

  function mountComponent(judgeId?: number) {
    const wrapper = mount(DashboardPanels, {
      props: { judgeId },
      global: {
        plugins: [createPinia()],
        provide: {
          caseService,
        },
      },
    });
    useCommonStore().loggedInUserInfo = {
      judgeId: 1,
      isActive: true,
      roles: ['Judge'],
      permissions: [PERMISSIONS.EDIT_RESERVED_JUDGMENTS_DUE_DATE],
    } as UserInfo;
    return wrapper;
  }

  it('uses the signed-in judge when the selected schedule belongs to another judge', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const common = useCommonStore();
    common.loggedInUserInfo = {
      judgeId: 1, isActive: true, roles: [], permissions: [PERMISSIONS.EDIT_RESERVED_JUDGMENTS_DUE_DATE],
    } as unknown as UserInfo;
    common.userInfo = { judgeId: 2, roles: ['Judge'] } as UserInfo;
    const vm = wrapper.vm as any;
    const row = {
      judgeId: 1,
      physicalFileId: 'file',
      appearanceId: 'appearance',
      updatedDate: '2026-11-03T12:00:00Z',
    };
    expect(vm.canEditDueDate(row)).toBe(true);
    expect(vm.canEditDueDate({ ...row, judgeId: 2 })).toBe(false);
    common.loggedInUserInfo.roles = ['System Administrator'];
    expect(vm.canEditDueDate(row)).toBe(true);
    common.loggedInUserInfo.permissions = [];
    expect(vm.canEditDueDate(row)).toBe(false);
  });

  it.each([
    { physicalFileId: '' },
    { physicalFileId: ' ' },
    { physicalFileId: null },
    { appearanceId: '' },
    { appearanceId: ' ' },
    { appearanceId: undefined },
    { reason: 'CNT', restrictionCode: 'S' },
    { reason: 'DEC', restrictionCode: 'G' },
    { reason: 'DEC', restrictionCode: undefined },
    { updatedDate: undefined },
  ])(
    'does not offer editing for a known ineligible record: %j',
    async (fields) => {
      const wrapper = mountComponent();
      await flushPromises();
      useCommonStore().loggedInUserInfo = {
        judgeId: 1,
        isActive: true,
        roles: ['Judge'],
        permissions: [PERMISSIONS.EDIT_RESERVED_JUDGMENTS_DUE_DATE],
      } as UserInfo;
      expect(
        (wrapper.vm as any).canEditDueDate({
          judgeId: 1,
          physicalFileId: 'file',
          appearanceId: 'appearance',
          updatedDate: '2026-11-03T12:00:00Z',
          ...fields,
        })
      ).toBe(false);
    }
  );

  it.each([
    { reason: undefined },
    { reason: null },
    { reason: ' ' },
    { reason: 'DEC', restrictionCode: 'S' },
    { reason: 'dec', restrictionCode: 's' },
  ])(
    'allows owned reserved judgments and seized decisions: %j',
    async (fields) => {
      const wrapper = mountComponent();
      await flushPromises();
      useCommonStore().loggedInUserInfo = {
        judgeId: 1,
        isActive: true,
        roles: ['Judge'],
        permissions: [PERMISSIONS.EDIT_RESERVED_JUDGMENTS_DUE_DATE],
      } as UserInfo;
      expect(
        (wrapper.vm as any).canEditDueDate({
          judgeId: 1,
          physicalFileId: 'file',
          appearanceId: 'appearance',
          updatedDate: '2026-11-03T12:00:00Z',
          ...fields,
        })
      ).toBe(true);
    }
  );

  it('updates every displayed duplicate from the saved API response', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const vm = wrapper.vm as any;
    caseService.setDueDateOverride.mockResolvedValue({
      succeeded: true,
      errors: [],
      payload: vm.reservedJudgements.map((row: Case) => ({
        ...row,
        overrideDueDate: '2026-12-01',
        updatedDate: '2026-11-03T12:00:00Z',
      })),
    });
    await vm.saveDueDate(vm.reservedJudgements[0], '2026-12-01');
    expect(
      vm.reservedJudgements.every(
        (row: Case) => row.overrideDueDate === '2026-12-01'
      )
    ).toBe(true);
    expect(
      vm.reservedJudgements.every(
        (row: Case) => row.updatedDate === '2026-11-03T12:00:00Z'
      )
    ).toBe(true);
    // The API omits null properties; a cleared override must still replace the old value.
    caseService.setDueDateOverride.mockResolvedValue({
      succeeded: true,
      errors: [],
      payload: vm.reservedJudgements.map((row: Case) => ({ ...row, overrideDueDate: undefined })),
    });
    await vm.saveDueDate(vm.reservedJudgements[0], null);
    expect(
      vm.reservedJudgements.every((row: Case) => row.overrideDueDate === null)
    ).toBe(true);
  });

  it('does not offer editing without a verified write identity or signed-in permissions', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const vm = wrapper.vm as any;
    const row = vm.reservedJudgements[0];
    expect(vm.canEditDueDate(row)).toBe(true);
    vm.dueDateEditingJudgeId = null;
    expect(vm.canEditDueDate(row)).toBe(false);
    vm.dueDateEditingJudgeId = 1;
    const common = useCommonStore();
    common.userInfo = common.loggedInUserInfo;
    common.loggedInUserInfo = null;
    expect(vm.canEditDueDate(row)).toBe(false);
    await expect(vm.saveDueDate(row, '2026-12-01')).rejects.toThrow('cannot edit');
    expect(caseService.setDueDateOverride).not.toHaveBeenCalled();
  });

  it('prevents overlapping saves for duplicate-key rows and updates only returned rows', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const vm = wrapper.vm as any;
    const row = vm.reservedJudgements[0];
    vm.reservedJudgements.push({ ...row, id: 'duplicate' });
    let complete!: (value: unknown) => void;
    caseService.setDueDateOverride.mockImplementation(() => new Promise(resolve => { complete = resolve; }));
    const saving = vm.saveDueDate(row, '2026-12-01');
    await expect(vm.saveDueDate(vm.reservedJudgements[2], '2026-12-02')).rejects.toThrow('already being saved');
    expect(caseService.setDueDateOverride).toHaveBeenCalledOnce();
    complete({ succeeded: true, errors: [], payload: [row, vm.reservedJudgements[2]].map(value => ({
      ...value, overrideDueDate: '2026-12-01', updatedDate: '2026-11-16T12:00:00Z',
    })) });
    await saving;
    expect(vm.reservedJudgements[0].overrideDueDate).toBe('2026-12-01');
    expect(vm.reservedJudgements[2].updatedDate).toBe('2026-11-16T12:00:00Z');
    expect(vm.reservedJudgements[1].overrideDueDate).toBeUndefined();
  });

  it('ignores a completed save after switching judges even when the new response reuses an ID', async () => {
    const wrapper = mountComponent(1);
    await flushPromises();
    const vm = wrapper.vm as any;
    const row = vm.reservedJudgements[0];
    let complete!: (value: unknown) => void;
    caseService.setDueDateOverride.mockImplementation(() => new Promise(resolve => { complete = resolve; }));
    const saving = vm.saveDueDate(row, '2026-12-01');
    await wrapper.setProps({ judgeId: 2 });
    await flushPromises();
    complete({ succeeded: true, errors: [], payload: [{ ...row, overrideDueDate: '2026-12-01' }] });
    await saving;
    expect(vm.reservedJudgements[0].overrideDueDate).toBeUndefined();
  });

  it('ignores a completed save after unmounting', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const vm = wrapper.vm as any;
    const row = vm.reservedJudgements[0];
    let complete!: (value: unknown) => void;
    caseService.setDueDateOverride.mockImplementation(() => new Promise(resolve => { complete = resolve; }));
    const saving = vm.saveDueDate(row, '2026-12-01');
    wrapper.unmount();
    complete({ succeeded: true, errors: [], payload: [{ ...row, overrideDueDate: '2026-12-01' }] });
    await saving;
    expect(row.overrideDueDate).toBeUndefined();
  });

  it('uses only the newest judge GET response', async () => {
    const wrapper = mountComponent(1);
    await flushPromises();
    let older!: (value: unknown) => void;
    caseService.getAssignedCases.mockImplementationOnce(() => new Promise(resolve => { older = resolve; }));
    await wrapper.setProps({ judgeId: 2 });
    const newest = structuredClone(mockData);
    newest.payload.reservedJudgments[0].styleOfCause = 'Newest judge response';
    caseService.getAssignedCases.mockResolvedValueOnce(newest);
    await wrapper.setProps({ judgeId: 3 });
    await flushPromises();
    older(structuredClone(mockData));
    await flushPromises();
    expect((wrapper.vm as any).reservedJudgements[0].styleOfCause).toBe('Newest judge response');
    expect((wrapper.vm as any).isLoading).toBe(false);
  });

  it('shows saved overrides again after refetch and component remount', async () => {
    const persisted = structuredClone(mockData);
    persisted.payload.reservedJudgments[0].overrideDueDate = '2026-12-01';
    caseService.getAssignedCases.mockResolvedValue(persisted);
    const wrapper = mountComponent(1);
    await flushPromises();
    expect((wrapper.vm as any).reservedJudgements[0].overrideDueDate).toBe('2026-12-01');
    await wrapper.setProps({ judgeId: 2 });
    await wrapper.setProps({ judgeId: 1 });
    await flushPromises();
    expect((wrapper.vm as any).reservedJudgements[0].overrideDueDate).toBe('2026-12-01');
    wrapper.unmount();
    const remounted = mountComponent(1);
    await flushPromises();
    expect((remounted.vm as any).reservedJudgements[0].overrideDueDate).toBe('2026-12-01');
  });

  it('does not mutate a persisted row on failed saves', async () => {
    const wrapper = mountComponent();
    await flushPromises();
    const vm = wrapper.vm as any;
    const row = vm.reservedJudgements[0];
    caseService.setDueDateOverride.mockResolvedValue({ succeeded: false, errors: ['Save rejected.'] });
    await expect(vm.saveDueDate(row, '2026-12-01')).rejects.toThrow('Save rejected.');
    expect(row.overrideDueDate).toBeUndefined();
    expect(row.updatedDate).toBe('2026-11-03T12:00:00Z');
  });

  it('renders v-skeleton-loader on initial load', async () => {
    const wrapper = mountComponent();
    expect(wrapper.find('v-skeleton-loader').exists()).toBe(true);
  });

  it('renders panel title and count', async () => {
    const wrapper = mountComponent();

    (wrapper.vm as any).isLoading = false;
    await nextTick();

    await flushPromises();

    expect(wrapper.text()).toContain('Reserved judgments & decisions');
    expect(wrapper.text()).toContain(
      `(${mockData.payload.reservedJudgments.length})`
    );
    expect(wrapper.text()).toContain('Scheduled continuations');
    expect(wrapper.text()).toContain(
      `(${mockData.payload.scheduledContinuations.length})`
    );
    expect(wrapper.text()).toContain('Other seized cases');
    expect(wrapper.text()).toContain(`(${mockData.payload.others.length})`);
    expect(wrapper.text()).toContain('Future assigned');
    expect(wrapper.text()).toContain(
      `(${mockData.payload.futureAssigned.length})`
    );
  });

  it('renders unique file number counts correctly', async () => {
    const duplicateData: ApiResponse<AssignedCaseResponse> = {
      succeeded: true,
      payload: {
        reservedJudgments: [
          {
            id: 'case1',
            courtClass: 'CC',
            physicalFileId: 'phys-1',
            courtFileNumber: 'CF-12345',
            fileNumber: 'CF-12345',
          } as Case,
          {
            id: 'case2',
            courtClass: 'CC',
            physicalFileId: 'phys-1',
            courtFileNumber: 'CF-12345',
            fileNumber: 'CF-12345', // duplicate
          } as Case,
        ],
        scheduledContinuations: [
          {
            id: 'case4',
            courtClass: 'CC',
            physicalFileId: 'phys-3',
            courtFileNumber: 'CF-11111',
            fileNumber: 'CF-11111',
          } as Case,
          {
            id: 'case5',
            courtClass: 'CC',
            physicalFileId: 'phys-3',
            courtFileNumber: 'CF-11111',
            fileNumber: 'CF-11111', // duplicate
          } as Case,
        ],
        others: [
          {
            id: 'case7',
            courtClass: 'CC',
            physicalFileId: 'phys-5',
            courtFileNumber: 'CF-33333',
            fileNumber: 'CF-33333',
          } as Case,
          {
            id: 'case8',
            courtClass: 'CC',
            physicalFileId: 'phys-5',
            courtFileNumber: 'CF-33333',
            fileNumber: 'CF-33333', // duplicate
          } as Case,
        ],
        futureAssigned: [
          {
            id: 'case9',
            courtClass: 'SC',
            physicalFileId: 'phys-6',
            courtFileNumber: 'CF-44444',
            fileNumber: 'CF-44444',
          } as Case,
          {
            id: 'case10',
            courtClass: 'SC',
            physicalFileId: 'phys-6',
            courtFileNumber: 'CF-44444',
            fileNumber: 'CF-44444', // duplicate
          } as Case,
        ],
      },
      errors: [],
    };

    caseService.getAssignedCases = vi.fn().mockResolvedValue(duplicateData);
    const wrapper = mountComponent();
    await flushPromises();

    const vm = wrapper.vm as any;

    expect(vm.uniqueJudgements).toBe(1);
    expect(vm.uniqueContinuations).toBe(1);
    expect(vm.uniqueOthers).toBe(1);
    expect(vm.uniqueFutureAssigned).toBe(1);
  });

  it('logs error if service fails', async () => {
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    caseService.getAssignedCases = vi.fn().mockRejectedValue(new Error('fail'));
    const wrapper = mountComponent();
    const vm = wrapper.vm as any;
    vm.expanded = 'reserved-judgement';
    await flushPromises();
    expect(errorSpy).toHaveBeenCalledWith(
      'Failed to load RJs or scheduled continuations:',
      expect.any(Error)
    );
    errorSpy.mockRestore();
  });

  describe('viewCaseDetails', () => {
    it('opens criminal file URL for criminal court class', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      const vm = wrapper.vm as any;
      const criminalCase: Case = {
        id: '1',
        courtClass: getEnumName(CourtClassEnum, CourtClassEnum.A),
        physicalFileId: '1',
        courtFileNumber: 'C-1',
      } as Case;

      vm.viewCaseDetails(criminalCase);

      expect(windowOpenSpy).toHaveBeenCalledWith('/criminal-file/1', '_blank');
    });

    it('opens civil file URL for civil court class', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      const vm = wrapper.vm as any;
      const civilCase: Case = {
        id: '2',
        courtClass: getEnumName(CourtClassEnum, CourtClassEnum.F),
        physicalFileId: '2',
        courtFileNumber: 'F-2',
      } as Case;

      vm.viewCaseDetails(civilCase);

      expect(windowOpenSpy).toHaveBeenCalledWith('/civil-file/2', '_blank');
    });

    it('adds file to courtFileSearchStore when viewing case details', async () => {
      const wrapper = mountComponent();
      await flushPromises();

      const store = useCourtFileSearchStore();
      const addFilesForViewingSpy = vi.spyOn(store, 'addFilesForViewing');

      const vm = wrapper.vm as any;
      const testCase: Case = {
        id: '3',
        courtClass: 'CC',
        physicalFileId: 'phys-789',
        courtFileNumber: 'CF-99999',
      } as Case;

      vm.viewCaseDetails(testCase);

      expect(addFilesForViewingSpy).toHaveBeenCalledWith({
        searchCriteria: {},
        searchResults: [],
        files: [
          {
            key: 'phys-789',
            value: 'CF-99999',
          },
        ],
      });
    });
  });
});
