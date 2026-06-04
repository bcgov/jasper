import { Order } from '@/types';
import { Anchor } from '@/types/common';
import { OrderReviewStatus } from '@/types/common';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import { defineComponent } from 'vue';
import OrdersDataTable from 'CMP/orders/OrdersDataTable.vue';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

type TableHeader = {
  title: string;
  key: string;
  value?: (item: Order) => string;
  sort?: (a: string, b: string) => number;
};

type OrdersDataTableVm = {
  headers: TableHeader[];
  sortBy: { key: string; order: 'asc' | 'desc' }[];
  data: Order[];
  viewOrderDetails: (item: Order) => void;
  viewCaseDetails: (item: Order) => void;
  isAgedOrder: (order: Order) => boolean;
  getRowProps: (params: { item: Order }) => { class: string | undefined };
};

// Mock the utils
vi.mock('@/utils/utils', () => ({
  getCourtClassLabel: vi.fn((courtClass: string) => {
    if (courtClass === 'CC') return 'Criminal';
    if (courtClass === 'CV') return 'Civil';
    return courtClass;
  }),
  getCourtClassStyle: vi.fn((courtClass: string) => {
    if (courtClass === 'CC') return 'criminal-class';
    if (courtClass === 'CV') return 'civil-class';
    return '';
  }),
}));

const mockData: Order[] = [
  {
    id: '1',
    packageId: 12345,
    packageDocumentId: '340',
    packageName: 'test 1',
    receivedDate: '2026-01-15',
    processedDate: '2026-01-20',
    courtClass: 'CC',
    courtFileNumber: 'CF-2026-001',
    styleOfCause: 'R v Smith',
    physicalFileId: 'file-001',
    status: OrderReviewStatus.Pending,
    priorityType: 'TST',
    priorityTypeDescription: 'Test Priority Description',
    courtListType: 'Order',
  },
  {
    id: '2',
    packageId: 67890,
    packageDocumentId: '341',
    packageName: 'test 2',
    receivedDate: '2026-01-14',
    processedDate: '2026-01-21',
    courtClass: 'CV',
    courtFileNumber: 'CV-2026-001',
    styleOfCause: 'Jones v Brown',
    physicalFileId: 'file-002',
    status: OrderReviewStatus.Approved,
    priorityType: 'IS',
    courtListType: 'Application',
  },
];

const mockViewOrderDetails = vi.fn();
const mockViewCaseDetails = vi.fn();

const VDataTableVirtualStub = defineComponent({
  name: 'VDataTableVirtual',
  props: {
    items: {
      type: Array,
      required: true,
    },
  },
  template: `
    <div>
      <slot name="item.priorityType" :item="items[0]" />
      <slot name="item.courtListType" :item="items[0]" />
    </div>
  `,
});

const LabelWithTooltipStub = defineComponent({
  name: 'LabelWithTooltip',
  props: {
    values: {
      type: Array,
      required: true,
    },
    appendCount: {
      type: Boolean,
      default: true,
    },
    location: {
      type: String,
      default: undefined,
    },
  },
  template: '<div class="label-with-tooltip-stub"></div>',
});

const getVm = (wrapper: ReturnType<typeof mount>) =>
  wrapper.vm as unknown as OrdersDataTableVm;

describe('OrdersDataTable.vue', () => {
  it('renders table with default columns when no columns prop is provided', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const headers = getVm(wrapper).headers;
    const headerTitles = headers.map((h) => h.title);

    expect(headerTitles).toEqual([
      'PACKAGE #',
      'PRIORITY',
      'TYPE',
      'DATE RECEIVED',
      'DIVISION',
      'FILE #',
      'ACCUSED / PARTIES',
      'NOTES',
    ]);
  });

  it('renders table with custom columns when columns prop is provided', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
        columns: [
          'packageId',
          'receivedDate',
          'processedDate',
          'division',
          'fileNumber',
          'styleOfCause',
        ],
      },
    });

    const headers = getVm(wrapper).headers;
    const headerTitles = headers.map((h) => h.title);

    expect(headerTitles).toEqual([
      'PACKAGE #',
      'DATE RECEIVED',
      'DATE PROCESSED',
      'DIVISION',
      'FILE #',
      'ACCUSED / PARTIES',
    ]);
  });

  it('formats DATE RECEIVED using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const headers = getVm(wrapper).headers;
    const receivedDateHeader = headers.find((h) => h.key === 'receivedDate');

    expect(receivedDateHeader).toBeDefined();
    if (!receivedDateHeader?.value) {
      throw new Error('Expected receivedDateHeader.value to be defined');
    }
    const formatted = receivedDateHeader.value(mockData[0]);
    expect(formatted).toBe(
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].receivedDate))
    );
  });

  it('formats DATE PROCESSED using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
        columns: ['processedDate'],
      },
    });

    const headers = getVm(wrapper).headers;
    const processedDateHeader = headers.find((h) => h.key === 'processedDate');

    expect(processedDateHeader).toBeDefined();
    if (!processedDateHeader?.value) {
      throw new Error('Expected processedDateHeader.value to be defined');
    }
    const formatted = processedDateHeader.value(mockData[0]);
    expect(formatted).toBe(
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].processedDate))
    );
  });

  it('sorts dates correctly', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const headers = getVm(wrapper).headers;
    const receivedDateHeader = headers.find((h) => h.key === 'receivedDate');

    expect(receivedDateHeader).toBeDefined();
    if (!receivedDateHeader?.sort) {
      throw new Error('Expected receivedDateHeader.sort to be defined');
    }
    const sortResult = receivedDateHeader.sort(
      mockData[0].receivedDate,
      mockData[1].receivedDate
    );

    // 2026-01-15 is later than 2026-01-14, so should return positive
    expect(sortResult).toBeGreaterThan(0);
  });

  it('sets custom sortBy when provided', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
        sortBy: [{ key: 'packageId', order: 'desc' }],
      },
    });

    const sortBy = getVm(wrapper).sortBy;
    expect(sortBy[0].key).toBe('packageId');
    expect(sortBy[0].order).toBe('desc');
  });

  it('sets default sortBy when no sortBy prop is provided', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const sortBy = getVm(wrapper).sortBy;
    expect(sortBy).toEqual([{ key: 'receivedDate', order: 'asc' }]);
  });

  it('calls viewOrderDetails when package number link is clicked', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const mockItem = mockData[0];
    wrapper.vm.viewOrderDetails(mockItem);

    expect(mockViewOrderDetails).toHaveBeenCalledWith(mockItem);
  });

  it('calls viewCaseDetails when style of cause link is clicked', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const mockItem = mockData[0];
    wrapper.vm.viewCaseDetails(mockItem);

    expect(mockViewCaseDetails).toHaveBeenCalledWith(mockItem);
  });

  it('passes correct data to v-data-table-virtual', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    expect(getVm(wrapper).data).toEqual(mockData);
  });

  it('handles empty data array', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: [],
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    expect(getVm(wrapper).data).toEqual([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('maps column keys to correct headers', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
        columns: ['packageId', 'fileNumber'],
      },
    });

    const headers = getVm(wrapper).headers;
    expect(headers).toHaveLength(2);
    expect(headers[0].key).toBe('packageId');
    expect(headers[1].key).toBe('courtFileNumber');
  });

  it('renders LabelWithTooltip for priorityType when description is present', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: [mockData[0]],
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
      global: {
        stubs: {
          VDataTableVirtual: VDataTableVirtualStub,
          LabelWithTooltip: LabelWithTooltipStub,
        },
      },
    });

    const tooltips = wrapper.findAllComponents(LabelWithTooltipStub);
    expect(tooltips).toHaveLength(1);

    expect(tooltips[0].props('values')).toEqual([
      'TST',
      'Test Priority Description',
    ]);
    expect(tooltips[0].props('appendCount')).toBe(false);
    expect(tooltips[0].props('location')).toBe(Anchor.Top);
  });

  it('renders courtListType as plain text (already mapped to display label)', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: [mockData[0]],
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
      global: {
        stubs: {
          VDataTableVirtual: VDataTableVirtualStub,
          LabelWithTooltip: LabelWithTooltipStub,
        },
      },
    });

    expect(wrapper.text()).toContain('Order');
  });

  it('renders plain text for priorityType when description is missing', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: [mockData[1]],
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
      global: {
        stubs: {
          VDataTableVirtual: VDataTableVirtualStub,
          LabelWithTooltip: LabelWithTooltipStub,
        },
      },
    });

    const tooltips = wrapper.findAllComponents(LabelWithTooltipStub);
    expect(tooltips).toHaveLength(0);
    expect(wrapper.text()).toContain('IS');
    expect(wrapper.text()).toContain('Application');
  });

  describe('isAgedOrder', () => {
    const buildOrder = (receivedDate: string): Order => ({
      ...mockData[0],
      receivedDate,
    });

    beforeEach(() => {
      vi.useFakeTimers();
      // Fix "now" to 2026-06-04 so date math is deterministic.
      vi.setSystemTime(new Date('2026-06-04T12:00:00'));
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('returns false when highlightAgedOrders is not enabled', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
        },
      });

      // Received 30 days ago - would be "aged" if highlighting were on.
      const order = buildOrder('05-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns false when highlightAgedOrders is explicitly false', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: false,
        },
      });

      const order = buildOrder('05-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns false when receivedDate is not in the expected dd-MMM-yyyy format', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      const order = buildOrder('2026-05-05');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns false when receivedDate is an empty string', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      const order = buildOrder('');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns true when received date exceeds the default 5-day threshold', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      // 6 days before 2026-06-04 => 2026-05-29
      const order = buildOrder('29-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(true);
    });

    it('returns false when received date equals the default 5-day threshold (strictly greater than)', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      // 5 days before 2026-06-04 => 2026-05-30
      const order = buildOrder('30-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns false when received date is within the default 5-day threshold', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      // 1 day before 2026-06-04 => 2026-06-03
      const order = buildOrder('03-Jun-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('returns false when received date is today', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      const order = buildOrder('04-Jun-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('honours a custom agedOrdersThresholdDays when exceeded', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
          agedOrdersThresholdDays: 10,
        },
      });

      // 11 days before 2026-06-04 => 2026-05-24
      const order = buildOrder('24-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(true);
    });

    it('honours a custom agedOrdersThresholdDays when not exceeded', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
          agedOrdersThresholdDays: 10,
        },
      });

      // 9 days before 2026-06-04 => 2026-05-26 (within threshold)
      const order = buildOrder('26-May-2026');
      expect(getVm(wrapper).isAgedOrder(order)).toBe(false);
    });

    it('treats a threshold of 0 as "any past day is aged"', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
          agedOrdersThresholdDays: 0,
        },
      });

      const yesterday = buildOrder('03-Jun-2026');
      const today = buildOrder('04-Jun-2026');
      expect(getVm(wrapper).isAgedOrder(yesterday)).toBe(true);
      expect(getVm(wrapper).isAgedOrder(today)).toBe(false);
    });

    it('getRowProps applies the aged-row class when the order is aged', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      const agedOrder = buildOrder('29-May-2026');
      expect(getVm(wrapper).getRowProps({ item: agedOrder })).toEqual({
        class: 'aged-row',
      });
    });

    it('getRowProps does not apply the aged-row class when the order is not aged', () => {
      const wrapper = mount(OrdersDataTable, {
        props: {
          data: mockData,
          viewOrderDetails: mockViewOrderDetails,
          viewCaseDetails: mockViewCaseDetails,
          highlightAgedOrders: true,
        },
      });

      const freshOrder = buildOrder('03-Jun-2026');
      expect(getVm(wrapper).getRowProps({ item: freshOrder })).toEqual({
        class: undefined,
      });
    });
  });
});
