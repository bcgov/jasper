import CourtListTableSearchDialog from '@/components/courtlist/CourtListTableSearchDialog.vue';
import { mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { createVuetify } from 'vuetify';

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
    const wrapper: any = mount(CourtListTableSearchDialog, mountOptions);

    (wrapper.vm as unknown as { showDialog: boolean }).showDialog = true;

    await nextTick();

    const dialog = wrapper.find('v-dialog');
    const isShown = dialog.element.getAttribute('modelvalue') === 'true';

    expect(dialog.exists()).toBe(true);
    expect(isShown).toBe(true);
    expect(wrapper.vm.selectedReportType).toBe('Daily');
  });

  it(`modelValue is set to false when dialog not shown`, async () => {
    const wrapper = mount(CourtListTableSearchDialog, mountOptions);

    (wrapper.vm as unknown as { showDialog: boolean }).showDialog = false;

    await nextTick();

    const dialog = wrapper.find('v-dialog');
    const isHidden = dialog.element.getAttribute('modelvalue') === 'false';
    expect(isHidden).toBe(true);
  });

  it(`generateReport should validate and call onGenerate`, async () => {
    const wrapper = mount(CourtListTableSearchDialog, mountOptions);
    const button = wrapper.find('v-btn-tertiary');
    button.trigger('click');
    await nextTick();

    expect(mockOnGenerate).toHaveBeenCalled();
  });

  it(`clicking Print with no selectedReportType should not call onGenerate`, async () => {
    const wrapper: any = mount(CourtListTableSearchDialog, mountOptions);
    wrapper.vm.selectedReportType = null;

    await nextTick();

    const button = wrapper.find('v-btn-tertiary');
    button.trigger('click');

    expect(mockOnGenerate).not.toHaveBeenCalled();
  });
});
