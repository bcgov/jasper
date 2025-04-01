<template>
  <v-expand-transition>
    <v-card v-show="showDropdown" class="mx-auto" height="100">
      <v-container>
        <v-form @submit.prevent="searchForCourtList(true)">
          <v-row class="py-1">
            <v-col cols="3">
              <v-select
                v-model="selectedCourtLocation"
                :disabled="!searchAllowed"
                @update:modelValue="selectedCourtRoom = ''"
                :items="locationsAndCourtRooms"
                return-object
                item-title="name"
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
                :disabled="!searchAllowed"
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
                rounded="xl"
                group
                mandatory
              >
                <v-btn
                  :color="schedule === 'my_schedule' ? `${GREEN}` : ''"
                  value="my_schedule"
                  disabled
                >
                  My Schedule
                </v-btn>
                <v-btn
                  :color="schedule === 'room_schedule' ? `${GREEN}` : ''"
                  value="room_schedule"
                >
                  Room Schedule
                </v-btn>
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
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCourtListStore } from '@/stores';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { LocationInfo } from '@/types/courtlist';
  import { courtListType } from '@/types/courtlist/jsonTypes';
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

  const emit =
    defineEmits<
      (
        e: 'courtListSearched',
        data: any,
        selectedLocationId: string,
        selectedCourtRoom: string
      ) => void
    >();
  const GREEN = '#62d3a4';
  const commonStore = useCommonStore();
  const courtListStore = useCourtListStore();
  const isLoading = ref(false);
  const isDataReady = ref(false);
  const isMounted = ref(false);
  const isLocationDataMounted = ref(false);
  const searchAllowed = ref(true);
  const selectedCourtRoom = ref();
  const formSubmit = ref(false);
  const TEN_MINUTES = 600000;
  const NINE_MINUTES = 540000;
  const ONE_MINUTE = 60000;
  const courtRefreshInterval = TEN_MINUTES;
  const warningRefreshInterval = NINE_MINUTES;
  const warningTime = ONE_MINUTE;
  const schedule = ref('room_schedule');
  const shortHandDate = computed(() =>
    date.value ? date.value.toISOString().substring(0, 10) : ''
  );
  const errors = reactive({
    isMissingRoom: false,
    isMissingLocation: false,
  });
  const selectedCourtLocation = ref<LocationInfo>();
  const httpService = inject<HttpService>('httpService');
  const locationsAndCourtRooms = ref<LocationInfo[]>();
  const locationsService = inject<LocationService>('locationService');
  const snackBarStore = useSnackbarStore();
  let searchInterval: NodeJS.Timeout;
  let warningInterval: NodeJS.Timeout;

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

  watch([selectedCourtRoom, schedule, selectedCourtLocation, date], () =>
    setupAutoRefresh()
  );

  if (!httpService) {
    throw new Error('Service is undefined.');
  }

  onMounted(async () => {
    await getListOfAvailableCourts();
    isLoading.value = false;
  });

  onUnmounted(() => clearTimers());

  const getListOfAvailableCourts = async () => {
    locationsAndCourtRooms.value = await locationsService?.getLocations(true);
    commonStore.updateCourtRoomsAndLocations(locationsAndCourtRooms.value);
    isLocationDataMounted.value = true;
  };

  const getCourtListDetails = () => {
    if (!selectedCourtLocation.value) {
      return;
    }
    isDataReady.value = false;
    isMounted.value = false;
    isSearching.value = true;

    // todo: extract to service layer
    httpService
      .get<courtListType>(
        'api/courtlist/court-list?agencyId=' +
          selectedCourtLocation.value.locationId.toString() +
          '&roomCode=' +
          selectedCourtRoom.value +
          '&proceeding=' +
          shortHandDate.value
      )
      .then((Response) => Response)
      .then((data) => {
        if (data) {
          courtListStore.courtListInformation.detailsData = data;
        }
        emit(
          'courtListSearched',
          data,
          selectedCourtLocation.value!.locationId,
          selectedCourtRoom.value
        );
      })
      .finally(() => {
        isMounted.value = true;
        searchAllowed.value = true;
        isDataReady.value = true;
        isSearching.value = false;
        formSubmit.value = false;
        setupAutoRefresh();
      });
  };

  const validateForm = () => {
    if (schedule.value === 'my_schedule') {
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
