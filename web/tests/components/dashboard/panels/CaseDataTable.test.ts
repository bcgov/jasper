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
      formatDateInstanceToDDMMMYYYY(new Date(mockData[0].dueDate!))
    );
  });

  it('formats DECISION DATE without shifting the source calendar date', () => {
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
    expect(formatted).toBe('01-Jul-2024');
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

  it('keeps the original decision date distinct from an effective override, including on reload', () => {
    const row = {
      ...mockData[0],
      dueDate: '2026-11-03',
      overrideDueDate: '2026-12-01',
    };
    for (let load = 0; load < 2; load++) {
      const wrapper = mount(CaseDataTable, {
        props: {
          data: [row],
          viewCaseDetails: mockViewCaseDetails,
          columns: ['decisionDate', 'dueDate'],
        },
      });
      const headers = (wrapper.vm as any).headers;
      expect(
        headers.find((h: any) => h.key === 'decisionDate').value(row)
      ).toBe('03-Nov-2026');
      expect(headers.find((h: any) => h.key === 'dueDate').value(row)).toBe(
        '01-Dec-2026'
      );
      wrapper.unmount();
    }
  });

  it('uses the decision date as the default and displays missing dates as blank', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        columns: ['dueDate'],
      },
    });
    const header = (wrapper.vm as any).headers[0];
    expect(header.value({ ...mockData[0], dueDate: '03-Nov-2026' })).toBe(
      '03-Nov-2026'
    );
    expect(header.value({ ...mockData[0], dueDate: null })).toBe('');
    expect((wrapper.vm as any).inputDate('2026-11-03')).toBe('2026-11-03');
  });

  it('parses and sorts source date strings explicitly', () => {
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        columns: ['decisionDate', 'dueDate'],
      },
    });
    const vm = wrapper.vm as any;
    expect(vm.inputDate('03-Nov-2026')).toBe('2026-11-03');
    expect(vm.inputDate('31-Feb-2026')).toBe('');
    for (const header of vm.headers) {
      expect(header.sort('01-Nov-2026', '31-Oct-2026')).toBeGreaterThan(0);
      expect(header.sort('31-Oct-2026', '01-Nov-2026')).toBeLessThan(0);
    }
  });

  it('saves and clears an override, restoring the source default', async () => {
    const row = {
      ...mockData[0],
      dueDate: '2026-11-03',
      overrideDueDate: null as string | null,
    };
    const save = vi.fn(async (_item: Case, date: string | null) => {
      row.overrideDueDate = date;
    });
    const wrapper = mount(CaseDataTable, {
      props: {
        data: [row],
        viewCaseDetails: mockViewCaseDetails,
        columns: ['dueDate'],
        canEditDueDate: () => true,
        saveDueDate: save,
      },
    });
    const vm = wrapper.vm as any;
    vm.drafts[row.id] = '2026-12-01';
    await vm.saveOverride(row);
    expect(save).toHaveBeenLastCalledWith(row, '2026-12-01');
    expect(vm.headers[0].value(row)).toBe('01-Dec-2026');
    vm.drafts[row.id] = '';
    await vm.saveOverride(row);
    expect(save).toHaveBeenLastCalledWith(row, null);
    expect(vm.headers[0].value(row)).toBe('03-Nov-2026');
  });

  it('keeps an unsaved selection and exposes API failures for retry', async () => {
    const save = vi.fn().mockRejectedValue({
      originalError: {
        response: { data: { errors: ['Case changed. Refresh.'] } },
      },
    });
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        canEditDueDate: () => true,
        saveDueDate: save,
      },
    });
    const vm = wrapper.vm as any;
    vm.drafts[mockData[0].id] = '2026-12-01';
    await vm.saveOverride(mockData[0]);
    expect(vm.drafts[mockData[0].id]).toBe('2026-12-01');
    expect(vm.errors[mockData[0].id]).toBe('Case changed. Refresh.');
    expect(vm.saving[mockData[0].id]).toBe(false);
  });

  it('does not submit edits when the displayed judge does not own the record', async () => {
    const save = vi.fn();
    const wrapper = mount(CaseDataTable, {
      props: {
        data: mockData,
        viewCaseDetails: mockViewCaseDetails,
        canEditDueDate: () => false,
        saveDueDate: save,
      },
    });
    await (wrapper.vm as any).saveOverride(mockData[0]);
    expect(save).not.toHaveBeenCalled();
  });
});
