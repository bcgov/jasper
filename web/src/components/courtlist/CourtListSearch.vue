<template>
  <v-expand-transition>
    <v-card v-show="showDropdown" class="mx-auto" height="100" elevation="15">
      <v-container>
        <v-form @submit.prevent="searchForCourtList">
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
  import { LocationInfo } from '@/types/courtlist';
  import { courtListType } from '@/types/courtlist/jsonTypes';
  import { mdiClose } from '@mdi/js';
  import { computed, inject, onMounted, reactive, ref, watch } from 'vue';

  // Component v-models
  const showDropdown = defineModel<boolean>('showDropdown');
  const isSearching = defineModel<boolean>('isSearching');
  const date = defineModel<Date>('date');
  const bannerDate = defineModel<Date | null>('bannerDate');

  watch(bannerDate, (newValue, oldValue) => {
    if (oldValue != null && newValue !== oldValue) {
      searchForCourtList();
    }
  });
  const emit = defineEmits(['courtListSearched']);
  const GREEN = '#62d3a4';
  const commonStore = useCommonStore();
  const courtListStore = useCourtListStore();
  const isLoading = ref(false);
  const isDataReady = ref(false);
  const isMounted = ref(false);
  const isLocationDataMounted = ref(false);
  const searchAllowed = ref(true);
  const selectedCourtRoom = ref();
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

  if (!httpService) {
    throw new Error('Service is undefined.');
  }

  onMounted(async () => {
    await getListOfAvailableCourts();
    isLoading.value = false;
  });

  const getListOfAvailableCourts = async () => {
    locationsAndCourtRooms.value =
      await locationsService?.getLocations(true);
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
        emit('courtListSearched', data);
      })
      .finally(() => {
        isMounted.value = true;
        searchAllowed.value = true;
        isDataReady.value = true;
        isSearching.value = false;
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

  const searchForCourtList = () => {
    if (!validateForm()) {
      return;
    }
    showDropdown.value = false;
    searchAllowed.value = false;
    if (!bannerDate.value) {
      bannerDate.value = date.value;
    }
    getCourtListDetails();
  };
</script>
