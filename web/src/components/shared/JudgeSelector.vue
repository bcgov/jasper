<template>
  <div class="d-flex align-center">
    <span class="mr-2">You are viewing data for:</span>
    <v-select
      v-model="selectedJudgeId"
      :items="judges"
      item-title="fullName"
      item-value="personId"
      density="compact"
      hide-details
      style="min-width: 200px"
      label="Select a Judge"
      clearable
      :return-object="false"
    ></v-select>
  </div>
</template>
<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { PersonSearchItem } from '@/types';
  import { UserInfo } from '@/types/common';
  import { defineProps, ref, watch } from 'vue';

  defineProps<{
    judges: PersonSearchItem[];
  }>();

  const commonStore = useCommonStore();
  const selectedJudgeId = ref<number | null>(
    commonStore.userInfo?.judgeId ?? null
  );

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
