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
  import { UserService } from '@/services/UserService';
  import { PersonSearchItem } from '@/types';
  import { inject, onMounted, ref, watch } from 'vue';
  import { useCommonStore } from '@/stores';
  import { UserInfo } from '@/types/common';

  const judges = ref<PersonSearchItem[]>();
  const userService = inject<UserService>('userService');
  const isLoading = ref(true);
  const commonStore = useCommonStore();
  const selectedJudgeId = ref(null);
  const isDataLoaded = ref(false);

  onMounted(async () => {
    isLoading.value = true;
    const result = await userService?.getJudges();
    judges.value = result;
    selectedJudgeId.value = commonStore.userInfo?.judgeId;
    isLoading.value = false;
  });

  watch(selectedJudgeId, async (newVal: number) => {
    if (!newVal) {
      return;
    }

    if (newVal != commonStore.userInfo.judgeId) {
      commonStore.setUserInfo({ ...commonStore.userInfo, judgeId: newVal });
    }
  });

  watch(
    () => commonStore.userInfo,
    (newUserInfo: UserInfo) => {
      if (!newUserInfo) {
        return;
      }
      selectedJudgeId.value = newUserInfo.judgeId;
    }
  );
</script>
