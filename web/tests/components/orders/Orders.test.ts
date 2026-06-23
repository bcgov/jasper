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

// Mock vue-router's useRoute (Orders.vue derives isDeskOrdersView from route.name)
const mockRoute = { name: 'Orders' };
vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
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
  courtClass: 'Criminal' | 'Civil' = 'Criminal',
  receivedDate?: string
): Order => ({
  id: faker.string.uuid(),
  packageId: faker.number.int({ min: 10000, max: 99999 }),
  packageDocumentId: faker.string.uuid(),
  packageName: faker.lorem.word(),
  receivedDate: receivedDate || faker.date.past().toISOString().split('T')[0],
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
  priorityType: '',
  courtListType: '',
});

describe('Orders.vue', () => {
  let pinia: any;
  let mockOrdersStore: any;
  let mockCourtFileSearchStore: any;
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

    // Reset route to default before each test
    mockRoute.name = 'Orders';

    mockOrdersStore = {
      isLoading: false,
      orders: [mockPendingOrder, mockApprovedOrder],
      initialize: vi.fn(),
    };

    mockCourtFileSearchStore = {
      addFilesForViewing: vi.fn(),
    };

    mockOrderService.getOrders.mockResolvedValue([]);

    const { useOrdersStore, useCourtFileSearchStore } =
      await import('@/stores');
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

  describe('expansion panel titles', () => {
    it('renders both pending and completed expansion panel titles', () => {
      const wrapper = createWrapper();

      const titles = wrapper.findAll('h5');
      expect(titles).toHaveLength(2);
      expect(titles[0].text()).toContain('For signing');
      expect(titles[1].text()).toBe('Completed');
    });

    it('renders "Completed" title on the second expansion panel', () => {
      const wrapper = createWrapper();

      const titles = wrapper.findAll('h5');
      expect(titles[1].text()).toBe('Completed');
    });

    it('"Completed" title does not include a count', () => {
      const wrapper = createWrapper();

      const completedTitle = wrapper.findAll('h5')[1].text();
      expect(completedTitle).toBe('Completed');
      expect(completedTitle).not.toContain('(');
    });

    it('renders "Applications" title when on DeskOrders route', () => {
      mockRoute.name = 'DeskOrders';

      const wrapper = createWrapper();

      const title = wrapper.findAll('h5')[0].text();
      expect(title).toContain('Applications');
      expect(title).not.toContain('For signing');
    });

    it('does not render "For signing" title when on DeskOrders route', () => {
      mockRoute.name = 'DeskOrders';
      mockOrdersStore.orders = [];

      const wrapper = createWrapper();

      const title = wrapper.findAll('h5')[0].text();
      expect(title).toBe('Applications');
    });

    it('displays count next to "Applications" title when there are pending desk orders', () => {
      mockRoute.name = 'DeskOrders';
      // A desk order is identified by courtListTypeDescription === 'Desk Order'
      // (see isDeskOrder in useOrderCounts).
      const deskOrder: Order = {
        ...generateOrder(OrderReviewStatus.Pending, 'Criminal'),
        courtListTypeDescription: 'Desk Order',
      };
      mockOrdersStore.orders = [deskOrder];

      const wrapper = createWrapper();

      const title = wrapper.findAll('h5')[0].text();
      expect(title).toContain('Applications');
      expect(title).toContain('(1)');
    });
  });

  it('filters pending orders correctly', () => {
    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.pendingOrders).toHaveLength(1);
    expect(vm.pendingOrders[0].id).toBe(mockPendingOrder.id);
    expect(vm.pendingOrders[0].status).toBe(OrderReviewStatus.Pending);
  });

  it('filters completed orders correctly', () => {
    // Override with an approved order received within the last 30 days
    const recentApprovedOrder = generateOrder(
      OrderReviewStatus.Approved,
      'Civil',
      new Date().toISOString().split('T')[0]
    );
    mockOrdersStore.orders = [mockPendingOrder, recentApprovedOrder];

    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.completedOrders).toHaveLength(1);
    expect(vm.completedOrders[0].id).toBe(recentApprovedOrder.id);
    expect(vm.completedOrders[0].status).toBe(OrderReviewStatus.Approved);
  });

  it('excludes completed orders received more than 30 days ago', () => {
    const daysAgo = (days: number) => {
      const d = new Date();
      d.setDate(d.getDate() - days);
      return d.toISOString().split('T')[0];
    };

    const recentApprovedOrder = generateOrder(
      OrderReviewStatus.Approved,
      'Civil',
      daysAgo(5)
    );
    const oldApprovedOrder = generateOrder(
      OrderReviewStatus.Approved,
      'Civil',
      daysAgo(31)
    );
    mockOrdersStore.orders = [recentApprovedOrder, oldApprovedOrder];

    const wrapper = createWrapper();

    const vm = wrapper.vm as any;
    expect(vm.completedOrders).toHaveLength(1);
    expect(vm.completedOrders[0].id).toBe(recentApprovedOrder.id);
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
      const { getCourtClassLabel, isCourtClassLabelCriminal } =
        await import('@/utils/utils');
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
      const { getCourtClassLabel, isCourtClassLabelCriminal } =
        await import('@/utils/utils');
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
