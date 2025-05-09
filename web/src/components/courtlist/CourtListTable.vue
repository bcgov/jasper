<template>
  <v-data-table
    v-model="selected"
    must-sort
    :sort-by="sortBy"
    :items="data"
    :headers
    :search="search"
    item-value="appearanceId"
    items-per-page="100"
    class="pb-5"
  >
    <template v-slot:item.estimatedTime="{ item }">
      {{ hoursMinsFormatter(item.estimatedTimeHour, item.estimatedTimeMin) }}
    </template>
    <template v-slot:item.fileMarkers="{ item }">
      <FileMarkers class-override="ml-1 mt-1" :markers="getFileMarkers(item)" />
    </template>
    <template v-slot:item.appearanceReasonCd="{ value, item }">
      <v-tooltip :text="item.appearanceReasonDsc" location="top">
        <template v-slot:activator="{ props }">
          <span v-bind="props" class="has-tooltip">{{ value }}</span>
        </template>
      </v-tooltip>
    </template>
    <template v-slot:item.counsel="{ item }">
      <v-tooltip
        :disabled="
          (item.counsel?.length ?? 0) + (item.accusedCounselNm ? 1 : 0) < 2
        "
        location="top"
      >
        <template #activator="{ props }">
          <span
            v-bind="props"
            :class="{
              'has-tooltip':
                (item?.accusedCounselNm ? 1 : 0) + (item.counsel?.length ?? 0) >
                1,
            }"
            >{{ renderCounsel(item.accusedCounselNm, item.counsel) }}</span
          >
        </template>
        <span
          v-html="renderCounselTooltip(item.accusedCounselNm, item.counsel)"
        ></span>
      </v-tooltip>
    </template>
    <template v-slot:item.crown="{ value }">
      <v-tooltip :disabled="value?.length < 2" location="top">
        <template #activator="{ props }">
          <span :class="{ 'has-tooltip': value?.length > 1 }" v-bind="props">
            {{ renderName(value) }}
          </span>
        </template>
        <span v-html="renderTooltip(value)"></span>
      </v-tooltip>
    </template>
    <template v-slot:item.actions>
      <v-icon :icon="mdiNotebookEditOutline" size="large" />
      <v-icon :icon="mdiFileDocumentEditOutline" size="large" />
    </template>
    <template v-slot:bottom />
  </v-data-table>
</template>

<script setup lang="ts">
  import FileMarkers from '@/components/shared/FileMarkers.vue';
  import { FileMarkerEnum } from '@/types/common';
  import { CourtListAppearance } from '@/types/courtlist';
  import { PcssCounsel } from '@/types/criminal';
  import { hoursMinsFormatter } from '@/utils/dateUtils';
  import { mdiFileDocumentEditOutline, mdiNotebookEditOutline } from '@mdi/js';
  import { ref } from 'vue';

  const selected = defineModel<string[]>('selectedItems');
  const sortBy = ref([
    { key: 'appearanceSequenceNumber', order: 'asc' },
  ] as const);

  defineProps<{
    data: CourtListAppearance[];
    search: string;
  }>();

  const headers = ref([
    { key: 'data-table-group' },
    {
      title: '#',
      key: 'appearanceSequenceNumber',
    },
    { title: 'FILE #', key: 'courtFileNumber' },
    { title: 'ACCUSED/PARTIES', key: 'accusedNm' },
    { title: 'TIME', key: 'appearanceTm' },
    { title: 'EST.', key: 'estimatedTime' },
    { title: 'ROOM', key: 'courtRoomCd' },
    { title: 'REASON', key: 'appearanceReasonCd' },
    { title: 'FILE MARKERS', key: 'fileMarkers', sortable: false },
    { title: 'COUNSEL', key: 'counsel' },
    { title: 'CROWN', key: 'crown' },
    {
      title: 'CASE AGE (days)',
      key: 'caseAgeDays',
      value: (item: CourtListAppearance) => item.caseAgeDays ?? '',
    },
    { title: 'NOTES', key: 'actions', sortable: false },
  ]);

  const renderTooltip = (items: any[], additionalItem?: string) => {
    let tooltip =
      items?.map((item) => `${item?.lastNm}, ${item?.givenNm}`).join('<br/>') ||
      '';
    if (additionalItem) {
      tooltip += `${tooltip ? '<br/>' : ''}${splitNames(additionalItem)}`;
    }
    return tooltip;
  };

  const renderName = (items: any[], additionalItem?: string) => {
    if (!items?.length && !additionalItem) {
      return '';
    }
    let name = items?.[0] ? `${items[0]?.lastNm}, ${items[0]?.givenNm}` : '';
    const count = (items?.length || 0) + (additionalItem ? 1 : 0);
    if (additionalItem && !name) {
      name = splitNames(additionalItem);
    }
    return count > 1 ? `${name} +${count - 1}` : name;
  };

  const renderCounselTooltip = (
    accusedCounselNm: string,
    counsel: PcssCounsel[] | undefined
  ) => renderTooltip(counsel ?? [], accusedCounselNm);

  const renderCounsel = (
    accusedCounselNm: string,
    counsel: PcssCounsel[] | undefined
  ) => renderName(counsel ?? [], accusedCounselNm);

  const splitNames = (name: string) => {
    const [firstName, lastName] = name.split(' ');
    return `${lastName}, ${firstName}`;
  };

  const getFileMarkers = (item: CourtListAppearance) => {
    const { continuationYn, inCustodyYn, detainedYn } = item;
    return [
      { marker: FileMarkerEnum.CNT, value: continuationYn },
      { marker: FileMarkerEnum.IC, value: inCustodyYn },
      { marker: FileMarkerEnum.DO, value: detainedYn },
    ];
  };
</script>
