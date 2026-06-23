<template>
  <v-app-bar app>
    <v-app-bar-title class="mr-4">
      <router-link data-testid="router-link" to="/">
        <img
          data-testid="logo"
          class="logo"
          :src="logo"
          alt="logo"
          width="63"
        />
      </router-link>
    </v-app-bar-title>
    <v-tabs align-tabs="start" v-model="selectedTab" text-color="#000">
      <v-tab value="dashboard" to="/dashboard">Dashboard</v-tab>
      <v-tab value="court-list" to="/court-list">Court list</v-tab>
      <v-tab value="court-file-search" to="/court-file-search">
        Court file search
      </v-tab>
      <v-btn
        class="v-tab underline-on-hover"
        value="dars"
        @click="darsStore.openModal()"
        :rounded="false"
        >DARS</v-btn
      >
      <OrdersTab
        v-if="showOrders"
        value="orders"
        title="For Signing"
        label="orders"
        :priority-count="priorityPendingOrdersCount"
        :regular-count="regularPendingOrdersCount"
        :pulse-active="badgePulseActive"
      />
      <OrdersTab
        v-if="showOrders"
        value="desk-orders"
        title="Applications"
        label="desk orders"
        :priority-count="priorityPendingDeskOrdersCount"
        :regular-count="regularPendingDeskOrdersCount"
        :pulse-active="badgePulseActive"
      />
      <v-spacer></v-spacer>
      <div class="d-flex align-center">
        <JudgeSelector
          data-testid="judge-selector"
          v-if="showJudgeSelector"
          :judges="judges"
        />
        <v-btn
          spaced="end"
          size="x-large"
          @click.stop="emit('open-profile')"
          class="text-subtitle-1"
        >
          <span class="text-left">
            <div
              class="mb-1"
              :class="
                hasUnviewedReleaseNotes
                  ? 'font-weight-bold release-notes-emphasis'
                  : ''
              "
            >
              {{ userName }}
            </div>
          </span>
          <template #append>
            <v-icon :icon="mdiAccountCircle" size="32" />
          </template>
        </v-btn>
      </div>
    </v-tabs>
  </v-app-bar>
</template>

<script setup lang="ts">
  import logo from '@/assets/jasper-logo.svg?url';
  import { useOrderBadgePulse } from '@/composables/useOrderBadgePulse.js';
  import { isDeskOrder, useOrderCounts } from '@/composables/useOrderCounts';
  import { JudgeService, OrderService } from '@/services';
  import { NotificationsService } from '@/signalr/notifications';
  import { useCommonStore } from '@/stores';
  import { useDarsStore } from '@/stores/DarsStore';
  import { useNotificationsStore } from '@/stores/NotificationsStore';
  import { useOrdersStore } from '@/stores/OrdersStore';
  import { PersonSearchItem } from '@/types';
  import { RolesEnum } from '@/types/common';
  import { mdiAccountCircle } from '@mdi/js';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import JudgeSelector from './JudgeSelector.vue';
  import OrdersTab from '../orders/OrdersTab.vue';

  const emit = defineEmits<(e: 'open-profile') => void>();

  const commonStore = useCommonStore();
  const darsStore = useDarsStore();
  const ordersStore = useOrdersStore();
  const notificationsStore = useNotificationsStore();

  const route = useRoute();
  const selectedTab = ref('/dashboard');
  const orderService = inject<OrderService>('orderService');
  const notificationsService = inject<NotificationsService>(
    'notificationsService'
  );
  const judgeService = inject<JudgeService>('judgeService');
  const judges = ref<PersonSearchItem[]>([]);
  // Only users with Admin role can see Orders tab for now.
  const requiredOrderRoles = [RolesEnum.Admin] as const;

  if (!judgeService || !orderService || !notificationsService) {
    throw new Error('Service is not available!');
  }

  // Create a reactive reference to judgeId for the orders store
  const judgeId = computed(() => commonStore.userInfo?.judgeId ?? null);

  onMounted(async () => {
    // Fetch judges
    const judgesData = await judgeService?.getJudges();
    judges.value = judgesData ?? [];
  });

  watch(
    () => route.fullPath,
    (newPath) => {
      if (
        newPath.startsWith('/civil-file') ||
        newPath.startsWith('/criminal-file')
      ) {
        selectedTab.value = 'court-file-search';
      } else {
        selectedTab.value = newPath;
      }
    }
  );

  const userName = computed(() => commonStore.currentUserTitle);

  const hasUnviewedReleaseNotes = computed(
    () => commonStore.hasUnviewedReleaseNotes
  );

  const showOrders = computed(
    () =>
      requiredOrderRoles.every((requiredRole) =>
        commonStore.userInfo?.roles?.includes(requiredRole)
      ) ?? false
  );

  const canInitializeNotifications = computed(
    () => commonStore.isInitialized && !!commonStore.userInfo
  );

  const canInitializeOrderFeatures = computed(
    () => canInitializeNotifications.value && showOrders.value
  );

  // Initialize notifications once app initialization and auth are ready.
  watch(
    canInitializeNotifications,
    (canInitialize) => {
      if (canInitialize) {
        void notificationsStore
          .initialize(notificationsService, orderService)
          .catch((error) => {
            console.error('Failed to initialize notifications.', error);
          });
      }
    },
    { immediate: true }
  );

  // Initialize orders store only when user has permission to view orders.
  watch(
    canInitializeOrderFeatures,
    (canInitialize) => {
      if (canInitialize) {
        ordersStore.initialize(orderService, judgeId);
      }
    },
    { immediate: true }
  );

  const showJudgeSelector = computed(
    () =>
      (selectedTab.value === 'dashboard' ||
        selectedTab.value === 'court-list' ||
        selectedTab.value === 'orders') &&
      judges.value &&
      judges.value.length > 0
  );

  // Pending order counts, split by desk vs. non-desk and priority vs. regular.
  const orderCounts = useOrderCounts(
    () => ordersStore.orders,
    (o) => !isDeskOrder(o)
  );
  const deskOrderCounts = useOrderCounts(() => ordersStore.orders, isDeskOrder);

  // Names retained for backward compatibility with existing tests/templates.
  const priorityPendingOrdersCount = orderCounts.priority;
  const regularPendingOrdersCount = orderCounts.regular;
  const priorityPendingDeskOrdersCount = deskOrderCounts.priority;
  const regularPendingDeskOrdersCount = deskOrderCounts.regular;

  const { active: badgePulseActive } = useOrderBadgePulse(
    [orderCounts, deskOrderCounts],
    [() => ordersStore.lastFetched]
  );
</script>

<style scoped>
  :deep(.v-tab) {
    color: #000 !important;
  }

  :deep(.v-tab--selected) {
    color: #000 !important;
  }

  .logo {
    transition:
      transform 0.3s ease,
      filter 0.3s ease;
  }

  .logo:hover {
    transform: scale(1.02);
    filter: brightness(1.1);
  }

  .underline-on-hover:hover :deep(.v-btn__content) {
    text-decoration: underline;
    text-underline-offset: 2px;
  }

  .release-notes-emphasis {
    text-decoration: underline;
    text-underline-offset: 2px;
  }
</style>
