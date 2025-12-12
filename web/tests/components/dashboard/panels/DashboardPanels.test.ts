import { useCourtFileSearchStore } from '@/stores';
import { AssignedCaseResponse, Case } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { CourtClassEnum } from '@/types/common';
import { getEnumName } from '@/utils/utils';
import { flushPromises, mount } from '@vue/test-utils';
import DashboardPanels from 'CMP/dashboard/panels/DashboardPanels.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

const mockData: ApiResponse<AssignedCaseResponse> = {
  succeeded: true,
  payload: {
    reservedJudgments: [
      {
        id: 'case1',
        courtClass: 'CC',
        physicalFileId: 'phys-1',
        courtFileNumber: 'CF-12345',
      } as Case,
      {
        id: 'case2',
        courtClass: 'SC',
        physicalFileId: 'phys-2',
        courtFileNumber: 'CF-67890',
      } as Case,
    ],
    scheduledContinuations: [
      {
        id: 'case3',
        courtClass: 'CC',
        physicalFileId: 'phys-3',
        courtFileNumber: 'CF-11111',
      } as Case,
      {
        id: 'case4',
        courtClass: 'SC',
        physicalFileId: 'phys-4',
        courtFileNumber: 'CF-22222',
      } as Case,
    ],
    others: [
      {
        id: 'case5',
        courtClass: 'CC',
        physicalFileId: 'phys-5',
        courtFileNumber: 'CF-33333',
      } as Case,
    ],
    futureAssigned: [
      {
        id: 'case6',
        courtClass: 'SC',
        physicalFileId: 'phys-6',
        courtFileNumber: 'CF-44444',
      } as Case,
    ],
  },
  errors: [],
};

const createCaseService = () => ({
  getAssignedCases: vi.fn().mockResolvedValue(mockData),
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

  function mountComponent() {
    return mount(DashboardPanels, {
      global: {
        plugins: [createPinia()],
        provide: {
          caseService,
        },
      },
    });
  }

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
