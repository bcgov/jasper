<template>
  <v-theme-provider :theme="theme">
    <v-app>
      <profile-off-canvas v-model="profile" />
      <v-app-bar app>
        <v-app-bar-title class="mr-4">
          <router-link to="/">
            <img class="logo" :src="logo" alt="logo" width="63" />
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
            <JudgeSelector
              v-if="
                (selectedTab === 'dashboard' || selectedTab === 'court-list') &&
                judges &&
                judges?.length > 0
              "
              :judges="judges"
            />
            <v-btn
              spaced="end"
              size="x-large"
              @click.stop="profile = true"
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
      <v-main>
        <router-view />
      </v-main>
      <DarsAccessModal v-model="darsStore.isModalVisible" />
      <snackbar />
    </v-app>
  </v-theme-provider>
</template>

<script setup lang="ts">
  import logo from '@/assets/jasper-logo.svg?url';
  import { JudgeService, OrderService } from '@/services';
  import { useCommonStore } from '@/stores';
  import { PersonSearchItem } from '@/types';
  import { OrderReviewStatus, RolesEnum } from '@/types/common';
  import { mdiAccountCircle } from '@mdi/js';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import DarsAccessModal from './components/dashboard/DarsAccessModal.vue';
  import JudgeSelector from './components/shared/JudgeSelector.vue';
  import ProfileOffCanvas from './components/shared/ProfileOffCanvas.vue';
  import Snackbar from './components/shared/Snackbar.vue';
  import { useDarsStore } from './stores/DarsStore';
  import { useOrdersStore } from './stores/OrdersStore';
  import { useThemeStore } from './stores/ThemeStore';

  const themeStore = useThemeStore();
  const commonStore = useCommonStore();
  const darsStore = useDarsStore();
  const ordersStore = useOrdersStore();
  const theme = ref(themeStore.state);
  const profile = ref(false);

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
      ordersStore.fetchOrders(orderService),
    ]);
    judges.value = judgesData ?? [];
  });

  watch(
    () => route.path,
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

  const pendingOrdersCount = computed(
    () =>
      ordersStore.orders.filter((o) => o.status === OrderReviewStatus.Pending)
        .length
  );
</script>

<style>
  .v-tabs {
    flex: 10;
  }

  .v-toolbar-title {
    flex: none;
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
</style>

<style scoped>
  .underline-on-hover:hover :deep(.v-btn__content) {
    text-decoration: underline;
    text-underline-offset: 2px;
  }
</style>
