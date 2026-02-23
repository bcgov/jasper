<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />

  <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == 403">
    <span> You are not authorized to access this page. </span>
  </v-card>
  <!------------------------------------------------------->
  <banner title="Court list" color="#183a4a" bgColor="#b4e6ff">
    <template #left v-if="appliedDate"
      >:
      <v-icon :icon="mdiChevronLeft" @click="addDay(-1)" />
      <b>{{ shortHandDate }}</b>
      <v-icon :icon="mdiChevronRight" @click="addDay(1)" />
    </template>
    <template #right>
      <v-btn @click="showDropdown = !showDropdown">
        Search by Courtroom & Date
      </v-btn>
    </template>
  </banner>

  <v-expand-transition>
    <v-card v-show="showDropdown" height="100" elevation="7">
      <court-list-search
        v-model:date="selectedDate"
        v-model:isSearching="searchingRequest"
        v-model:showDropdown="showDropdown"
        v-model:appliedDate="appliedDate"
        @courtListSearched="populateCardTablePairings"
      />
    </v-card>
  </v-expand-transition>
  <v-container>
    <v-skeleton-loader class="my-1" type="table" :loading="searchingRequest">
      <court-list-table-search
        v-if="cardTablePairings.length"
        v-model:filesFilter="selectedFilesFilter"
        v-model:AMPMFilter="selectedAMPMFilter"
        v-model:search="search"
        :isFuture
      />
      <template
        v-for="pairing in filteredTablePairings"
        :key="pairing.card.courtListLocationID"
      >
        <div class="w-100">
          <court-list-card :cardInfo="pairing.card" />
          <court-list-table
            :search="search"
            :data="pairing.tableData"
          />
        </div>
      </template>
      <court-list-table-search-dialog
        v-model:showDialog="showDialog"
        :on-generate="onGenerateClick"
      />
    </v-skeleton-loader>
    <div
      v-if="
        !searchingRequest && !cardTablePairings.length && searchResultMessage
      "
    >
      <p>{{ searchResultMessage }}</p>
    </div>
  </v-container>
</template>

<script setup lang="ts">
  import shared from '@/components/shared';
  import { CourtListService } from '@/services';
  import { ApiResponse } from '@/types/ApiResponse';
  import { DivisionEnum } from '@/types/common';
  import {
    CourtListAppearance,
    CourtListCardInfo,
    CourtListSearchResult,
  } from '@/types/courtlist';
  import { DocumentRequestType } from '@/types/shared';
  import {
    formatDateInstanceToDDMMMYYYY,
    parseDDMMMYYYYToDate,
  } from '@/utils/dateUtils';
  import { parseQueryStringToString } from '@/utils/utils';
  import { mdiChevronLeft, mdiChevronRight } from '@mdi/js';
  import { computed, inject, provide, ref, watch } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import CourtListSearch from './CourtListSearch.vue';
  import CourtListTable from './CourtListTable.vue';
  import CourtListTableSearch from './CourtListTableSearch.vue';

  const route = useRoute();
  const router = useRouter();
  const errorCode = ref(0);
  const searchingRequest = ref(false);
  const isLoading = ref(false);
  const selectedDate = ref(
    parseDDMMMYYYYToDate(parseQueryStringToString(route.query.date)) ??
      new Date()
  );
  const searchResultMessage = ref<string>('');
  const appliedDate = ref<Date | null>(null);
  const showDropdown = ref(false);
  const search = ref('');
  const selectedFilesFilter = ref();
  const selectedAMPMFilter = ref<string | null>(null);
  const documentUrls = ref<string[]>([]);
  const cardTablePairings = ref<
    {
      card: CourtListCardInfo;
      table: CourtListAppearance[];
    }[]
  >([]);
  const filesFilterMap: {
    [key: string]: (appearance: CourtListAppearance) => boolean;
  } = {
    Complete: (appearance: CourtListAppearance) => !!appearance.isComplete,
    Cancelled: (appearance: CourtListAppearance) =>
      appearance.appearanceStatusCd === 'CNCL',
    'To be called': (appearance: CourtListAppearance) =>
      appearance.appearanceStatusCd === 'SCHD',
  };
  const showDialog = ref(false);
  // We ignore the time portion of the date for comparison as we only care about the day
  const isFuture = computed(
    () =>
      appliedDate.value !== null &&
      appliedDate.value.setHours(0, 0, 0, 0) >= new Date().setHours(0, 0, 0, 0)
  );

  const filterByAMPM = (table: CourtListAppearance[]) =>
    selectedAMPMFilter.value 
      ? table.filter((appearance: CourtListAppearance) => appearance.appearanceTm.includes(selectedAMPMFilter.value || ''))
      : table;

  const filterByFiles = (table: CourtListAppearance[]) => 
    selectedFilesFilter.value
      ? table.filter(filesFilterMap[selectedFilesFilter.value])
      : table;

  const filteredTablePairings = computed<
    {
      card: CourtListCardInfo;
      tableData: CourtListAppearance[];
    }[]
  >(() => {
    return cardTablePairings.value
      .map((pairing) => ({ ...pairing, tableData: filterByFiles(filterByAMPM(pairing.table)) }));
  });

  const shortHandDate = computed(() =>
    appliedDate.value
      ? appliedDate.value.toLocaleDateString('en-US', {
          weekday: 'long',
          day: '2-digit',
          month: 'long',
          year: 'numeric',
        })
      : ''
  );

  watch(selectedDate, (newValue) => {
    if (!route.query.date) {
      return;
    }

    router.replace({
      query: {
        ...route.query,
        date: formatDateInstanceToDDMMMYYYY(newValue),
      },
    });
  });

  const courtListService = inject<CourtListService>('courtListService');
  if (!courtListService) {
    throw new Error('Service(s) is undefined.');
  }

  const populateCardTablePairings = (
    resp?: ApiResponse<CourtListSearchResult>
  ) => {
    searchResultMessage.value = '';
    cardTablePairings.value = [];
    if (!resp) {
      return;
    }

    if (!resp.succeeded) {
      searchResultMessage.value =
        resp.errors?.[0] ??
        'An error occurred while searching. Please try again.';
      return;
    }

    const items = resp.payload?.items;
    if (resp.succeeded && (!items || items.length === 0)) {
      searchResultMessage.value = 'No activities.';
      return;
    }

    const data = resp.payload;
    for (const courtList of data.items) {
      const courtRoomDetails = courtList.courtRoomDetails[0];
      if (!courtRoomDetails) {
        return;
      }
      const adjudicatorDetails = courtRoomDetails.adjudicatorDetails[0];
      const card = {} as CourtListCardInfo;
      card.fileCount = courtList.appearances.length;
      card.activity = courtList.activityDsc;
      card.presider = adjudicatorDetails?.adjudicatorNm;
      card.courtListRoom = courtRoomDetails.courtRoomCd;
      card.courtListLocationID = courtList.locationId;
      card.courtListLocation = courtList.locationNm;
      card.amPM = adjudicatorDetails?.amPm;

      // In some cases, the adjudicatorDetails won't have AMPM information, but the courtRoomDetails may
      if (!card.amPM) {
        if (courtRoomDetails.isAM === 'Y' && courtRoomDetails.isPM !== 'Y') {
          card.amPM = 'AM';
        } else if (courtRoomDetails.isPM === 'Y' && courtRoomDetails.isAM !== 'Y') {
          card.amPM = 'PM';
        } else {
          card.amPM = 'AM/PM';
        }
      }

      cardTablePairings.value.push({ card, table: courtList.appearances });
    }

    // We always want AM pairings to appear before PM pairings
    cardTablePairings.value.sort((a, b) =>
      a.card.amPM?.localeCompare(b.card.amPM)
    );
  };

  const addDay = (days: number) => {
    if (appliedDate.value) {
      appliedDate.value = new Date(
        appliedDate.value.setDate(appliedDate.value.getDate() + days)
      );
      selectedDate.value = appliedDate.value;
    }
  };

  provide('menuClicked', () => {
    showDialog.value = true;
  });

  const onGenerateClick = (reportType: 'Daily' | 'Additions') => {
    documentUrls.value = [];
    // Prepare unique combinations to generate report(s)
    const uniqueMap = new Map<
      string,
      {
        locationId: number;
        locationName: string;
        date: string;
        division: string;
        class: string;
        courtRoom: string;
      }
    >();
    for (const element of cardTablePairings.value) {
      for (const data of element.table) {
        const obj = {
          locationId: element.card.courtListLocationID,
          locationName: element.card.courtListLocation,
          date: data.appearanceDt,
          division: data.courtDivisionCd,
          class: data.courtClassCd,
          courtRoom: data.courtRoomCd,
        };

        const key = `${obj.locationId}|${obj.locationName}|${obj.date}|${obj.division}|${obj.class}|${obj.courtRoom}`;
        uniqueMap.set(key, obj);
      }
    }
    const documents: Array<{
      documentType: DocumentRequestType;
      documentData: Record<string, any>;
      groupKeyTwo: string;
      documentName: any;
      groupKeyOne: string;
    }> = [];
    for (const value of uniqueMap.values()) {
      const documentData: Record<string, any> = {
        courtDivisionCd: value.division,
        courtClass: value.class,
        date: value.date,
        locationId: value.locationId,
        roomCode: value.courtRoom,
      };
      if (value.division === DivisionEnum.R) {
        documentData.reportType = reportType;
        documentData.isCriminal = true;
      } else if (value.division === DivisionEnum.I) {
        documentData.additionsList = reportType === 'Additions' ? 'Y' : 'N';
      }

      documents.push({
        documentType: DocumentRequestType.Report,
        documentData,
        groupKeyTwo: value.courtRoom,
        documentName: reportType,
        groupKeyOne: value.locationName,
      });
    }
    shared.openMergedDocuments(documents);
  };
</script>
