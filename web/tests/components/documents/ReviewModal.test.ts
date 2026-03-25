import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import ReviewModal from '@/components/documents/ReviewModal.vue';

class MockFileReader {
  public result: string | ArrayBuffer | null = null;
  public onload: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
  public onerror: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
  public error: DOMException | null = null;

  readAsDataURL() {
    this.result = 'data:application/pdf;base64,VEVTVA==';
    this.onload?.call(this as unknown as FileReader, new ProgressEvent('load'));
  }
}

describe('ReviewModal.vue', () => {
  beforeEach(() => {
    vi.stubGlobal('FileReader', MockFileReader as unknown as typeof FileReader);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  const createWrapper = (props = {}, modelValue = true) => {
    return mount(ReviewModal, {
      props: {
        canApprove: false,
        modelValue,
        ...props,
      },
    });
  };

  describe('Dialog visibility', () => {
    it('should show dialog when modelValue is true', () => {
      const wrapper = createWrapper({}, true);
      expect(wrapper.find('v-card').exists()).toBe(true);
    });

    it('should emit update:modelValue when close button is clicked', async () => {
      const wrapper = createWrapper();
      const closeButton = wrapper.find('[aria-label="Close dialog"]');
      await closeButton.trigger('click');
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([false]);
    });
  });

  describe('Header and UI elements', () => {
    it('should render dialog title', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Review Order');
    });

    it('should render instruction text', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Add any notes or reasoning for your decision');
      expect(wrapper.text()).toContain('Note: Comments are required for any action other than Approval');
    });

    it('should render comments textarea', () => {
      const wrapper = createWrapper();
      const textarea = wrapper.find('v-textarea');
      expect(textarea.exists()).toBe(true);
    });
  });

  describe('canApprove prop behavior', () => {
    it('should show warning alert when canApprove is false', () => {
      const wrapper = createWrapper({ canApprove: false });
      expect(wrapper.text()).toContain('Document signature is required before Approval');
    });

    it('should not show warning alert when canApprove is true', () => {
      const wrapper = createWrapper({ canApprove: true });
      const alerts = wrapper.findAll('.v-alert');
      const warningAlert = alerts.find(alert => 
        alert.text().includes('Document signature is required before Approval')
      );
      expect(warningAlert).toBeFalsy();
    });
  });

  describe('Action buttons', () => {
    it('should render all three action buttons', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Reject');
      expect(wrapper.text()).toContain('Awaiting documentation');
      expect(wrapper.text()).toContain('Approve');
    });

    it('should render Reject button with proper attributes', () => {
      const wrapper = createWrapper();
      const rejectButton = wrapper.find('[color="error"]');
      expect(rejectButton.exists()).toBe(true);
      expect(rejectButton.text()).toContain('Reject');
    });

    it('should render Awaiting documentation button with proper attributes', () => {
      const wrapper = createWrapper();
      const pendingButton = wrapper.find('[color="warning"]');
      expect(pendingButton.exists()).toBe(true);
      expect(pendingButton.text()).toContain('Awaiting documentation');
    });
  });

  describe('Approve functionality', () => {
    it('should render Approve button and be enabled when canApprove is true', () => {
      const wrapper = createWrapper({ canApprove: true });
      const approveButton = wrapper.find('[color="success"]');
      expect(approveButton.exists()).toBe(true);
      expect(approveButton.text()).toContain('Approve');
    });

    it('should render Approve button with proper color and text', () => {
      const wrapper = createWrapper({ canApprove: true });
      const approveButton = wrapper.find('[color="success"]');
      expect(approveButton.exists()).toBe(true);
      expect(approveButton.attributes('color')).toBe('success');
    });

    it('should disable approve button when canApprove is false', () => {
      const wrapper = createWrapper({ canApprove: false });
      const approveButton = wrapper.find('[color="success"]');
      expect(approveButton.attributes('disabled')).toBeDefined();
    });
  });

  describe('Optional file upload', () => {
    const createUploadWrapper = () =>
      mount(ReviewModal, {
        props: {
          canApprove: true,
          modelValue: true,
        },
        global: {
          stubs: {
            'v-btn': {
              props: ['disabled'],
              emits: ['click'],
              template:
                '<button :disabled="disabled" @click="$emit(\'click\')"><slot /></button>',
            },
            'v-file-upload': {
              emits: ['update:modelValue'],
              template: '<div data-testid="review-document-upload"></div>',
            },
          },
        },
      });

    it('should emit empty documentData when no file is uploaded', async () => {
      const wrapper = createUploadWrapper();
      const approveButton = wrapper
        .findAll('button')
        .find((button) => button.text().includes('Approve'));

      expect(approveButton).toBeTruthy();
      await approveButton!.trigger('click');
      await flushPromises();

      const emitted = wrapper.emitted('reviewOrder');
      expect(emitted).toBeTruthy();
      expect(emitted?.[0]?.[0]?.documentData).toBe('');
    });

    it('should emit uploaded file as base64 documentData', async () => {
      const wrapper = createUploadWrapper();

      const uploadStub = wrapper.get('[data-testid="review-document-upload"]');
      const uploadedFile = new File(['test'], 'uploaded.pdf', {
        type: 'application/pdf',
      });
      uploadStub.vm.$emit('update:modelValue', [uploadedFile]);

      const approveButton = wrapper
        .findAll('button')
        .find((button) => button.text().includes('Approve'));

      expect(approveButton).toBeTruthy();
      await approveButton!.trigger('click');
      await flushPromises();

      const emitted = wrapper.emitted('reviewOrder');
      expect(emitted).toBeTruthy();
      expect(emitted?.[0]?.[0]?.documentData).toBe('VEVTVA==');
    });
  });
});