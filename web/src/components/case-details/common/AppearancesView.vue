<template>
  <v-row>
    <v-col cols="9" />
    <v-col>
      <NameFilter
        v-if="isCriminal"
        v-model="selectedAccused"
        :people="appearances"
      />
    </v-col>
  </v-row>
    <div
      v-if="pastAppearances.length || futureAppearances.length"
      v-for="(appearances, type) in {
        past: pastAppearances,
        future: futureAppearances,
      }"
      :key="type"
    >
      <v-card
        class="my-6"
        color="var(--bg-gray)"
        elevation="0"
        v-if="appearances?.length"
      >
        <v-card-text>
          <v-row align="center" no-gutters>
            <v-col class="text-h5" cols="6">
              {{
                type === 'future' ? 'Future Appearances' : 'Past Appearances'
              }}
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>
      <v-data-table-virtual
        v-if="appearances?.length"
        :headers="type === 'future' ? futureHeaders : pastHeaders"
        :items="appearances"
        :sort-by="sortBy"
        :height="pastAppearances.length && futureAppearances.length ? 400 : 800"
        item-value="appearanceId"
        fixed-header
        show-expand
        variant="hover"
      >
        <template
          v-slot:item.data-table-expand="{
            internalItem,
            isExpanded,
            toggleExpand,
          }"
        >
          <v-icon
            color="primary"
            :icon="isExpanded(internalItem) ? mdiChevronUp : mdiChevronDown"
            @click="toggleExpand(internalItem)"
          />
        </template>
        <template v-slot:expanded-row="{ columns, item }">
          <tr class="expanded">
            <td :colspan="columns.length">
              <CivilAppearanceDetails
                v-if="!isCriminal"
                :fileId="fileNumber"
                :appearanceId="item.appearanceId"
              />
              <CriminalAppearanceDetails
                v-else
                :fileId="fileNumber"
                :appearanceId="item.appearanceId"
                :partId="(item as criminalApprDetailType).partId"
                :isPast="type === 'past'"
              />
            </td>
          </tr>
        </template>
        <template v-slot:item.appearanceDt="{ value }">
          <span> {{ value }} </span>
        </template>
        <template v-slot:item.DARS="{ item }">
          <v-icon
            v-if="item.appearanceStatusCd === 'SCHD'"
            :icon="mdiHeadphones"
          />
        </template>
        <template v-slot:item.appearanceReasonCd="{ value, item }">
          <v-tooltip :text="item.appearanceReasonDsc" location="top">
            <template v-slot:activator="{ props }">
              <span v-bind="props" class="has-tooltip">{{ value }}</span>
            </template>
          </v-tooltip>
        </template>
        <template v-slot:item.appearanceTm="{ value, item }">
          {{ value ? extractTime(value) : '' }} <br />
          <span style="color: gray">
            {{
              hoursMinsFormatter(item.estimatedTimeHour, item.estimatedTimeMin)
            }}
          </span>
        </template>
        <template v-slot:item.courtLocation="{ value, item }">
          {{ value }} <br />
          <span style="color: gray">Room {{ item.courtRoomCd }}</span>
        </template>
        <template v-slot:item.appearanceStatusCd="{ value }">
          <AppearanceStatusChip :status="value" />
        </template>
      </v-data-table-virtual>
    </div>
  <div v-else>
    <v-card class="my-6" color="var(--bg-gray)" elevation="0">
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="12"> Appearances </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      :headers="pastHeaders"
      :items="pastAppearances"
      no-data-text="No appearances"
    >
    </v-data-table-virtual>
  </div>
</template>

<script setup lang="ts">
  import CivilAppearanceDetails from '@/components/case-details/civil/appearances/CivilAppearanceDetails.vue';
  import CriminalAppearanceDetails from '@/components/case-details/criminal/appearances/CriminalAppearanceDetails.vue';
  import AppearanceStatusChip from '@/components/shared/AppearanceStatusChip.vue';
  import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
  import { ApprDetailType } from '@/types/shared';
  import {
    extractTime,
    formatDateToDDMMMYYYY,
    hoursMinsFormatter,
  } from '@/utils/dateUtils';
  import { formatToFullName } from '@/utils/utils';
  import { mdiChevronDown, mdiChevronUp, mdiHeadphones } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    appearances: ApprDetailType[];
    isCriminal: boolean;
    fileNumber: string;
  }>();

  const pastHeaders = [
    {
      key: 'data-table-expand',
    },
    {
      title: 'DATE',
      key: 'appearanceDt',
      value: (item) => formatDateToDDMMMYYYY(item.appearanceDt),
      sortRaw: (a: ApprDetailType, b: ApprDetailType) =>
        new Date(a.appearanceDt).getTime() - new Date(b.appearanceDt).getTime(),
      width: '13%',
    },
    { title: '', key: 'DARS', sortable: false, width: '1%' },
    { title: 'REASON', key: 'appearanceReasonCd' },
    {
      title: 'TIME DURATION',
      key: 'appearanceTm',
    },
    { title: 'LOCATION ROOM', key: 'courtLocation' },
    {
      title: 'PRESIDER',
      key: 'judgeFullNm',
      value: (item) =>
        // Check for empty string with comma, this seems to be very common
        item.judgeFullNm && item.judgeFullNm !== ', ' ? item.judgeFullNm : '',
    },
    ...(props.isCriminal
      ? [
          {
            title: 'ACCUSED',
            key: 'name',
            value: (item) =>
              item.lastNm && item.givenNm
                ? item.lastNm + ', ' + item.givenNm
                : '',
          },
        ]
      : []),
    { title: 'STATUS', key: 'appearanceStatusCd' },
  ];
  const futureHeaders = [
    {
      key: 'data-table-expand',
    },
    {
      title: 'DATE',
      key: 'appearanceDt',
      value: (item) => formatDateToDDMMMYYYY(item.appearanceDt),
    },
    { title: 'REASON', key: 'appearanceReasonCd' },
    {
      title: 'TIME DURATION',
      key: 'appearanceTm',
    },
    { title: 'LOCATION ROOM', key: 'courtLocation' },
    { title: 'ACTIVITY', key: 'activity' },
    ...(props.isCriminal
      ? [
          {
            title: 'ACCUSED',
            key: 'name',
            value: (item) => formatToFullName(item.lastNm, item.givenNm),
          },
        ]
      : []),

    { title: 'STATUS', key: 'appearanceStatusCd' },
  ];

  const selectedAccused = ref<string>();
  const sortBy = ref([{ key: 'appearanceDt', order: 'desc' }] as const);
  const now = new Date();

  const filterByAccused = (appearance: criminalApprDetailType) =>
    !selectedAccused.value ||
    formatToFullName(appearance.lastNm, appearance.givenNm) ===
      selectedAccused.value;
  const futureAppearances = computed(() =>
    props.appearances
      ?.filter((app: ApprDetailType) => new Date(app?.appearanceDt) > now)
      .filter((app) =>
        props.isCriminal ? filterByAccused(app as criminalApprDetailType) : true
      )
  );
  const pastAppearances = computed(() =>
    props.appearances
      ?.filter((app: ApprDetailType) => new Date(app?.appearanceDt) <= now)
      .filter((app) =>
        props.isCriminal ? filterByAccused(app as criminalApprDetailType) : true
      )
  );
</script>

<style scoped>
  :deep() tr:has(+ tr.expanded) {
    background-color: var(--bg-very-light-gray) !important;
    border: thin solid rgba(var(--v-border-color), var(--v-border-opacity)) !important;
    border-bottom: 0 !important;
  }
  :deep() tr:has(+ tr.expanded) > td {
    border-bottom: 0 !important;
  }
</style>
