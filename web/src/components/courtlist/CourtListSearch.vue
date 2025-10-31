<template>
  <v-expand-transition>
    <v-card v-show="showDropdown" class="mx-auto" height="100">
      <v-container>
        <v-form @submit.prevent="searchForCourtList(true)">
          <v-row class="py-1">
            <v-col cols="3">
              <v-select
                v-model="selectedCourtLocation"
                :disabled="!searchAllowed || schedule === MY_SCHEDULE"
                @update:modelValue="selectedCourtRoom = ''"
                :items="locationsAndCourtRooms"
                return-object
                item-title="shortName"
                item-value="locationId"
                label="Location"
                :loading="!isLocationDataMounted"
                placeholder="Select a location"
                :error-messages="
                  errors.isMissingLocation ? ['Location is required'] : []
                "
              >
              </v-select>
            </v-col>
            <v-col cols="2">
              <v-select
                v-model="selectedCourtRoom"
                :disabled="!searchAllowed || schedule === MY_SCHEDULE"
                :items="
                  selectedCourtLocation
                    ? selectedCourtLocation.courtRooms
                    : ['']
                "
                label="Room"
                item-title="room"
                item-value="room"
                :error-messages="
                  errors.isMissingRoom ? ['Room is required'] : []
                "
              >
              </v-select>
            </v-col>
            <v-col cols="2">
              <v-date-input
                :disabled="!searchAllowed"
                prepend-icon=""
                prepend-inner-icon="$calendar"
                v-model="date"
              />
            </v-col>
            <v-col>
              <v-btn-toggle
                v-model="schedule"
                :disabled="!searchAllowed"
                border="sm"
                rounded="xl"
                group
                mandatory
              >
                <v-btn
                  :color="schedule === MY_SCHEDULE ? `${GREEN}` : ''"
                  :value="MY_SCHEDULE"
                  text="My Schedule"
                />
                <v-btn
                  :color="schedule === ROOM_SCHEDULE ? `${GREEN}` : ''"
                  :value="ROOM_SCHEDULE"
                  text="Room Schedule"
                />
              </v-btn-toggle>
            </v-col>
            <v-col>
              <v-btn-tertiary
                type="submit"
                text="Update court list"
                size="large"
              />
            </v-col>
            <v-spacer />
            <v-col>
              <v-btn :icon="mdiClose" @click="showDropdown = false" />
            </v-col>
          </v-row>
        </v-form>
      </v-container>
    </v-card>
  </v-expand-transition>
</template>

<script setup lang="ts">
  import { LocationService } from '@/services';
  import { CourtListService } from '@/services/CourtListService';
  import { useCommonStore, useCourtListStore } from '@/stores';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { LocationInfo } from '@/types/courtlist';
  import { mdiClose } from '@mdi/js';
  import {
    computed,
    inject,
    onMounted,
    onUnmounted,
    reactive,
    ref,
    watch,
  } from 'vue';

  // Component v-models
  const showDropdown = defineModel<boolean>('showDropdown');
  const isSearching = defineModel<boolean>('isSearching');
  const date = defineModel<Date>('date');
  const appliedDate = defineModel<Date | null>('appliedDate');

  const emit = defineEmits<(e: 'courtListSearched', data: any) => void>();
  const GREEN = '#62d3a4';
  const commonStore = useCommonStore();
  const courtListStore = useCourtListStore();
  const isDataReady = ref(false);
  const isMounted = ref(false);
  const isLocationDataMounted = ref(false);
  const searchAllowed = ref(true);
  const selectedCourtRoom = ref();
  const formSubmit = ref(false);
  const judgeId = ref(commonStore.userInfo?.judgeId);
  const TEN_MINUTES = 600000;
  const NINE_MINUTES = 540000;
  const ONE_MINUTE = 60000;
  const MY_SCHEDULE = 'my_schedule';
  const ROOM_SCHEDULE = 'room_schedule';
  const courtRefreshInterval = TEN_MINUTES;
  const warningRefreshInterval = NINE_MINUTES;
  const warningTime = ONE_MINUTE;
  const schedule = ref(MY_SCHEDULE);
  const shortHandDate = computed(() =>
    date.value ? date.value.toISOString().substring(0, 10) : ''
  );
  const errors = reactive({
    isMissingRoom: false,
    isMissingLocation: false,
  });
  const selectedCourtLocation = ref<LocationInfo | null>();
  const courtListService = inject<CourtListService>('courtListService');
  const locationsAndCourtRooms = ref<LocationInfo[]>();
  const locationsService = inject<LocationService>('locationService');
  const snackBarStore = useSnackbarStore();
  let searchInterval: NodeJS.Timeout;
  let warningInterval: NodeJS.Timeout;

  watch(schedule, () => scheduleChanged());
  watch(
    appliedDate,
    (newValue, oldValue) => {
      if (oldValue != null && newValue !== oldValue && !formSubmit.value) {
        searchForCourtList();
      }
      formSubmit.value = false;
    },
    { immediate: true }
  );

  watch(
    () => commonStore.userInfo?.judgeId,
    async (newVal) => {
      judgeId.value = newVal;
      selectedCourtLocation.value = getJudgeHomeLocation();
      searchForCourtList();
    }
  );

  watch([selectedCourtRoom, schedule, selectedCourtLocation, date], () =>
    setupAutoRefresh()
  );

  onMounted(() => {
    searchForCourtList();
    getListOfAvailableCourts();
  });

  onUnmounted(() => clearTimers());

  const getListOfAvailableCourts = async () => {
    locationsAndCourtRooms.value = await locationsService?.getLocations(true);
    commonStore.updateCourtRoomsAndLocations(locationsAndCourtRooms.value);
    isLocationDataMounted.value = true;
  };

  const getJudgeHomeLocation = () => {
    return (
      locationsAndCourtRooms.value?.find(
        (l) =>
          l.locationId === commonStore.userInfo?.judgeHomeLocationId?.toString()
      ) || null
    );
  };

  const getCourtListDetails = async () => {
    isDataReady.value = false;
    isMounted.value = false;
    isSearching.value = true;
    const data = await courtListService?.getCourtList(
      selectedCourtLocation?.value?.locationId
        ? selectedCourtLocation.value.locationId.toString()
        : null,
      selectedCourtRoom?.value,
      shortHandDate.value,
      judgeId.value!
    );

    if (data) {
      courtListStore.courtListInformation.detailsData = data;
    }
    emit('courtListSearched', data);
    isMounted.value = true;
    searchAllowed.value = true;
    isDataReady.value = true;
    isSearching.value = false;
    formSubmit.value = false;
    setupAutoRefresh();
  };
  const scheduleChanged = () => {
    selectedCourtLocation.value = null;
    if (schedule.value === ROOM_SCHEDULE) {
      selectedCourtLocation.value = getJudgeHomeLocation();
    }
    selectedCourtRoom.value = '';
    errors.isMissingLocation = false;
    errors.isMissingRoom = false;
  };

  const validateForm = () => {
    if (schedule.value === MY_SCHEDULE) {
      return true;
    }
    errors.isMissingLocation = !selectedCourtLocation.value;
    errors.isMissingRoom = !selectedCourtRoom.value;

    return !errors.isMissingRoom && !errors.isMissingLocation;
  };

  const searchForCourtList = (submittedFromForm = false) => {
    if (!validateForm()) {
      return;
    }
    formSubmit.value = submittedFromForm;
    showDropdown.value = false;
    searchAllowed.value = false;
    appliedDate.value = date.value ?? null;
    getCourtListDetails();
  };

  const setupAutoRefresh = () => {
    clearTimers();
    searchInterval = setInterval(() => {
      if (appliedDate.value) {
        searchForCourtList(true);
      }
    }, courtRefreshInterval);
    warningInterval = setInterval(() => {
      if (appliedDate.value && !isSearching.value) {
        showWarning();
      }
    }, warningRefreshInterval);
  };

  const showWarning = () => {
    snackBarStore.showSnackbar(
      'This page will refresh in 1 minute to ensure you see the latest updates.',
      '#b4e6ff',
      '🔄 Heads-up!',
      warningTime
    );
  };

  const clearTimers = () => {
    clearInterval(searchInterval);
    clearInterval(warningInterval);
    snackBarStore.hideSnackbar();
  };
</script>
