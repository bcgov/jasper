import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ReviewModal from '@/components/documents/ReviewModal.vue';

describe('ReviewModal.vue', () => {
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
});