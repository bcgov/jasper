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
      />
      <template
        v-for="pairing in filteredTablePairings"
        :key="pairing.card.courtListLocationID"
        class="w-100"
      >
        <court-list-card :cardInfo="pairing.card" />
        <court-list-table :search="search" :data="pairing.table" />
      </template>
      <court-list-table-search-dialog
        v-model:showDialog="showDialog"
        :on-generate="onGenerateClick"
      />
    </v-skeleton-loader>
  </v-container>
</template>

<script setup lang="ts">
  import { CourtListService } from '@/services';
  import { usePDFViewerStore } from '@/stores';
  import { DivisionEnum } from '@/types/common';
  import { CourtListAppearance, CourtListCardInfo } from '@/types/courtlist';
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
  const appliedDate = ref<Date | null>(null);
  const showDropdown = ref(false);
  const search = ref('');
  const selectedFilesFilter = ref();
  const selectedAMPMFilter = ref();
  const documentUrls = ref<string[]>([]);
  const pdfStore = usePDFViewerStore();
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

  const filterByAMPM = (pairing: any) =>
    !selectedAMPMFilter.value || pairing.card.amPM === selectedAMPMFilter.value;

  const filterByFiles = (table: any) => {
    return selectedFilesFilter.value
      ? table.filter(filesFilterMap[selectedFilesFilter.value])
      : table;
  };

  const filteredTablePairings = computed<
    {
      card: CourtListCardInfo;
      table: CourtListAppearance[];
    }[]
  >(() => {
    return cardTablePairings.value
      .filter(filterByAMPM)
      .map((pairing) => ({ ...pairing, table: filterByFiles(pairing.table) }));
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

  const populateCardTablePairings = (data: any) => {
    cardTablePairings.value = [];
    if (!data?.items?.length) {
      return;
    }

    data.items.forEach((courtList: any) => {
      const courtRoomDetails = courtList.courtRoomDetails[0];
      const adjudicatorDetails = courtRoomDetails.adjudicatorDetails[0];
      if (adjudicatorDetails) {
        const card = {} as CourtListCardInfo;
        const appearances = courtList.appearances as CourtListAppearance[];
        card.fileCount = courtList.casesTarget;
        card.activity = courtList.activityDsc;
        card.presider = adjudicatorDetails?.adjudicatorNm;
        card.courtListRoom = courtRoomDetails.courtRoomCd;
        card.courtListLocationID = courtList.locationId;
        card.courtListLocation = courtList.locationNm;
        card.amPM = adjudicatorDetails?.amPm;

        cardTablePairings.value.push({ card, table: appearances });
      }
    });
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
        date: string;
        division: string;
        class: string;
        courtRoom: string;
      }
    >();
    cardTablePairings.value.forEach((element) => {
      element.table.forEach((data) => {
        const obj = {
          locationId: element.card.courtListLocationID,
          date: data.appearanceDt,
          division: data.courtDivisionCd,
          class: data.courtClassCd,
          courtRoom: data.courtRoomCd,
        };

        const key = `${obj.locationId}|${obj.date}|${obj.division}|${obj.class}|${obj.courtRoom}`;
        uniqueMap.set(key, obj);
      });
    });

    uniqueMap.forEach((value, key) => {
      let queryParams: Record<string, any> = {
        courtDivision: value.division,
        courtClass: value.class,
        date: value.date,
        locationId: value.locationId,
        roomCode: value.courtRoom,
      };

      if (value.division === DivisionEnum.R) {
        queryParams.reportType = reportType;
      } else if (value.division === DivisionEnum.I) {
        queryParams.additionsList = reportType === 'Additions' ? 'Y' : 'N';
      }

      documentUrls.value.push(courtListService.generateReportUrl(queryParams));
    });

    pdfStore.addUrls({
      urls: documentUrls.value,
    });

    window.open('/pdf-viewer', 'pdf-viewer');
  };
</script>
