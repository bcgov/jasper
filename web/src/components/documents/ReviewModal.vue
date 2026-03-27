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
        <!-- Comments Section -->
        <div class="mb-6">
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
          />
        </div>

        <DocumentUpload
          v-model:show="show"
          v-model:selectedFile="selectedUpload"
          :disabled="props.canApprove"
        />
      </v-card-text>

      <v-card-text>
        <v-alert
          v-if="!canApprove"
          type="warning"
          variant="tonal"
          density="comfortable"
        >
          Document signature or upload is required before Approval.
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
  import { ref, computed, watch } from 'vue';
  import {
    mdiClose,
    mdiCheckBold,
    mdiPencilBoxOutline,
    mdiAccountClock,
  } from '@mdi/js';
  import { OrderReviewStatus } from '@/types/common';
  import { OrderReview } from '@/types';
  import { arrayBufferToBase64 } from '@/utils/utils';
  import DocumentUpload from './DocumentUpload.vue';

  const props = defineProps<{
    canApprove: boolean;
  }>();

  const emit = defineEmits<(e: 'reviewOrder', review: OrderReview) => void>();
  const show = defineModel<boolean>({ type: Boolean, required: true });

  const comments = ref<string>('');
  const selectedUpload = ref<File | null>(null);
  const canReject = computed<boolean>(() => comments.value?.length > 0);
  const canApprove = computed<boolean>(
    () => props.canApprove || selectedUpload.value !== null
  );

  watch(show, (newVal) => {
    if (!newVal) {
      selectedUpload.value = null;
    }
  });

  const reviewOrder = async (status: OrderReviewStatus) => {
    let documentData = '';
    if (selectedUpload.value) {
      const arrayBuffer = await selectedUpload.value.arrayBuffer();
      documentData = arrayBufferToBase64(arrayBuffer);
    }

    const review: OrderReview = {
      comments: comments.value,
      status: status,
      signed: status === OrderReviewStatus.Approved,
      documentData,
      userProvidedDocument: selectedUpload.value !== null,
    };
    emit('reviewOrder', review);

    show.value = false;
  };
</script>
