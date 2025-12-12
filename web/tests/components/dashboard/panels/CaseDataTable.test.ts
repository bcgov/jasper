import { Case } from '@/types';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import CaseDataTable from 'CMP/dashboard/panels/CaseDataTable.vue';
import { describe, expect, it, vi } from 'vitest';

const mockData: Case[] = [
  {
    id: '1',
    appearanceId: 'app-1',
    fileNumber: '12345',
    courtFileNumber: 'CF-12345',
    appearanceDate: '2024-06-01',
    dueDate: '2024-07-01',
    ageInDays: 10,
    courtClass: 'CC',
    physicalFileId: 'phys-1',
    styleOfCause: 'Smith v. Jones',
    reason: 'Reserved',
    partId: 'part-1',
  },
  {
    id: '2',
    appearanceId: 'app-2',
    fileNumber: '67890',
    courtFileNumber: 'CF-67890',
    appearanceDate: '2024-05-20',
    dueDate: '2024-06-20',
    ageInDays: 22,
    courtClass: 'SC',
    physicalFileId: 'phys-2',
    styleOfCause: 'Doe v. Roe',
    reason: 'Continuation',
    partId: 'part-2',
  },
];

const mockViewCaseDetails = vi.fn();

describe('CaseDataTable.vue', () => {
  it('renders table with default columns when no columns prop is provided', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });
    const headers = (wrapper.vm as any).headers;
    const headerTitles = headers.map((h: any) => h.title);
    expect(headerTitles).toEqual([
      'FILE #',
      'ACCUSED / PARTIES',
      'DIVISION',
      'NEXT APPEARANCE',
      'REASON',
      'LAST APPEARANCE',
      'CASE AGE (days)',
    ]);
  });

  it('renders table with custom columns when columns prop is provided', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        columns: [
          'fileNumber',
          'styleOfCause',
          'division',
          'decisionDate',
          'reason',
          'lastAppearance',
          'dueDate',
          'caseAge',
        ],
      },
    });
    const headers = (wrapper.vm as any).headers;
    const headerTitles = headers.map((h: any) => h.title);
    expect(headerTitles).toEqual([
      'FILE #',
      'ACCUSED / PARTIES',
      'DIVISION',
      'DECISION DATE',
      'REASON',
      'LAST APPEARANCE',
      'DUE DATE',
      'CASE AGE (days)',
    ]);
  });

  it('formats LAST APPEARANCE date using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });
    const headers = (wrapper.vm as any).headers;
    const lastAppearanceHeader = headers.find(
      (h: any) => h.key === 'appearanceDate'
    );
    expect(lastAppearanceHeader).toBeDefined();
    const formatted = lastAppearanceHeader.value(mockData[0]);
    expect(formatted).toBe(
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].appearanceDate))
    );
  });

  it('formats NEXT APPEARANCE date using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });
    const headers = (wrapper.vm as any).headers;
    const nextAppearanceHeader = headers.find((h: any) => h.key === 'dueDate');
    expect(nextAppearanceHeader).toBeDefined();
    const formatted = nextAppearanceHeader.value(mockData[0]);
    expect(formatted).toBe(
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].dueDate))
    );
  });

  it('formats DECISION DATE using formatDateInstanceToDDMMMYYYY', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        columns: ['decisionDate'],
      },
    });
    const headers = (wrapper.vm as any).headers;
    const decisionDateHeader = headers.find(
      (h: any) => h.title === 'DECISION DATE'
    );
    expect(decisionDateHeader).toBeDefined();
    const formatted = decisionDateHeader.value(mockData[0]);
    expect(formatted).toBe(
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].dueDate))
    );
  });

  it('sets custom sortBy when provided', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        sortBy: [{ key: 'cc', order: 'desc' }],
      },
    });
    const sortBy = (wrapper.vm as any).sortBy;
    expect(sortBy[0].key).toBe('cc');
    expect(sortBy[0].order).toBe('desc');
  });

  it('sets default sortBy when no sortBy prop is provided', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });
    const sortBy = (wrapper.vm as any).sortBy;
    expect(sortBy).toEqual([{ key: 'dueDate', order: 'asc' }]);
  });

  it('calls viewCaseDetails when style of cause link is clicked', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    const mockItem = mockData[0];
    wrapper.vm.viewCaseDetails(mockItem);

    expect(mockViewCaseDetails).toHaveBeenCalledWith(mockItem);
  });

  it('passes correct data to v-data-table-virtual', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
      },
    });

    expect((wrapper.vm as any).data).toEqual(mockData);
  });
});
