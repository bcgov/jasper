<template>
  <CivilSummary :details />
  <AdjudicatorRestrictionsPanel
    v-if="adjudicatorRestrictions?.length > 0"
    :adjudicatorRestrictions
  />
  <PartiesPanel :courtClassCd="details.courtClassCd" :parties />
  <ChildrenPanel v-if="children.length > 0" :children />
</template>
<script setup lang="ts">
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import {
    AdjudicatorRestrictionsInfoType,
    RoleTypeEnum,
  } from '@/types/common';
  import AdjudicatorRestrictionsPanel from '../common/adjudicator-restrictions/AdjudicatorRestrictionsPanel.vue';
  import ChildrenPanel from './children/ChildrenPanel.vue';
  import CivilSummary from './CivilSummary.vue';
  import PartiesPanel from './parties/PartiesPanel.vue';

  const props = defineProps<{
    details: civilFileDetailsType;
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
  }>();

  const childRoleTypes = [RoleTypeEnum.CHD, RoleTypeEnum.COF];

  const parties =
    props.details.party?.filter(
      (p) => !childRoleTypes.includes(p.roleTypeCd)
    ) ?? [];
  const children =
    props.details.party?.filter((p) => childRoleTypes.includes(p.roleTypeCd)) ??
    [];
</script>
