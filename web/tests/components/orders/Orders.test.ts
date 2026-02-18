import type { Order } from '@/types';
import { OrderReviewStatus, RolesEnum, UserInfo } from '@/types/common';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import Orders from 'CMP/orders/Orders.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mockUserInfo: UserInfo = {
  userType: faker.helpers.arrayElement(['Judge', 'Staff', 'Admin']),
  enableArchive: faker.datatype.boolean(),
  roles: [RolesEnum.Admin],
  subRole: faker.person.jobTitle(),
  isSupremeUser: faker.helpers.arrayElement(['true', 'false']),
  isActive: true,
  agencyCode: faker.string.alpha({ length: 5 }).toUpperCase(),
  userId: faker.string.uuid(),
  judgeId: faker.number.int({ min: 1, max: 10000 }),
  judgeHomeLocationId: faker.number.int({ min: 1, max: 100 }),
  email: faker.internet.email(),
  userTitle: `Judge ${faker.person.fullName()}`,
};

// Mock the stores
vi.mock('@/stores', () => ({
  useOrdersStore: vi.fn(),
  useCourtFileSearchStore: vi.fn(),
  useCommonStore: () => ({
    userInfo: mockUserInfo,
  }),
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

// Mock orderService
const mockOrderService = {
  getOrders: vi.fn(),
};

// Helper function to generate test orders
const generateOrder = (
  status: OrderReviewStatus,
  courtClass: 'Criminal' | 'Civil' = 'Criminal'
): Order => ({
  id: faker.string.uuid(),
  packageId: faker.number.int({ min: 10000, max: 99999 }),
  packageDocumentId: faker.string.uuid(),
  packageName: faker.lorem.word(),
  receivedDate: faker.date.past().toISOString().split('T')[0],
  processedDate:
    status === OrderReviewStatus.Approved
      ? faker.date.recent().toISOString().split('T')[0]
      : '',
  courtClass,
  courtFileNumber: `${courtClass === 'Criminal' ? 'CF' : 'CV'}-${faker.number.int({ min: 2020, max: 2026 })}-${faker.string.numeric(3)}`,
  styleOfCause:
    courtClass === 'Criminal'
      ? `R v ${faker.person.lastName()}`
      : `${faker.person.lastName()} v ${faker.person.lastName()}`,
  physicalFileId: `file-${faker.string.alphanumeric(3)}`,
  status,
});

describe('Orders.vue', () => {
  let pinia: any;
  let mockOrdersStore: any;
  let mockCourtFileSearchStore: any;
  let mockCommonStore: any;
  const mockPendingOrder: Order = generateOrder(
    OrderReviewStatus.Pending,
    'Criminal'
  );
  const mockApprovedOrder: Order = generateOrder(
    OrderReviewStatus.Approved,
    'Civil'
  );

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

  const createWrapper = () => {
    return mount(Orders, {
      global: {
        plugins: [pinia],
        provide: {
          orderService: mockOrderService,
        },
      },
    });
  };

  it('renders skeleton loader when loading', () => {
    mockOrdersStore.isLoading = true;

    const wrapper = createWrapper();

    expect(wrapper.find('v-skeleton-loader').exists()).toBe(true);
    expect(wrapper.find('.my-4').exists()).toBe(false);
  });

  it('renders expansion panels when not loading', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('v-skeleton-loader').exists()).toBe(false);
    expect(wrapper.find('.my-4').exists()).toBe(true);
    expect(wrapper.findAll('v-expansion-panel')).toHaveLength(2);
  });

  it('displays correct count of pending orders in title', () => {
    const wrapper = createWrapper();

    const title = wrapper.find('h5').text();
    expect(title).toContain('For signing');
    expect(title).toContain('(1)');
  });

  it('does not show count when there are no pending orders', () => {
    mockOrdersStore.orders = [mockApprovedOrder];

    const wrapper = createWrapper();

    const title = wrapper.find('h5').text();
    expect(title).toBe('For signing');
    expect(title).not.toContain('(');
  });

  it('filters pending orders correctly', () => {
    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(1);
    expect(vm.pendingOrders[0].id).toBe(mockPendingOrder.id);
    expect(vm.pendingOrders[0].status).toBe(OrderReviewStatus.Pending);
  });

  it('filters completed orders correctly', () => {
    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.completedOrders).toHaveLength(1);
    expect(vm.completedOrders[0].id).toBe(mockApprovedOrder.id);
    expect(vm.completedOrders[0].status).toBe(OrderReviewStatus.Approved);
  });

  it('handles empty orders array', () => {
    mockOrdersStore.orders = [];

    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(0);
    expect(vm.completedOrders).toHaveLength(0);
  });

  it('handles undefined orders array', () => {
    mockOrdersStore.orders = undefined;

    const wrapper = createWrapper();

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
        .spyOn(globalThis, 'open')
        .mockImplementation(() => null);

      const wrapper = createWrapper();

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
        `/criminal-file/${mockPendingOrder.physicalFileId}`,
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
        .spyOn(globalThis, 'open')
        .mockImplementation(() => null);

      const wrapper = createWrapper();

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
        `/civil-file/${mockApprovedOrder.physicalFileId}`,
        '_blank'
      );

      windowOpenSpy.mockRestore();
    });
  });
});
