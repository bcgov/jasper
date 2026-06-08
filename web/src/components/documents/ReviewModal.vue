<template>
  <v-dialog v-model="show" persistent max-width="750">
    <v-card>
      <!-- Header -->
      <v-card-title class="d-flex align-center">
        <v-icon class="me-2" :icon="mdiPencilBoxOutline" />
        Review Order
        <v-spacer />
        <v-btn
          icon
          variant="text"
          density="comfortable"
          @click="show = false"
          aria-label="Close dialog"
        >
          <v-icon :icon="mdiClose" />
        </v-btn>
      </v-card-title>
      <v-divider class="my-0" />

      <!-- Body -->
      <v-card-text class="py-0">
        <!-- Comments Section -->
        <div>
          <p class="text-body-2 text-medium-emphasis mb-3">
            Add any notes or reasoning for your decision. These comments will be
            saved with the order. <br />
            <strong>Required</strong> for any action other than Approval.
          </p>

          <v-textarea
            ref="commentsRef"
            v-model="comments"
            label="Review comments"
            rows="4"
            auto-grow
            clearable
            variant="outlined"
            :hide-details="true"
          />
        </div>

        <DocumentUpload
          class="mt-3"
          v-if="isFamilyDeskOrder"
          :disabled="!isFamilyDeskOrder"
          v-model:show="show"
          v-model:selectedFile="selectedUpload"
          text="Attach Desk Order"
        />
      </v-card-text>

      <v-card-text>
        <v-alert
          v-if="!canApprove"
          type="warning"
          variant="tonal"
          density="comfortable"
        >
          {{
            isFamilyDeskOrder
              ? 'Document upload is required before Approval.'
              : 'Document signature is required before Approval.'
          }}
        </v-alert>
      </v-card-text>
      <!-- Actions -->
      <v-card-actions class="px-6 py-4">
        <!-- Left (destructive / secondary) -->
        <div class="d-flex ga-2">
          <v-btn
            color="error"
            variant="text"
            :prepend-icon="mdiClose"
            :disabled="!canReject"
            @click="reviewOrder(OrderReviewStatus.Unapproved)"
          >
            Reject
          </v-btn>

          <v-btn
            color="warning"
            variant="outlined"
            :prepend-icon="mdiAccountClock"
            :disabled="!canReject"
            @click="reviewOrder(OrderReviewStatus.AwaitingDocumentation)"
          >
            Awaiting documentation
          </v-btn>
        </div>

        <v-spacer />

        <!-- Primary -->
        <v-btn
          color="success"
          size="large"
          :prepend-icon="mdiCheckBold"
          :disabled="!canApprove"
          @click="reviewOrder(OrderReviewStatus.Approved)"
        >
          Approve
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { useOrdersStore } from '@/stores';
  import { OrderReview } from '@/types';
  import { OrderCourtLisTypeEnum, OrderReviewStatus } from '@/types/common';
  import { arrayBufferToBase64 } from '@/utils/utils';
  import {
    mdiAccountClock,
    mdiCheckBold,
    mdiClose,
    mdiPencilBoxOutline,
  } from '@mdi/js';
  import { computed, ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import DocumentUpload from './DocumentUpload.vue';

  const props = defineProps<{
    canApprove: boolean;
  }>();

  const emit = defineEmits<(e: 'reviewOrder', review: OrderReview) => void>();
  const show = defineModel<boolean>({ type: Boolean, required: true });

  const comments = ref<string>('');
  const selectedUpload = ref<File | null>(null);
  const canReject = computed<boolean>(() => comments.value?.length > 0);
  const canApprove = computed<boolean>(() =>
    isFamilyDeskOrder.value
      ? selectedUpload.value !== null
      : props.canApprove || selectedUpload.value !== null
  );
  const orderStore = useOrdersStore();
  const route = useRoute();
  const orderId = computed(() => (route.query.id as string) ?? null);
  const currentOrder = computed(() =>
    orderId.value
      ? orderStore.orders.find((o) => o.id === orderId.value)
      : undefined
  );

  const isFamilyDeskOrder = computed(
    () => currentOrder.value?.courtListType === OrderCourtLisTypeEnum.PFM
  );

  watch(show, (newVal) => {
    if (!newVal) {
      selectedUpload.value = null;
    }
  });

  const reviewOrder = async (status: OrderReviewStatus) => {
    const documentData = '';
    let supportingDocumentData = '';
    if (selectedUpload.value) {
      const arrayBuffer = await selectedUpload.value.arrayBuffer();
      supportingDocumentData = arrayBufferToBase64(arrayBuffer);
    }

    const review: OrderReview = {
      comments: comments.value,
      status: status,
      signed: status === OrderReviewStatus.Approved && !isFamilyDeskOrder.value, // Approved Family Desk Orders doesn't require signature.
      documentData,
      supportingDocumentData,
    };

    emit('reviewOrder', review);

    show.value = false;
  };
</script>
