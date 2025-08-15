<template>
  <div class="d-flex align-center" v-if="judges && judges.length > 0">
    <span class="mr-2">You are viewing data for:</span>
    <v-select
      v-model="selectedJudgeId"
      :items="judges"
      item-title="fullName"
      item-value="personId"
      density="compact"
      hide-details
      :loading="isLoading"
      :disabled="isLoading"
      style="min-width: 200px"
      label="Select a Judge"
      clearable
      :return-object="false"
    ></v-select>
  </div>
</template>
<script setup lang="ts">
  import { DashboardService } from '@/services';
  import { useCommonStore } from '@/stores';
  import { PersonSearchItem } from '@/types';
  import { UserInfo } from '@/types/common';
  import { inject, onMounted, ref, watch } from 'vue';

  const judges = ref<PersonSearchItem[]>();
  const userService = inject<DashboardService>('dashboardService');
  const isLoading = ref(true);
  const commonStore = useCommonStore();
  const selectedJudgeId = ref<number | null>(null);
  const isDataLoaded = ref(false);

  onMounted(async () => {
    isLoading.value = true;
    const result = await userService?.getJudges();
    judges.value = result;
    selectedJudgeId.value = commonStore.userInfo?.judgeId ?? null;
    isLoading.value = false;
  });

  watch(selectedJudgeId, (newVal) => {
    if (!newVal) {
      return;
    }

    if (newVal != commonStore.userInfo?.judgeId) {
      commonStore.setUserInfo({ ...commonStore.userInfo, judgeId: newVal });
    }
  });

  watch(
    () => commonStore.userInfo,
    (newUserInfo: UserInfo | null) => {
      if (!newUserInfo) {
        return;
      }
      selectedJudgeId.value = newUserInfo.judgeId;
    }
  );
</script>
