import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { nextTick } from 'vue';
import ReviewModal from '@/components/documents/ReviewModal.vue';
import type { OrderReview } from '@/types';
import { OrderCourtLisTypeEnum, OrderReviewStatus } from '@/types/common';

const mockRoute: { query: Record<string, string | undefined> } = { query: {} };
vi.mock('vue-router', () => ({
  useRoute: vi.fn(() => mockRoute),
}));

const mockOrdersStore: {
  orders: Array<{ id: string; courtListType: OrderCourtLisTypeEnum }>;
} = { orders: [] };
vi.mock('@/stores', async () => {
  const actual = await vi.importActual<typeof import('@/stores')>('@/stores');
  return {
    ...actual,
    useOrdersStore: vi.fn(() => mockOrdersStore),
  };
});

vi.mock('@/components/documents/DocumentUpload.vue', () => ({
  default: {
    name: 'DocumentUpload',
    props: ['disabled', 'show', 'selectedFile', 'text'],
    emits: ['update:show', 'update:selectedFile'],
    template: '<div data-testid="document-upload">' + '<slot />' + '</div>',
  },
}));

const ORDER_ID = 'order-123';

const setRouteId = (id: string | undefined) => {
  mockRoute.query = id ? { id } : {};
};

const setOrders = (
  orders: Array<{ id: string; courtListType: OrderCourtLisTypeEnum }>
) => {
  mockOrdersStore.orders = orders;
};

const createFamilyDeskOrder = () => [
  { id: ORDER_ID, courtListType: OrderCourtLisTypeEnum.PFM },
];

const createNonFamilyDeskOrder = () => [
  { id: ORDER_ID, courtListType: OrderCourtLisTypeEnum.PCS },
];

type ReviewOrderMock = ReturnType<typeof vi.fn> &
  ((review: OrderReview) => Promise<void>);

let reviewOrderMock: ReviewOrderMock;

const getReviewCalls = (
  mock: ReviewOrderMock = reviewOrderMock
): OrderReview[][] => mock.mock.calls as OrderReview[][];

describe('ReviewModal.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    setRouteId(ORDER_ID);
    setOrders(createNonFamilyDeskOrder());
    reviewOrderMock = vi.fn().mockResolvedValue(undefined) as ReviewOrderMock;
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  const createWrapper = (props = {}, modelValue = true) => {
    return mount(ReviewModal, {
      props: {
        canApprove: false,
        modelValue,
        reviewOrder: reviewOrderMock,
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
      expect(wrapper.text()).toContain(
        'Add any notes or reasoning for your decision'
      );
      expect(wrapper.text()).toContain(
        'Required for any action other than Approval'
      );
    });

    it('should render comments textarea', () => {
      const wrapper = createWrapper();
      const textarea = wrapper.find('v-textarea');
      expect(textarea.exists()).toBe(true);
    });
  });

  describe('canApprove prop behavior (non-Family Desk order)', () => {
    it('should show signature warning alert when canApprove is false', () => {
      const wrapper = createWrapper({ canApprove: false });
      expect(wrapper.text()).toContain(
        'Document signature is required before Approval'
      );
    });

    it('should not show warning alert when canApprove is true', () => {
      const wrapper = createWrapper({ canApprove: true });
      expect(wrapper.text()).not.toContain(
        'Document signature is required before Approval'
      );
    });
  });

  describe('Family Desk Order (PFM) behavior', () => {
    beforeEach(() => {
      setOrders(createFamilyDeskOrder());
    });

    it('should render the DocumentUpload component', () => {
      const wrapper = createWrapper({ canApprove: true });
      expect(wrapper.find('[data-testid="document-upload"]').exists()).toBe(
        true
      );
    });

    it('should show the upload-required warning instead of the signature warning', () => {
      const wrapper = createWrapper({ canApprove: true });
      expect(wrapper.text()).toContain(
        'Document upload is required before Approval'
      );
      expect(wrapper.text()).not.toContain(
        'Document signature is required before Approval'
      );
    });

    it('should disable Approve until a supporting document has been selected', async () => {
      const wrapper = createWrapper({ canApprove: true });
      const approveButton = wrapper.find('[color="success"]');
      expect(approveButton.attributes('disabled')).toBe('true');

      const upload = wrapper.findComponent({ name: 'DocumentUpload' });
      const fakeFile = new File(['data'], 'order.pdf', {
        type: 'application/pdf',
      });
      upload.vm.$emit('update:selectedFile', fakeFile);
      await nextTick();

      expect(wrapper.find('[color="success"]').attributes('disabled')).toBe(
        'false'
      );
    });

    it('should emit signed=false on Approve for a Family Desk order', async () => {
      const wrapper = createWrapper({ canApprove: true });
      const upload = wrapper.findComponent({ name: 'DocumentUpload' });
      const fakeFile = new File(['data'], 'order.pdf', {
        type: 'application/pdf',
      });
      upload.vm.$emit('update:selectedFile', fakeFile);
      await nextTick();

      const approveButton = wrapper.find('[color="success"]');
      await approveButton.trigger('click');
      await flushPromises();

      const calls = getReviewCalls();
      expect(calls.length).toBeGreaterThan(0);
      const review = calls[0][0];
      expect(review.status).toBe(OrderReviewStatus.Approved);
      expect(review.signed).toBe(false);
      expect(review.documentData).toBe('');
      expect(review.supportingDocumentData).not.toBe('');
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

    it('should disable Reject and Awaiting documentation when comments are empty', () => {
      const wrapper = createWrapper();
      expect(
        wrapper.find('[color="error"]').attributes('disabled')
      ).toBeDefined();
      expect(
        wrapper.find('[color="warning"]').attributes('disabled')
      ).toBeDefined();
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

    it('should emit a fully-formed review payload with signed=true on Approve for non-Family Desk orders', async () => {
      const wrapper = createWrapper({ canApprove: true });
      const approveButton = wrapper.find('[color="success"]');
      await approveButton.trigger('click');
      await flushPromises();

      const calls = getReviewCalls();
      expect(calls.length).toBeGreaterThan(0);
      const review = calls[0][0];
      expect(review.status).toBe(OrderReviewStatus.Approved);
      expect(review.signed).toBe(true);
      expect(review.documentData).toBe('');
      expect(review.supportingDocumentData).toBe('');
    });
  });

  describe('Optional file upload', () => {
    const createUploadWrapper = () =>
      mount(ReviewModal, {
        props: {
          canApprove: true,
          modelValue: true,
          reviewOrder: reviewOrderMock,
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

      const calls = getReviewCalls();
      expect(calls.length).toBeGreaterThan(0);
      expect(calls[0]?.[0]?.documentData).toBe('');
      expect(calls[0]?.[0]?.supportingDocumentData).toBe('');
    });
  });

  describe('Submitted confirmation + auto-close', () => {
    it('should swap to the "Review Submitted" view after a successful Approve', async () => {
      const wrapper = createWrapper({ canApprove: true });
      const approveButton = wrapper.find('[color="success"]');
      await approveButton.trigger('click');
      await flushPromises();

      expect(wrapper.text()).toContain('Review Submitted');
      expect(wrapper.text()).toContain('Your review has been submitted.');
      expect(wrapper.text()).toContain('Orders dashboard');
    });

    it('should display the auto-close countdown after submission', async () => {
      vi.useFakeTimers();
      const wrapper = createWrapper({ canApprove: true });

      await wrapper.find('[color="success"]').trigger('click');
      await flushPromises();

      expect(wrapper.text()).toMatch(/This tab will close in 15 seconds\./);
    });

    it('should decrement the countdown each second', async () => {
      vi.useFakeTimers();
      const wrapper = createWrapper({ canApprove: true });

      await wrapper.find('[color="success"]').trigger('click');
      await flushPromises();

      vi.advanceTimersByTime(1000);
      await nextTick();
      expect(wrapper.text()).toMatch(/This tab will close in 14 seconds\./);

      vi.advanceTimersByTime(13_000);
      await nextTick();
      expect(wrapper.text()).toMatch(/This tab will close in 1 second\./);
    });

    it('should call window.close once the countdown reaches zero', async () => {
      vi.useFakeTimers();
      const closeSpy = vi.fn();
      vi.stubGlobal('close', closeSpy);

      const wrapper = createWrapper({ canApprove: true });
      await wrapper.find('[color="success"]').trigger('click');
      await flushPromises();

      vi.advanceTimersByTime(15_000);
      await nextTick();

      expect(closeSpy).toHaveBeenCalledTimes(1);
    });

    it('should reset submitted state and clear comments when the dialog is closed', async () => {
      const wrapper = createWrapper({ canApprove: true });
      await wrapper.find('[color="success"]').trigger('click');
      await flushPromises();
      expect(wrapper.text()).toContain('Review Submitted');

      await wrapper.setProps({ modelValue: false });
      await wrapper.setProps({ modelValue: true });
      await flushPromises();

      expect(wrapper.text()).not.toContain('Review Submitted');
      expect(wrapper.text()).toContain('Review Order');
    });
  });

  describe('Reject and Awaiting documentation submissions', () => {
    const createCommentsWrapper = (canApprove = false) =>
      mount(ReviewModal, {
        props: {
          canApprove,
          modelValue: true,
          reviewOrder: reviewOrderMock,
        },
        global: {
          stubs: {
            'v-textarea': {
              props: ['modelValue'],
              emits: ['update:modelValue'],
              template:
                '<textarea :value="modelValue" ' +
                '@input="$emit(\'update:modelValue\', $event.target.value)" />',
            },
          },
        },
      });

    const writeComment = async (
      wrapper: ReturnType<typeof createCommentsWrapper>,
      text: string
    ) => {
      const textarea = wrapper.find('textarea');
      await textarea.setValue(text);
    };

    it('should emit a Rejected review when the Reject button is clicked', async () => {
      const wrapper = createCommentsWrapper();
      await writeComment(wrapper, 'Needs work');

      const rejectButton = wrapper.find('[color="error"]');
      expect(rejectButton.attributes('disabled')).toBe('false');
      await rejectButton.trigger('click');
      await flushPromises();

      const calls = getReviewCalls();
      expect(calls.length).toBeGreaterThan(0);
      expect(calls[0][0].status).toBe(OrderReviewStatus.Unapproved);
      expect(calls[0][0].signed).toBe(false);
      expect(calls[0][0].comments).toBe('Needs work');
    });

    it('should emit an AwaitingDocumentation review when that button is clicked', async () => {
      const wrapper = createCommentsWrapper();
      await writeComment(wrapper, 'Awaiting evidence');

      const pendingButton = wrapper.find('[color="warning"]');
      expect(pendingButton.attributes('disabled')).toBe('false');
      await pendingButton.trigger('click');
      await flushPromises();

      const calls = getReviewCalls();
      expect(calls.length).toBeGreaterThan(0);
      expect(calls[0][0].status).toBe(OrderReviewStatus.AwaitingDocumentation);
      expect(calls[0][0].signed).toBe(false);
      expect(calls[0][0].comments).toBe('Awaiting evidence');
    });
  });
});
