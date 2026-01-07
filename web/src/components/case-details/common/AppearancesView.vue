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
  <v-card class="my-6" color="var(--bg-gray-500)" elevation="0">
    <v-card-text>
      <v-row align="center" no-gutters>
        <v-col class="text-h5" cols="6"> Appearances </v-col>
      </v-row>
    </v-card-text>
  </v-card>
  <v-data-table-virtual
    v-if="appearances?.length"
    :headers="headers"
    :items="appearances"
    :sort-by="sortBy"
    :height="800"
    item-value="appearanceId"
    fixed-header
    show-expand
    variant="hover"
  >
    <template v-slot:[`header.appearanceTm`]>
      TIME<br />EST. /DURATION
    </template>
    <template v-slot:[`header.courtLocation`]> LOCATION ROOM </template>
    <template
      v-slot:[`item.data-table-expand`]="{
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
    <template v-slot:[`expanded-row`]="{ columns, item }">
      <tr class="expanded">
        <td :colspan="columns.length">
          <CivilAppearanceDetails
            v-if="!isCriminal"
            :fileId="fileNumber"
            :appearanceId="item.appearanceId"
            :courtClassCd="courtClassCd"
          />
          <CriminalAppearanceDetails
            v-else
            :fileId="fileNumber"
            :appearanceId="item.appearanceId"
            :partId="(item as criminalApprDetailType).partId"
          />
        </td>
      </tr>
    </template>
    <template v-slot:[`item.appearanceDt`]="{ value }">
      <span> {{ value }} </span>
    </template>
    <template v-slot:[`item.transcripts`]="{ item }">
      <v-icon
        v-if="getAppearanceTranscripts(item.appearanceId).length > 0"
        :icon="mdiFileDocumentOutline"
        class="cursor-pointer"
        :data-testid="`transcript-button-${item.appearanceId}`"
        @click="handleTranscriptClick($event, item.appearanceId)"
      />
      <v-menu
        v-if="getAppearanceTranscripts(item.appearanceId).length > 1"
        v-model="transcriptMenus[item.appearanceId]"
        :activator="`[data-testid='transcript-button-${item.appearanceId}']`"
        location="bottom"
      >
        <v-list>
          <v-list-item
            v-for="transcript in getAppearanceTranscripts(item.appearanceId)"
            :key="`${transcript.orderId}-${transcript.id}`"
            @click="openTranscript(transcript)"
            class="cursor-pointer"
          >
            <v-list-item-title>
              Transcript - {{ transcript.description }}
            </v-list-item-title>
          </v-list-item>
        </v-list>
      </v-menu>
    </template>
    <template v-slot:[`item.DARS`]="{ item }">
      <v-icon
        v-if="item.appearanceStatusCd === 'SCHD'"
        :icon="mdiHeadphones"
        class="cursor-pointer"
        :data-testid="`dars-button-${item.appearanceId}`"
        @click="openDarsModal(item)"
      />
    </template>
    <template v-slot:[`item.appearanceReasonCd`]="{ value, item }">
      <v-tooltip :text="item.appearanceReasonDsc" location="top">
        <template v-slot:[`activator`]="{ props }">
          <span v-bind="props" class="has-tooltip">{{ value }}</span>
        </template>
      </v-tooltip>
    </template>
    <template v-slot:[`item.appearanceTm`]="{ value, item }">
      {{ value ? extractTime(value) : '' }} <br />
      <span style="color: gray">
        {{ hoursMinsFormatter(item.estimatedTimeHour, item.estimatedTimeMin) }}
      </span>
    </template>
    <template v-slot:[`item.courtLocation`]="{ value, item }">
      {{ value }} &nbsp; <span style="color: gray">{{ item.courtRoomCd }}</span>
    </template>
    <template v-slot:[`item.appearanceStatusCd`]="{ value }">
      <AppearanceStatusChip :status="value" />
    </template>
  </v-data-table-virtual>
  <div v-else>
    <v-card class="my-6" color="var(--bg-gray-500)" elevation="0">
      <v-card-text>
        <v-row align="center" no-gutters>
          <v-col class="text-h5" cols="12"> Appearances </v-col>
        </v-row>
      </v-card-text>
    </v-card>
    <v-data-table-virtual
      :headers="headers"
      :items="appearances"
      no-data-text="No appearances"
    >
    </v-data-table-virtual>
  </div>
</template>

<script setup lang="ts">
  import CriminalAppearanceDetails from '@/components/case-details/criminal/appearances/CriminalAppearanceDetails.vue';
  import CivilAppearanceDetails from '@/components/civil/CivilAppearanceDetails.vue';
  import shared from '@/components/shared';
  import AppearanceStatusChip from '@/components/shared/AppearanceStatusChip.vue';
  import { TranscriptDocument } from '@/services/DarsService';
  import { useDarsStore } from '@/stores/DarsStore';
  import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
  import {
    ApprDetailType,
    CourtDocumentType,
    FileDetailsType,
  } from '@/types/shared';
  import {
    extractTime,
    formatDateToDDMMMYYYY,
    hoursMinsFormatter,
  } from '@/utils/dateUtils';
  import { formatToFullName } from '@/utils/utils';
  import {
    mdiChevronDown,
    mdiChevronUp,
    mdiFileDocumentOutline,
    mdiHeadphones,
  } from '@mdi/js';
  import { computed, ref } from 'vue';

  const props = defineProps<{
    appearances: ApprDetailType[];
    isCriminal: boolean;
    fileNumber: string;
    courtClassCd: string;
    transcripts?: TranscriptDocument[];
    details: FileDetailsType;
  }>();

  const headers = [
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
    { title: '', key: 'transcripts', sortable: false, width: '1%' },
    { title: '', key: 'DARS', sortable: false, width: '1%' },
    { title: 'REASON', key: 'appearanceReasonCd' },
    {
      title: 'TIME EST. /DURATION',
      key: 'appearanceTm',
    },
    { title: 'LOCATION ROOM ACTIVITY', key: 'courtLocation' },
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

  const selectedAccused = ref<string>();
  const sortBy = ref([{ key: 'appearanceDt', order: 'desc' }] as const);
  const darsStore = useDarsStore();
  const transcriptMenus = ref<Record<string, boolean>>({});

  const getAppearanceTranscripts = (appearanceId: string | number) => {
    if (!props.transcripts) return [];
    const appearanceIdStr = String(appearanceId);
    return props.transcripts.filter((transcript) =>
      transcript.appearances.some((app) => {
        // For criminal files, match on justinAppearanceId
        // For civil files, match on ceisAppearanceId
        const transcriptAppearanceId = props.isCriminal
          ? String(app.justinAppearanceId)
          : String(app.ceisAppearanceId);
        return transcriptAppearanceId === appearanceIdStr;
      })
    );
  };

  const handleTranscriptClick = (event: MouseEvent, appearanceId: string) => {
    const transcripts = getAppearanceTranscripts(appearanceId);
    if (transcripts.length === 1) {
      // Open single transcript directly
      event.stopPropagation();
      openTranscript(transcripts[0]);
    }
    // For multiple transcripts, the menu will open automatically via v-menu
  };

  const openTranscript = (transcript: TranscriptDocument) => {
    shared.openDocumentsPdf(CourtDocumentType.Transcript, {
      documentId: transcript.id.toString(),
      orderId: transcript.orderId.toString(),
      documentDescription: `Transcript - ${transcript.description}`,
      fileId: props.fileNumber,
      fileNumberText: props.details.fileNumberTxt,
      courtLevel: props.details.courtLevelCd,
      courtClass: props.details.courtClassCd,
      location: props.details.homeLocationAgencyName,
      isCriminal: props.isCriminal,
    });
  };

  const openDarsModal = (item: ApprDetailType) => {
    // Parse the date string to Date object
    darsStore.openModal(
      new Date(item.appearanceDt),
      item.locationId || null,
      item.courtRoomCd || ''
    );
  };

  const filterByAccused = (appearance: criminalApprDetailType) =>
    !selectedAccused.value ||
    formatToFullName(appearance.lastNm, appearance.givenNm) ===
      selectedAccused.value;
  const appearances = computed(() =>
    props.appearances.filter((app) =>
      props.isCriminal ? filterByAccused(app as criminalApprDetailType) : true
    )
  );
</script>

<style scoped>
  :deep() tr:has(+ tr.expanded) {
    background-color: var(--bg-gray-200) !important;
    border: thin solid rgba(var(--v-border-color), var(--v-border-opacity)) !important;
    border-bottom: 0 !important;
  }
  :deep() tr:has(+ tr.expanded) > td {
    border-bottom: 0 !important;
  }
</style>
