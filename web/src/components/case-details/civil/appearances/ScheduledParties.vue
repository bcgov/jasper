<template>
  <v-data-table-virtual
    class="party-table"
    :headers
    :items="parties"
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
</template>

<script setup lang="ts">
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { PartyDetails } from '@/types/civil/jsonTypes/index';
  import { Anchor } from '@/types/common';
  import { formatFromFullname } from '@/utils/utils';

  defineProps<{ parties: PartyDetails[] }>();

  const headers = [
    {
      title: 'NAME',
      key: 'fullName',
      value: (item) => formatFromFullname(item.fullName),
    },
    { title: 'ROLE', key: 'role' },
    { title: 'CURRENT COUNSEL', key: 'counsel' },
  ];
</script>

<style scoped>
  .party-table {
    background-color: var(--bg-gray-200) !important;
    padding-bottom: 2rem !important;
  }
</style>
