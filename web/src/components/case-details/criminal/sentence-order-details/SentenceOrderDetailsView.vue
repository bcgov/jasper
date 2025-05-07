<template>
  <v-card color="var(--bg-gray)" elevation="0">
    <v-card-text class="text-h5"> Sentence/Order Details </v-card-text>
  </v-card>
  <h5 class="my-3 font-weight-bold">Counts ({{ count }})</h5>
  <div class="d-flex align-center justify-space-between mb-3">
    <div class="d-flex">
      <v-btn-secondary class="me-3" @click="openDialogClick"
        >Order Made Details</v-btn-secondary
      >
      <v-btn-secondary @click="() => openDialogClick(false)"
        >Judge's Recommendations</v-btn-secondary
      >
    </div>
    <NameFilter
      class="accused"
      v-model="selectedAccused"
      :people="participants"
    />
  </div>
  <SentenceOrderDetailsTable
    class="mb-5"
    :participants="filteredParticipants"
  />
  <SentenceOrderDetailsDialog
    v-model="showDialog"
    :title="dialogTitle"
    :subtitle="dialogSubtitle"
    :targetProperty="dialogTargetProperty"
    :participants
  />
</template>
<script setup lang="ts">
  import NameFilter from '@/components/shared/Form/NameFilter.vue';
  import { criminalParticipantType } from '@/types/criminal/jsonTypes';
  import { formatFromFullname } from '@/utils/utils';
  import { computed, ref } from 'vue';
  import SentenceOrderDetailsDialog from './SentenceOrderDetailsDialog.vue';
  import SentenceOrderDetailsTable from './SentenceOrderDetailsTable.vue';

  const props = defineProps<{ participants: criminalParticipantType[] }>();
  const selectedAccused = ref<string>();
  const filterByAccused = (item: criminalParticipantType) =>
    !selectedAccused.value ||
    (item.fullName &&
      formatFromFullname(item.fullName) === selectedAccused.value);

  const filteredParticipants = computed(
    () => props.participants?.filter(filterByAccused) ?? []
  );
  const count = computed(() =>
    filteredParticipants.value?.reduce((total, p) => total + p.count?.length, 0)
  );
  const dialogTitle = ref('');
  const dialogSubtitle = ref('');
  const dialogTargetProperty = ref('');
  const showDialog = ref(false);

  const openDialogClick = (isOrder = true) => {
    let title = 'Order Made Details';
    let subtitle = 'Conditions of Probation:';
    let targetProperty = 'sentDetailTxt';

    if (!isOrder) {
      title = `Judge's Recommendation`;
      subtitle = '';
      targetProperty = 'judgesRecommendation';
    }

    dialogTitle.value = title;
    dialogSubtitle.value = subtitle;
    dialogTargetProperty.value = targetProperty;
    showDialog.value = true;
  };
</script>
<style scope>
  .accused {
    max-width: 300px;
  }
</style>
