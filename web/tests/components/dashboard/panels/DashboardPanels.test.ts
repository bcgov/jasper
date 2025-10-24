import { AssignedCaseResponse, Case } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { flushPromises, mount } from '@vue/test-utils';
import DashboardPanels from 'CMP/dashboard/panels/DashboardPanels.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockData: ApiResponse<AssignedCaseResponse> = {
  succeeded: true,
  payload: {
    reservedJudgments: [
      {
        id: 'case1',
      } as Case,
      {
        id: 'case2',
      } as Case,
    ],
    scheduledContinuations: [],
  },
  errors: [],
};

const createCaseService = () => ({
  getAssignedCases: vi.fn().mockResolvedValue(mockData),
});

describe('DashboardPanels.vue', () => {
  let caseService: any;

  beforeEach(() => {
    caseService = createCaseService();
  });

  function mountComponent() {
    return mount(DashboardPanels, {
      global: {
        provide: {
          caseService,
        },
      },
    });
  }

  it('renders panel title and count', async () => {
    const wrapper = mountComponent();
    expect(wrapper.text()).toContain('Reserved judgments & decisions');
    expect(wrapper.text()).not.toContain('(2)');
  });

  it('loads judgements when panel is expanded', async () => {
    const wrapper = mountComponent();
    const vm = wrapper.vm as any;
    await vm.$nextTick();
    vm.expanded = 'reserved-judgement';
    await flushPromises();
    expect(caseService.getAssignedCases).toHaveBeenCalled();
    expect(wrapper.text()).toContain('2');
    expect(wrapper.text()).toContain('(2)');
  });

  it('clears judgements when panel is collapsed', async () => {
    const wrapper = mountComponent();
    const vm = wrapper.vm as any;
    vm.expanded = 'reserved-judgement';
    await flushPromises();
    expect(wrapper.text()).toContain('2');
    vm.expanded = '';
    await flushPromises();
    expect(
      wrapper.findComponent({ name: 'ReservedJudgementTable' }).props('data')
    ).toEqual([]);
  });

  it('shows skeleton loader while loading', async () => {
    caseService.getAssignedCases = vi.fn(() => new Promise(() => {}));
    const wrapper = mountComponent();
    const vm = wrapper.vm as any;
    vm.expanded = 'reserved-judgement';
    await vm.$nextTick();
    expect(wrapper.find('v-skeleton-loader').exists()).toBe(true);
  });

  it('logs error if service fails', async () => {
    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    caseService.getAssignedCases = vi.fn().mockRejectedValue(new Error('fail'));
    const wrapper = mountComponent();
    const vm = wrapper.vm as any;
    vm.expanded = 'reserved-judgement';
    await flushPromises();
    expect(errorSpy).toHaveBeenCalledWith(
      'Failed to load reserved judgements:',
      expect.any(Error)
    );
    errorSpy.mockRestore();
  });
});
