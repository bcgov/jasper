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
      <v-divider />

      <!-- Body -->
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis">
          Add any notes or reasoning for your decision. These comments will be
          saved with the order. <br />
          Note: Comments are required for any action other than Approval.
        </p>

        <v-textarea
          ref="commentsRef"
          v-model="comments"
          label="Review comments"
          rows="4"
          auto-grow
          clearable
          variant="outlined"
        />
        <v-alert
          v-if="!canApprove"
          type="warning"
          variant="tonal"
          density="comfortable"
          class="mx-6 mt-2"
        >
          Document signature is required before Approval.
        </v-alert>
      </v-card-text>

      <v-divider />

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
            @click="reviewOrder(OrderReviewStatus.Pending)"
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
  import { ref, computed } from 'vue';
  import {
    mdiClose,
    mdiCheckBold,
    mdiPencilBoxOutline,
    mdiAccountClock,
  } from '@mdi/js';
  import { OrderReviewStatus } from '@/types/common';
  import { OrderReview } from '@/types';

  defineProps<{
    canApprove: boolean;
  }>();

  const emit = defineEmits<(e: 'reviewOrder', review: OrderReview) => void>();
  const show = defineModel<boolean>({ type: Boolean, required: true });

  const comments = ref<string>('');
  const canReject = computed<boolean>(() => comments.value?.length > 0);

  const reviewOrder = (status: OrderReviewStatus) => {
    const review: OrderReview = {
      comments: comments.value,
      status: status,
      signed: status === OrderReviewStatus.Approved,
      documentData: null
    };
    emit('reviewOrder', review);

    show.value = false;
  };
</script>
