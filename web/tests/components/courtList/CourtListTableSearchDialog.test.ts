import CourtListTableSearchDialog from '@/components/courtlist/CourtListTableSearchDialog.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { createVuetify } from 'vuetify';
import { VForm } from 'vuetify/lib/components/index.mjs';

const vuetify = createVuetify();

describe('CourtListTableSearchDialog', () => {
  const mockOnGenerate = vi.fn();
  const mountOptions = {
    props: {
      onGenerate: mockOnGenerate,
    },
    global: {
      plugins: [vuetify],
    },
  };

  it('modelValue is set to true when dialog is shown', async () => {
    const wrapper = mount(CourtListTableSearchDialog, mountOptions);

    (wrapper.vm as unknown as { showDialog: boolean }).showDialog = true;

    await nextTick();

    const dialog = wrapper.find('v-dialog');
    const isShown = dialog.element.getAttribute('modelvalue') === 'true';

    expect(dialog.exists()).toBe(true);
    expect(isShown).toBe(true);
  });

  it(`modelValue is set to false when dialog not shown`, async () => {
    const wrapper = mount(CourtListTableSearchDialog, mountOptions);

    (wrapper.vm as unknown as { showDialog: boolean }).showDialog = false;

    await nextTick();

    const dialog = wrapper.find('v-dialog');
    const isHidden = dialog.element.getAttribute('modelvalue') === 'false';
    expect(isHidden).toBe(true);
  });

  it(`should show report type when selected class is of type 'criminal'`, async () => {
    const wrapper: any = mount(CourtListTableSearchDialog, mountOptions);
    wrapper.vm.types = [
      {
        shortDesc: 'Adult',
        code: 'A',
        codeType: '',
        longDesc: 'R',
      },
      {
        code: 'L',
        shortDesc: 'Enforcement/Legislated Statute',
        longDesc: 'I',
        codeType: '',
      },
    ];
    wrapper.vm.selectedType = 'A';

    await nextTick();

    const allSelect = wrapper.findAll('v-select');

    expect(allSelect.length).toBe(2);
    expect(allSelect[1].element.id).toBe('reportType');
  });

  it(`should show additions when selected class is of type 'civil'`, async () => {
    const wrapper: any = mount(CourtListTableSearchDialog, mountOptions);

    wrapper.vm.types = [
      {
        shortDesc: 'Adult',
        code: 'A',
        codeType: '',
        longDesc: 'R',
      },
      {
        code: 'L',
        shortDesc: 'Enforcement/Legislated Statute',
        longDesc: 'I',
        codeType: '',
      },
    ];
    wrapper.vm.selectedType = 'L';

    await nextTick();

    const allSelect = wrapper.findAll('v-select');

    expect(allSelect.length).toBe(2);
    expect(allSelect[1].element.id).toBe('additions');
  });

  it(`clicking Print should validate and emit 'update:showDialog'`, async () => {
    const wrapper = mount(CourtListTableSearchDialog, mountOptions);

    const vm = wrapper.vm as unknown as {
      selectedType: string;
      selectedReportType: string;
      generateReport: () => void;
      form: VForm;
    };

    vm.selectedType = 'A';
    vm.selectedReportType = 'Daily';

    vm.generateReport();

    await nextTick();

    expect(mockOnGenerate).toHaveBeenCalled();
  });
});
