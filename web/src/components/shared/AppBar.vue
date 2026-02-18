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
    <v-tabs align-tabs="start" v-model="selectedTab">
      <v-tab value="dashboard" to="/dashboard">Dashboard</v-tab>
      <v-tab value="court-list" to="/court-list">Court list</v-tab>
      <v-tab value="court-file-search" to="/court-file-search"
        >Court file search</v-tab
      >
      <v-btn
        class="v-tab underline-on-hover"
        value="dars"
        @click="darsStore.openModal()"
        >DARS</v-btn
      >
      <v-tab value="orders" to="/orders" v-if="showOrders">
        <v-badge
          data-testid="order-badge"
          v-if="pendingOrdersCount > 0"
          :content="pendingOrdersCount"
          color="error"
          offset-x="-10"
          offset-y="-10"
        >
          For Signing
        </v-badge>
        <template v-else>For Signing</template>
      </v-tab>
      <v-spacer></v-spacer>
      <div class="d-flex align-center">
        <JudgeSelector v-if="showJudgeSelector" :judges="judges" />
        <v-btn
          spaced="end"
          size="x-large"
          @click.stop="emit('open-profile')"
          class="text-subtitle-1"
        >
          <span class="text-left">
            <div class="mb-1">{{ userName }}</div>
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
  import { useCommonStore } from '@/stores';
  import { useDarsStore } from '@/stores/DarsStore';
  import { useOrdersStore } from '@/stores/OrdersStore';
  import { PersonSearchItem } from '@/types';
  import { OrderReviewStatus, RolesEnum } from '@/types/common';
  import { mdiAccountCircle } from '@mdi/js';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import JudgeSelector from './JudgeSelector.vue';

  const emit = defineEmits<{
    (e: 'open-profile'): void;
  }>();

  const commonStore = useCommonStore();
  const darsStore = useDarsStore();
  const ordersStore = useOrdersStore();

  const route = useRoute();
  const selectedTab = ref('/dashboard');
  const orderService = inject<OrderService>('orderService');
  const judgeService = inject<JudgeService>('judgeService');
  const judges = ref<PersonSearchItem[]>([]);

  if (!judgeService || !orderService) {
    throw new Error('Service is not available!');
  }

  onMounted(async () => {
    const [judgesData] = await Promise.all([
      judgeService?.getJudges(),
      ordersStore.fetchOrders(
        orderService,
        commonStore.userInfo?.judgeId ?? null
      ),
    ]);
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

  const userName = computed(() => commonStore.userInfo?.userTitle || '');
  // Only users with Admin role can see Orders tab for now.
  const requiredOrderRoles = [RolesEnum.Admin] as const;
  const showOrders = computed(
    () =>
      requiredOrderRoles.every((requiredRole) =>
        commonStore.userInfo?.roles?.includes(requiredRole)
      ) ?? false
  );

  const showJudgeSelector = computed(
    () =>
      (selectedTab.value === 'dashboard' ||
        selectedTab.value === 'court-list' ||
        selectedTab.value === 'orders') &&
      judges.value &&
      judges.value.length > 0
  );

  const pendingOrdersCount = computed(
    () =>
      ordersStore.orders.filter((o) => o.status === OrderReviewStatus.Pending)
        .length
  );
</script>

<style scoped>
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
</style>
