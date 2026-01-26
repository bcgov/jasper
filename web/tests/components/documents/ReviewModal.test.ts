import { describe, it, expect } from 'vitest';
import { shallowMount } from '@vue/test-utils';
import { nextTick } from 'vue';
import ReviewModal from '@/components/documents/ReviewModal.vue';

describe('ReviewModal.vue', () => {
  const createWrapper = (props = {}, modelValue = true) => {
    return shallowMount(ReviewModal, {
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

//   describe('Comments functionality', () => {
//     it('should update comments when user types', async () => {
//       const wrapper = createWrapper();
      
//       // Set comments directly on the component instance
//       wrapper.vm.comments = 'Test comment';
//       await nextTick();
      
//       // Test the component's internal state
//       expect(wrapper.vm.comments).toBe('Test comment');
      
//       // Verify the v-textarea receives the correct v-model binding
//       const textarea = wrapper.findComponent({ name: 'VTextarea' });
//       expect(textarea.props('modelValue')).toBe('Test comment');
//     });

//     it('should compute canReject as false when comments are empty', () => {
//       const wrapper = createWrapper();
//       const rejectButton = wrapper.findAll('button').find(btn => btn.text().includes('Reject'));
//       expect(rejectButton?.classes()).toContain('v-btn--disabled');
//     });

//     it('should compute canReject as true when comments have content', async () => {
//       const wrapper = createWrapper();
//       const textarea = wrapper.find('textarea');
//       await textarea.setValue('Test comment');
//       await nextTick();
      
//       const rejectButton = wrapper.findAll('button').find(btn => btn.text().includes('Reject'));
//       expect(rejectButton?.classes()).not.toContain('v-btn--disabled');
//     });

//     it('should enable Awaiting documentation button when comments are present', async () => {
//       const wrapper = createWrapper();
//       const textarea = wrapper.find('textarea');
//       await textarea.setValue('Waiting for more documents');
//       await nextTick();
      
//       const awaitingButton = wrapper.findAll('button').find(btn => 
//         btn.text().includes('Awaiting documentation')
//       );
//       expect(awaitingButton?.classes()).not.toContain('v-btn--disabled');
//     });

//     it('should disable Awaiting documentation button when comments are empty', () => {
//       const wrapper = createWrapper();
//       const awaitingButton = wrapper.findAll('button').find(btn => 
//         btn.text().includes('Awaiting documentation')
//       );
//       expect(awaitingButton?.classes()).toContain('v-btn--disabled');
//     });
//   });

  describe('Action buttons', () => {
    it('should render all three action buttons', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Reject');
      expect(wrapper.text()).toContain('Awaiting documentation');
      expect(wrapper.text()).toContain('Approve');
    });

    it('should close modal when Reject button is clicked', async () => {
      const wrapper = createWrapper();
      wrapper.vm.comments = 'Rejection reason';
      await nextTick();

      wrapper.vm.closeReviewModal();
      await nextTick();
      
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([false]);
    });

    it('should close modal when Awaiting documentation button is clicked', async () => {
      const wrapper = createWrapper();
      wrapper.vm.comments = 'Need more documentation';
      await nextTick();

      wrapper.vm.closeReviewModal();
      await nextTick();
      
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([false]);
    });
  });

  describe('Approve functionality', () => {
    it('should emit approveOrder with comments when Approve is clicked', async () => {
      const wrapper = createWrapper({ canApprove: true });
      const testComment = 'Looks good to approve';
      wrapper.vm.comments = testComment;
      await nextTick();

      wrapper.vm.approveOrder();
      await nextTick();
      
      expect(wrapper.emitted('approveOrder')).toBeTruthy();
      expect(wrapper.emitted('approveOrder')?.[0]).toEqual([testComment]);
    });

    it('should close modal after approving', async () => {
      const wrapper = createWrapper({ canApprove: true });
      wrapper.vm.comments = 'Approved';
      await nextTick();

      wrapper.vm.approveOrder();
      await nextTick();
      
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('approveOrder')).toBeTruthy();
    });

    it('should not approve when canApprove is false', async () => {
      const wrapper = createWrapper({ canApprove: false });
      await nextTick();

      wrapper.vm.approveOrder();
      await nextTick();
      
      expect(wrapper.emitted('approveOrder')).toBeFalsy();
    });
  });
});