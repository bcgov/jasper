<template>
  <v-skeleton-loader
    class="my-0"
    type="table"
    :height="200"
    color="var(--bg-gray-200)"
    :loading="partiesLoading"
  >
    <v-data-table-virtual
      class="party-table"
      :headers
      :items="parties?.party"
      item-value="appearanceId"
    >
      <template v-slot:item.role="{ item }">
        <span v-for="(role, index) in item.partyRole" :key="index">
          <span v-if="role.roleTypeDsc">
            {{ role.roleTypeDsc }}
          </span>
        </span>
      </template>
      <template v-slot:item.counsel="{ item }">
        <LabelWithTooltip
          v-if="item.counsel?.length > 0"
          :values="item.counsel.map((issue) => issue.counselFullName)"
          :location="Anchor.Top"
        />
      </template>
    </v-data-table-virtual>
  </v-skeleton-loader>
</template>

<script setup lang="ts">
  import { onMounted, inject, ref } from 'vue';
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { CivilAppearanceDetailParty } from '@/types/civil/jsonTypes/index';
  import { Anchor } from '@/types/common';
  import { formatFromFullname } from '@/utils/utils';
  import { FilesService } from '@/services';

  const partiesLoading = ref(false);
  const filesService = inject<FilesService>('filesService');
  const parties = ref<CivilAppearanceDetailParty>();

  const headers = [
    {
      title: 'NAME',
      key: 'fullName',
      value: (item) => formatFromFullname(item.fullName),
    },
    { title: 'ROLE', key: 'role' },
    { title: 'CURRENT COUNSEL', key: 'counsel' },
  ];

  const props = defineProps<{
    fileId: string;
    appearanceId: string;
  }>();

  onMounted(async () => {
    partiesLoading.value = true;
    const documentsResponse = await filesService?.civilAppearanceParty(
      props.fileId,
      props.appearanceId
    );
    parties.value = documentsResponse;
    partiesLoading.value = false;
  });
</script>

<style scoped>
  .party-table {
    background-color: var(--bg-gray-200) !important;
    padding-bottom: 2rem !important;
  }
</style>
