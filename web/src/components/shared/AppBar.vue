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
      <v-tab value="orders" to="/orders" v-if="showOrders">
        <v-badge
          v-if="priorityPendingOrdersCount > 0 && regularPendingOrdersCount > 0"
          data-testid="order-combo-badge"
          :class="{
            'badge-pulse': badgePulseActive,
            'order-combo-badge': true,
          }"
          offset-x="0"
          offset-y="-10"
        >
          <template #badge>
            <span
              class="capsule-half priority"
              :title="`${priorityPendingOrdersCount} priority orders pending`"
            >
              {{ priorityPendingOrdersCount }}
            </span>
            <span
              class="capsule-half regular"
              :title="`${regularPendingOrdersCount} regular orders pending`"
            >
              {{ regularPendingOrdersCount }}
            </span>
          </template>
          For Signing
        </v-badge>
        <v-badge
          v-else-if="priorityPendingOrdersCount > 0"
          data-testid="priority-badge"
          :content="priorityPendingOrdersCount"
          :class="{ 'badge-pulse': badgePulseActive }"
          color="var(--bg-red-500)"
          :title="`${priorityPendingOrdersCount} priority orders pending`"
          offset-x="-10"
          offset-y="-10"
        >
          For Signing
        </v-badge>
        <v-badge
          v-else-if="regularPendingOrdersCount > 0"
          data-testid="regular-badge"
          :content="regularPendingOrdersCount"
          :class="{ 'badge-pulse': badgePulseActive }"
          color="var(--bg-green-500)"
          :title="`${regularPendingOrdersCount} regular orders pending`"
          offset-x="-10"
          offset-y="-10"
        >
          For Signing
        </v-badge>
        <template v-else>For Signing</template>
      </v-tab>
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
  import { JudgeService, OrderService } from '@/services';
  import { NotificationsService } from '@/signalr/notifications';
  import { useCommonStore } from '@/stores';
  import { useDarsStore } from '@/stores/DarsStore';
  import { useNotificationsStore } from '@/stores/NotificationsStore';
  import { useOrdersStore } from '@/stores/OrdersStore';
  import { PersonSearchItem } from '@/types';
  import {
    OrderPriorityEnum,
    OrderReviewStatus,
    RolesEnum,
  } from '@/types/common';
  import { mdiAccountCircle } from '@mdi/js';
  import { computed, inject, nextTick, onMounted, ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import JudgeSelector from './JudgeSelector.vue';

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
  const priorityOrderTypes = new Set<string>(Object.values(OrderPriorityEnum));

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

  const priorityPendingOrdersCount = computed(
    () =>
      ordersStore.orders.filter(
        (o) =>
          o.status === OrderReviewStatus.Pending &&
          priorityOrderTypes.has(o.priorityType)
      ).length
  );

  const regularPendingOrdersCount = computed(
    () =>
      ordersStore.orders.filter(
        (o) =>
          o.status === OrderReviewStatus.Pending &&
          !priorityOrderTypes.has(o.priorityType)
      ).length
  );

  const badgePulseActive = ref(false);

  const triggerBadgePulse = async () => {
    if (
      priorityPendingOrdersCount.value === 0 ||
      regularPendingOrdersCount.value === 0
    ) {
      return;
    }

    badgePulseActive.value = false;
    await nextTick();
    badgePulseActive.value = true;
    globalThis.setTimeout(() => {
      badgePulseActive.value = false;
    }, 1200);
  };

  watch(
    () => ordersStore.lastFetched,
    () => {
      void triggerBadgePulse();
    }
  );

  watch([priorityPendingOrdersCount, regularPendingOrdersCount], () => {
    void triggerBadgePulse();
  });
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

  .badge-pulse :deep(.v-badge__badge) {
    animation: badge-pop 0.35s ease-out;
  }

  .order-combo-badge :deep(.v-badge__badge) {
    padding: 0;
    min-width: auto;
    height: 18px;
    overflow: hidden;
    background: transparent;
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 20px;
    height: 100%;
    padding: 0 6px;
    font-size: 0.75rem;
    line-height: 1;
    color: var(--bg-white-500);
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half.priority {
    background-color: var(--bg-red-500);
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half.regular {
    background-color: var(--bg-green-500);
  }

  @keyframes badge-pop {
    0% {
      transform: scale(1);
    }
    45% {
      transform: scale(1.2);
    }
    100% {
      transform: scale(1);
    }
  }
</style>
