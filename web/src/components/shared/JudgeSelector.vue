<template>
  <div class="d-flex align-center">
    <v-autocomplete
      ref="autocompleteRef"
      v-model="selectedJudgeId"
      v-model:menu="menuOpen"
      item-title="fullName"
      item-value="personId"
      :items="judges"
      density="compact"
      hide-details
      style="min-width: 375px"
      label="Viewing as"
      variant="outlined"
      clearable
      auto-select-first
      validate-on="blur eager"
    >
      <template #clear>
        <v-icon
          v-if="selectedJudgeId !== commonStore.loggedInUserInfo?.judgeId"
          :icon="mdiRestore"
          @click.stop="resetToOriginalJudge"
          class="cursor-pointer mr-1"
        ></v-icon>
      </template>
    </v-autocomplete>
  </div>
</template>
<script setup lang="ts">
  import { useCommonStore } from '@/stores';
  import { PersonSearchItem } from '@/types';
  import { UserInfo } from '@/types/common';
  import { ref, watch } from 'vue';
  import { mdiRestore } from '@mdi/js';

  const props = defineProps<{
    judges: PersonSearchItem[];
  }>();

  const commonStore = useCommonStore();
  const autocompleteRef = ref<any>(null);
  const selectedJudgeId = ref<number | null>(
    commonStore.userInfo?.judgeId ?? null
  );
  const menuOpen = ref(false);
  const resetToOriginalJudge = () => {
    menuOpen.value = false;
    selectedJudgeId.value = commonStore.loggedInUserInfo?.judgeId ?? null;
    autocompleteRef.value?.blur();
  };

  watch(selectedJudgeId, (newVal) => {
    if (!newVal) {
      //selectedJudgeId.value = commonStore.loggedInUserInfo?.judgeId ?? null;
      return;
    }

    if (newVal != commonStore.userInfo?.judgeId) {
      const { homeLocationId } = props.judges.find(
        (j) => j.personId === newVal
      );
      console.log('Updating judge to', newVal);
      commonStore.setUserInfo({
        ...commonStore.userInfo,
        judgeId: newVal,
        judgeHomeLocationId: homeLocationId,
      });
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
