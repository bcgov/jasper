<template>
  <v-skeleton-loader
    v-if="ordersStore.isLoading"
    :loading="ordersStore.isLoading"
    type="table"
  ></v-skeleton-loader>
  <div class="my-4 mx-2" v-else>
    <v-expansion-panels
      class="mb-3"
      bg-color="var(--bg-gray-500)"
      :flat="true"
      multiple
      :model-value="[0]"
    >
      <v-expansion-panel>
        <v-expansion-panel-title class="px-3">
          <h5 class="m-0">
            For signing
            {{ pendingOrders.length > 0 ? `(${pendingOrders.length})` : '' }}
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <OrdersDataTable
            :data="pendingOrders"
            :viewCaseDetails="viewCaseDetails"
            :viewOrderDetails="viewOrderDetails"
            :columns="[
              'packageId',
              'receivedDate',
              'division',
              'fileNumber',
              'styleOfCause',
            ]"
          />
        </v-expansion-panel-text>
      </v-expansion-panel>
      <v-expansion-panel collapsed>
        <v-expansion-panel-title class="px-3">
          <h5 class="m-0">Completed</h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <OrdersDataTable
            :data="completedOrders"
            :viewCaseDetails="viewCaseDetails"
            :viewOrderDetails="viewOrderDetails"
            :columns="[
              'packageId',
              'receivedDate',
              'processedDate',
              'division',
              'fileNumber',
              'styleOfCause',
            ]"
            :sortBy="[{ key: 'processedDate', order: 'desc' }]"
          />
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>
<script lang="ts" setup>
  import shared from '@/components/shared';
  import { OrderService } from '@/services';
  import {
    useCommonStore,
    useCourtFileSearchStore,
    useOrdersStore,
  } from '@/stores';
  import { Order } from '@/types';
  import { KeyValueInfo, OrderReviewStatus } from '@/types/common';
  import { DocumentData } from '@/types/shared';
  import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';
  import { computed, inject, ref, watch } from 'vue';

  const ordersStore = useOrdersStore();
  const courtFileSearchStore = useCourtFileSearchStore();
  const commonStore = useCommonStore();
  const judgeId = ref(commonStore.userInfo?.judgeId);
  const orderService = inject<OrderService>('orderService');

  if (!orderService) {
    throw new Error('Service is not available!');
  }

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal) => {
      judgeId.value = newVal;
      await ordersStore.fetchOrders(orderService, judgeId.value);
    }
  );

  const pendingOrders = computed(
    () =>
      ordersStore?.orders?.filter(
        (order) => order.status === OrderReviewStatus.Pending
      ) ?? []
  );

  const completedOrders = computed(
    () =>
      ordersStore?.orders?.filter(
        (order) => order.status === OrderReviewStatus.Approved
      ) ?? []
  );

  const viewCaseDetails = (item: Order) => {
    const courtClassLabel = getCourtClassLabel(item.courtClass);
    const isCriminal = isCourtClassLabelCriminal(courtClassLabel);

    const caseDetailUrl = `/${isCriminal ? 'criminal-file' : 'civil-file'}/${item.physicalFileId}`;

    const files: KeyValueInfo[] = [
      {
        key: item.physicalFileId,
        value: item.courtFileNumber,
      },
    ];
    courtFileSearchStore.addFilesForViewing({
      searchCriteria: {},
      searchResults: [],
      files,
    });

    window.open(caseDetailUrl, '_blank');
  };
  const viewOrderDetails = (order: Order) => {
    const courtClassLabel = getCourtClassLabel(order.courtClass);
    const isCriminal = isCourtClassLabelCriminal(courtClassLabel);
    const documentData: DocumentData = {
      courtClass: order.courtClass,
      fileId: order.physicalFileId,
      fileNumberText: order.courtFileNumber,
      documentId: order.packageDocumentId,
      documentDescription: order.packageName,
      isCriminal,
      orderId: order.id,
    };

    shared.openOrderDocuments(documentData);
  };
</script>
<style scoped>
  :deep(.v-expansion-panel) {
    margin-top: 0;
    margin-bottom: 1rem;
  }

  :deep(.v-expansion-panel-title) {
    min-height: 48px !important;
  }

  .v-expansion-panel-text {
    background-color: var(--bg-white-500) !important;
    max-height: 400px;
    overflow-y: auto;
  }

  :deep(.v-expansion-panel-text__wrapper) {
    padding: 0 !important;
  }
</style>
