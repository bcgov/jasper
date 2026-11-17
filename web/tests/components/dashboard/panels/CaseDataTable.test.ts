import { Case } from '@/types';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
import { mount } from '@vue/test-utils';
import CaseDataTable from 'CMP/dashboard/panels/CaseDataTable.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { defineComponent, nextTick } from 'vue';

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
  beforeEach(() => {
    setActivePinia(createPinia());
  });

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
    vm.drafts[row.id] = '01-Dec-2026';
    await vm.saveOverride(row);
    expect(save).toHaveBeenLastCalledWith(row, '2026-12-01');
    expect(vm.headers[0].value(row)).toBe('01-Dec-2026');
    vm.drafts[row.id] = '';
    await vm.saveOverride(row);
    expect(save).toHaveBeenLastCalledWith(row, null);
    expect(vm.headers[0].value(row)).toBe('03-Nov-2026');
  });

  it('restores the persisted display and exposes API failures', async () => {
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
    vm.drafts[mockData[0].id] = '01-Dec-2026';
    await vm.saveOverride(mockData[0]);
    expect(vm.drafts[mockData[0].id]).toBeUndefined();
    expect(vm.errors[mockData[0].id]).toBe('');
    expect(useSnackbarStore().message).toBe('Case changed. Refresh.');
    expect(useSnackbarStore().title).toBe('Error');
    expect(useSnackbarStore().isVisible).toBe(true);
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

  const mountEditor = (save = vi.fn(), permitted = true, columns: ('dueDate' | 'nextAppearance')[] = ['dueDate']) =>
    mount(CaseDataTable, {
      props: {
        data: [{ ...mockData[0], reason: '', dueDate: null }],
        columns,
        viewCaseDetails: mockViewCaseDetails,
        canEditDueDate: () => permitted,
        saveDueDate: save,
      },
      global: {
        stubs: {
          VDataTableVirtual: defineComponent({
            props: ['items'],
            template: '<div><template v-for="item in items"><slot name="item.dueDate" :item="item" /></template></div>',
          }),
          VTextField: defineComponent({
            props: ['modelValue', 'disabled', 'errorMessages'],
            emits: ['update:modelValue', 'update:focused'],
            template: '<div><input :value="modelValue" :disabled="disabled" @input="$emit(\'update:modelValue\', $event.target.value)" @focus="$emit(\'update:focused\', true)" @blur="$emit(\'update:focused\', false)" /><slot name="append-inner" /><span>{{ errorMessages }}</span></div>',
          }),
          VBtn: defineComponent({
            props: ['disabled', 'loading'],
            template: '<button :disabled="disabled || loading"><slot /></button>',
          }),
          VMenu: defineComponent({
            name: 'VMenu',
            props: ['modelValue'],
            emits: ['update:modelValue'],
            template: '<div><slot name="activator" :props="{}" /><slot /></div>',
          }),
          VDatePicker: defineComponent({
            name: 'VDatePicker',
            props: ['modelValue'],
            emits: ['update:modelValue'],
            template: '<div data-testid="calendar" />',
          }),
        },
      },
    });

  it('renders a blank RJ editor only in the permitted reserved judgments column', () => {
    const wrapper = mountEditor();
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('');
    expect(wrapper.find('[aria-label="Choose due date"]').exists()).toBe(true);
    expect(wrapper.findAll('button').some(button => button.text() === 'Save')).toBe(false);
    expect(mountEditor(vi.fn(), false).find('input').exists()).toBe(false);
    expect(mountEditor(vi.fn(), true, ['nextAppearance']).find('input').exists()).toBe(false);
  });

  it('commits a complete typed date on focus loss, using an ISO date-only request', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('01-Dec');
    expect(save).not.toHaveBeenCalled();
    await wrapper.get('input').setValue('01-Dec-2026');
    expect(save).not.toHaveBeenCalled();
    expect(wrapper.findAll('button').some(button => button.text() === 'Cancel')).toBe(false);
    await wrapper.get('input').trigger('focusout');
    expect(save).toHaveBeenCalledOnce();
    expect(save).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }), '2026-12-01');
    expect(useSnackbarStore().isVisible).toBe(false);
  });

  it('saves picker selections immediately using local calendar parts', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    wrapper.getComponent({ name: 'VDatePicker' }).vm.$emit('update:modelValue', new Date(2026, 11, 1));
    await nextTick();
    expect(save).toHaveBeenLastCalledWith(expect.anything(), '2026-12-01');
    expect(save).toHaveBeenCalledOnce();
    expect(useSnackbarStore().isVisible).toBe(false);
    await wrapper.setProps({ data: [{ ...wrapper.props('data')[0], overrideDueDate: '2026-12-01' }] });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('01-Dec-2026');
  });

  it('initially displays the DEC source date and restores it after clearing an override', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    const source = { ...mockData[0], reason: 'DEC', restrictionCode: 'S', dueDate: '03-Nov-2026' };
    await wrapper.setProps({ data: [source] });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('03-Nov-2026');
    await wrapper.setProps({ data: [{ ...source, overrideDueDate: '2026-12-01' }] });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('01-Dec-2026');
    await wrapper.get('input').setValue('');
    await wrapper.get('input').trigger('focusout');
    expect(save).toHaveBeenLastCalledWith(expect.anything(), null);
    await wrapper.setProps({ data: [{ ...source, overrideDueDate: null }] });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('03-Nov-2026');
  });

  it.each(['31-Feb-2026', '29-Feb-2027', '01-Dec', '2026-12-01', '32-Jan-2026', '01-Foo-2026'])(
    'rejects invalid or partial typed input: %s', async (date) => {
      const save = vi.fn();
      const wrapper = mountEditor(save);
      await wrapper.get('input').setValue(date);
      await wrapper.get('input').trigger('focusout');
      expect(save).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Enter a valid date as DD-MMM-YYYY.');
    }
  );

  it('cancels a draft and accepts a valid leap day', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('29-Feb-2028');
    await wrapper.get('input').trigger('keydown', { key: 'Escape' });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('');
    expect(save).not.toHaveBeenCalled();
    await wrapper.get('input').setValue('29-Feb-2028');
    await wrapper.get('input').trigger('focusout');
    expect(save).toHaveBeenLastCalledWith(expect.anything(), '2028-02-29');
  });

  it('does not save an unchanged override or a cancelled draft on focus loss', async () => {
    const save = vi.fn();
    const wrapper = mountEditor(save);
    await wrapper.get('input').trigger('focusout');
    await wrapper.get('input').setValue('01-Dec-2026');
    const calendar = wrapper.get('[aria-label="Choose due date"]');
    await wrapper.get('input').trigger('focusout', { relatedTarget: calendar.element });
    expect(save).not.toHaveBeenCalled();
    await wrapper.get('input').trigger('keydown', { key: 'Escape' });
    await wrapper.get('input').trigger('focusout');
    expect(save).not.toHaveBeenCalled();
    await wrapper.setProps({ data: [{ ...wrapper.props('data')[0], overrideDueDate: '2026-12-01' }] });
    await wrapper.get('input').setValue('01-Dec-2026');
    await wrapper.get('input').trigger('focusout');
    expect(save).not.toHaveBeenCalled();
  });

  it('defers focus-loss saving while the calendar is open and saves once after selection', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('01-Dec-2026');
    wrapper.getComponent({ name: 'VMenu' }).vm.$emit('update:modelValue', true);
    await wrapper.get('input').trigger('focusout');
    expect(save).not.toHaveBeenCalled();
    wrapper.getComponent({ name: 'VDatePicker' }).vm.$emit('update:modelValue', new Date(2026, 11, 2));
    await nextTick();
    await wrapper.get('input').trigger('focusout');
    expect(save).toHaveBeenCalledOnce();
    expect(save).toHaveBeenLastCalledWith(expect.anything(), '2026-12-02');
  });

  it('saves a typed draft when the calendar closes after focus has left the editor', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('01-Dec-2026');
    wrapper.getComponent({ name: 'VMenu' }).vm.$emit('update:modelValue', true);
    await wrapper.get('input').trigger('blur');
    await wrapper.get('input').trigger('focusout');
    expect(save).not.toHaveBeenCalled();
    wrapper.getComponent({ name: 'VMenu' }).vm.$emit('update:modelValue', false);
    await nextTick();
    expect(save).toHaveBeenCalledOnce();
    expect(save).toHaveBeenLastCalledWith(expect.anything(), '2026-12-01');
  });

  it('supports Enter to save and Escape to cancel an uncommitted change', async () => {
    const save = vi.fn().mockResolvedValue(undefined);
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('01-Dec-2026');
    await wrapper.get('input').trigger('keydown', { key: 'Escape' });
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('');
    expect(save).not.toHaveBeenCalled();
    await wrapper.get('input').setValue('01-Dec-2026');
    await wrapper.get('input').trigger('keydown', { key: 'Enter' });
    expect(save).toHaveBeenLastCalledWith(expect.anything(), '2026-12-01');
  });

  it('rejects invalid picker values and handles structured API validation errors', async () => {
    const save = vi.fn().mockRejectedValue({
      originalError: { response: { status: 400, data: { errors: { UpdatedDate: ['A revision is required.'] } } } },
    });
    const wrapper = mountEditor(save);
    wrapper.getComponent({ name: 'VDatePicker' }).vm.$emit('update:modelValue', new Date(Number.NaN));
    await nextTick();
    expect(save).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Enter a valid date as DD-MMM-YYYY.');
    await wrapper.get('input').setValue('01-Dec-2026');
    await wrapper.get('input').trigger('focusout');
    expect((wrapper.vm as any).errors['1']).toBe('');
    expect(useSnackbarStore().message).toBe('A revision is required.');
    expect((wrapper.vm as any).drafts['1']).toBeUndefined();
  });

  it('blocks repeated saves and ignores failures after replacement of the displayed data', async () => {
    let reject!: (error: Error) => void;
    const save = vi.fn(() => new Promise<void>((_resolve, fail) => { reject = fail; }));
    const wrapper = mountEditor(save);
    const vm = wrapper.vm as any;
    await wrapper.get('input').setValue('01-Dec-2026');
    const request = vm.saveOverride(wrapper.props('data')[0]);
    await vm.saveOverride(wrapper.props('data')[0]);
    expect(save).toHaveBeenCalledOnce();
    await nextTick();
    expect(wrapper.get('input').attributes('disabled')).toBeDefined();
    await wrapper.setProps({ data: [{ ...mockData[0], id: 'new' }] });
    reject(new Error('Old request failed'));
    await request;
    expect(vm.errors['1']).toBeUndefined();
    expect(vm.saving['1']).toBeUndefined();
    expect(useSnackbarStore().isVisible).toBe(false);
  });

  it.each([403, 404, 409, 500])('restores the persisted date on HTTP %i with useful feedback', async (status) => {
    const save = vi.fn().mockRejectedValue({ originalError: { response: { status } } });
    const wrapper = mountEditor(save);
    await wrapper.get('input').setValue('01-Dec-2026');
    await wrapper.get('input').trigger('focusout');
    await nextTick();
    expect((wrapper.get('input').element as HTMLInputElement).value).toBe('');
    expect((wrapper.vm as any).errors['1']).toBe('');
    expect(useSnackbarStore().isVisible).toBe(true);
    expect(useSnackbarStore().title).toBe('Error');
    expect(useSnackbarStore().color).toBe('#b84157');
    expect(useSnackbarStore().message).toBeTruthy();
  });
});
