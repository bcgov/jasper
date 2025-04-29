<template>
  <v-card variant="text" class="mb-4">
    <v-row>
      <v-col class="pb-0">
        <v-chip
          variant="flat"
          rounded="lg"
          color="var(--bg-pale-blue)"
          class="w-100 justify-center align-center text-uppercase"
        >
          {{ getName }}
        </v-chip>
      </v-col>
    </v-row>
    <v-row v-if="showAlias">
      <v-col cols="6" class="data-label">Alias</v-col>
      <v-col>
        <LabelWithTooltip :values="aliases" />
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="6" class="data-label">Role</v-col>
      <v-col>{{ party.roleTypeDescription }}</v-col>
    </v-row>
    <v-row>
      <v-col cols="6" class="data-label">Counsel</v-col>
      <v-col>
        <LabelWithTooltip :values="counselNames" />
      </v-col>
    </v-row>
  </v-card>
</template>
<script setup lang="ts">
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { partyType } from '@/types/civil/jsonTypes';
  import { CourtClassEnum } from '@/types/common';
  import { formatToFullName } from '@/utils/utils';
  import { computed } from 'vue';

  const props = defineProps<{
    party: partyType;
    courtClassCd: string;
  }>();

  // Alias is only visible for Small Claims
  const showAlias = props.courtClassCd === CourtClassEnum[CourtClassEnum.C];
  const counselNames =
    props.party.selfRepresentedYN === 'Y'
      ? ['Self-Represented']
      : (props.party.counsel?.map((c) => c.fullNm) ?? []);
  const aliases =
    props.party.aliases?.map((a) =>
      a.surnameNm && a.firstGivenNm
        ? `${a.surnameNm?.toUpperCase()}, ${a.firstGivenNm} ${a.secondGivenNm ?? ''} ${a.thirdGivenNm ?? ''}`
        : a.organizationNm
    ) ?? [];

  const getName = computed(() => {
    const { lastNm, givenNm, orgNm } = props.party;
    return lastNm ? formatToFullName(lastNm, givenNm) : orgNm;
  });
</script>
<style scoped>
  .v-chip {
    color: var(--text-deep-blue);
  }
</style>
