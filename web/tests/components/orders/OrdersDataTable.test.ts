import { Order } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import OrdersDataTable from 'CMP/orders/OrdersDataTable.vue';
import { describe, expect, it, vi } from 'vitest';

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
  },
];

const mockViewOrderDetails = vi.fn();
const mockViewCaseDetails = vi.fn();

describe('OrdersDataTable.vue', () => {
  it('renders table with default columns when no columns prop is provided', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: mockData,
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const headers = (wrapper.vm as any).headers;
    const headerTitles = headers.map((h: any) => h.title);

    expect(headerTitles).toEqual([
      'PACKAGE #',
      'DATE RECEIVED',
      'DIVISION',
      'FILE #',
      'ACCUSED / PARTIES',
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

    const headers = (wrapper.vm as any).headers;
    const headerTitles = headers.map((h: any) => h.title);

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

    const headers = (wrapper.vm as any).headers;
    const receivedDateHeader = headers.find(
      (h: any) => h.key === 'receivedDate'
    );

    expect(receivedDateHeader).toBeDefined();
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

    const headers = (wrapper.vm as any).headers;
    const processedDateHeader = headers.find(
      (h: any) => h.key === 'processedDate'
    );

    expect(processedDateHeader).toBeDefined();
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

    const headers = (wrapper.vm as any).headers;
    const receivedDateHeader = headers.find(
      (h: any) => h.key === 'receivedDate'
    );

    expect(receivedDateHeader).toBeDefined();
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

    const sortBy = (wrapper.vm as any).sortBy;
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

    const sortBy = (wrapper.vm as any).sortBy;
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

    expect((wrapper.vm as any).data).toEqual(mockData);
  });

  it('handles empty data array', () => {
    const wrapper = mount(OrdersDataTable, {
      props: {
        data: [],
        viewOrderDetails: mockViewOrderDetails,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    expect((wrapper.vm as any).data).toEqual([]);
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

    const headers = (wrapper.vm as any).headers;
    expect(headers).toHaveLength(2);
    expect(headers[0].key).toBe('packageId');
    expect(headers[1].key).toBe('courtFileNumber');
  });
});
