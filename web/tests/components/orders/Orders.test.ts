import type { Order } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { mount } from '@vue/test-utils';
import Orders from 'CMP/orders/Orders.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Mock the stores
vi.mock('@/stores', () => ({
  useOrdersStore: vi.fn(),
  useCourtFileSearchStore: vi.fn(),
}));

// Mock the utils
vi.mock('@/utils/utils', () => ({
  getCourtClassLabel: vi.fn((courtClass: string) => courtClass),
  isCourtClassLabelCriminal: vi.fn((label: string) => label === 'Criminal'),
}));

// Mock OrdersDataTable component
vi.mock('CMP/orders/OrdersDataTable.vue', () => ({
  default: {
    name: 'OrdersDataTable',
    template: '<div class="orders-data-table-mock"></div>',
  },
}));

describe('Orders.vue', () => {
  let pinia: any;
  let mockOrdersStore: any;
  let mockCourtFileSearchStore: any;

  const mockPendingOrder: Order = {
    id: '1',
    packageId: 12345,
    packageDocumentId: '340',
    packageName: 'test 1',
    receivedDate: '2026-01-15',
    processedDate: '',
    courtClass: 'Criminal',
    courtFileNumber: 'CF-2026-001',
    styleOfCause: 'R v Smith',
    physicalFileId: 'file-001',
    status: OrderReviewStatus.Pending,
  };

  const mockApprovedOrder: Order = {
    id: '2',
    packageId: 12346,
    packageDocumentId: '341',
    packageName: 'test 2',
    receivedDate: '2026-01-14',
    processedDate: '2026-01-20',
    courtClass: 'Civil',
    courtFileNumber: 'CV-2026-001',
    styleOfCause: 'Jones v Brown',
    physicalFileId: 'file-002',
    status: OrderReviewStatus.Approved,
  };

  beforeEach(async () => {
    pinia = createPinia();
    setActivePinia(pinia);

    mockOrdersStore = {
      isLoading: false,
      orders: [mockPendingOrder, mockApprovedOrder],
    };

    mockCourtFileSearchStore = {
      addFilesForViewing: vi.fn(),
    };

    const { useOrdersStore, useCourtFileSearchStore } = await import(
      '@/stores'
    );
    vi.mocked(useOrdersStore).mockReturnValue(mockOrdersStore);
    vi.mocked(useCourtFileSearchStore).mockReturnValue(
      mockCourtFileSearchStore
    );
  });

  it('renders skeleton loader when loading', () => {
    mockOrdersStore.isLoading = true;

    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    expect(wrapper.find('v-skeleton-loader').exists()).toBe(true);
    expect(wrapper.find('.my-4').exists()).toBe(false);
  });

  it('renders expansion panels when not loading', () => {
    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    expect(wrapper.find('v-skeleton-loader').exists()).toBe(false);
    expect(wrapper.find('.my-4').exists()).toBe(true);
    expect(wrapper.findAll('v-expansion-panel')).toHaveLength(2);
  });

  it('displays correct count of pending orders in title', () => {
    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const title = wrapper.find('h5').text();
    expect(title).toContain('For signing');
    expect(title).toContain('(1)');
  });

  it('does not show count when there are no pending orders', () => {
    mockOrdersStore.orders = [mockApprovedOrder];

    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const title = wrapper.find('h5').text();
    expect(title).toBe('For signing');
    expect(title).not.toContain('(');
  });

  it('filters pending orders correctly', () => {
    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(1);
    expect(vm.pendingOrders[0].id).toBe('1');
    expect(vm.pendingOrders[0].status).toBe(OrderReviewStatus.Pending);
  });

  it('filters completed orders correctly', () => {
    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const vm = wrapper.vm as any;
    expect(vm.completedOrders).toHaveLength(1);
    expect(vm.completedOrders[0].id).toBe('2');
    expect(vm.completedOrders[0].status).toBe(OrderReviewStatus.Approved);
  });

  it('handles empty orders array', () => {
    mockOrdersStore.orders = [];

    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(0);
    expect(vm.completedOrders).toHaveLength(0);
  });

  it('handles undefined orders array', () => {
    mockOrdersStore.orders = undefined;

    const wrapper = mount(Orders, {
      global: {
        plugins: [pinia],
      },
    });

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(0);
    expect(vm.completedOrders).toHaveLength(0);
  });

  describe('viewCaseDetails', () => {
    it('opens criminal file in new window for criminal court class', async () => {
      const { getCourtClassLabel, isCourtClassLabelCriminal } = await import(
        '@/utils/utils'
      );
      vi.mocked(getCourtClassLabel).mockReturnValue('Criminal');
      vi.mocked(isCourtClassLabelCriminal).mockReturnValue(true);

      const windowOpenSpy = vi
        .spyOn(window, 'open')
        .mockImplementation(() => null);

      const wrapper = mount(Orders, {
        global: {
          plugins: [pinia],
        },
      });

      const vm = wrapper.vm as any;
      vm.viewCaseDetails(mockPendingOrder);

      expect(mockCourtFileSearchStore.addFilesForViewing).toHaveBeenCalledWith({
        searchCriteria: {},
        searchResults: [],
        files: [
          {
            key: mockPendingOrder.physicalFileId,
            value: mockPendingOrder.courtFileNumber,
          },
        ],
      });

      expect(windowOpenSpy).toHaveBeenCalledWith(
        '/criminal-file/file-001',
        '_blank'
      );

      windowOpenSpy.mockRestore();
    });

    it('opens civil file in new window for non-criminal court class', async () => {
      const { getCourtClassLabel, isCourtClassLabelCriminal } = await import(
        '@/utils/utils'
      );
      vi.mocked(getCourtClassLabel).mockReturnValue('Civil');
      vi.mocked(isCourtClassLabelCriminal).mockReturnValue(false);

      const windowOpenSpy = vi
        .spyOn(window, 'open')
        .mockImplementation(() => null);

      const wrapper = mount(Orders, {
        global: {
          plugins: [pinia],
        },
      });

      const vm = wrapper.vm as any;
      vm.viewCaseDetails(mockApprovedOrder);

      expect(mockCourtFileSearchStore.addFilesForViewing).toHaveBeenCalledWith({
        searchCriteria: {},
        searchResults: [],
        files: [
          {
            key: mockApprovedOrder.physicalFileId,
            value: mockApprovedOrder.courtFileNumber,
          },
        ],
      });

      expect(windowOpenSpy).toHaveBeenCalledWith(
        '/civil-file/file-002',
        '_blank'
      );

      windowOpenSpy.mockRestore();
    });
  });
});
