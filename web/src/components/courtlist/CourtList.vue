<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />

  <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == 403">
    <span> You are not authorized to access this page. </span>
  </v-card>
  <!------------------------------------------------------->
  <banner title="Court list" color="#183a4a" bgColor="#b4e6ff" />

  <v-container>
    <v-form @submit.prevent="searchForCourtList">
      <v-row class="mt-2 ml-2">
        <v-col md="4">
          <v-select
            v-model="selectedCourtLocation"
            id="locationSelect"
            :disabled="!searchAllowed"
            :state="selectedCourtLocationState ? null : false"
            @update:modelValue="LocationChanged"
            :items="courtRoomsAndLocations"
            item-title="text"
            item-value="value"
            label="Location"
            :clearable="false"
          >
          </v-select>
        </v-col>
        <v-col>
          <v-date-input
            prepend-icon=""
            prepend-inner-icon="$calendar"
            v-model="selectedDate"
          />
        </v-col>
        <v-col>
          <v-select
            v-if="syncFlag"
            v-model="selectedCourtRoom"
            id="roomSelect"
            :disabled="!searchAllowed"
            @change="RoomChanged"
            :items="selectedCourtLocation.Rooms"
            :state="selectedCourtRoomState ? null : false"
            label="Room"
            item-title="text"
            item-value="value"
            :clearable="false"
            required
          >
          </v-select>
        </v-col>
        <v-col>
          <action-buttons :showReset="false" />
        </v-col>
      </v-row>
    </v-form>

    <div v-if="searchingRequest">
      <v-card class="pa-md-4 mx-lg-auto">
        <v-row>
          <v-col>
            <h2>{{ shortHandDate }}</h2>
          </v-col>
          <v-col>
            <h4>{{ courtListLocation }}</h4>
          </v-col>
          <v-col>
            <h5>({{ courtListLocationID }})</h5>
          </v-col>
          <v-col>
            <h3>CourtRoom: {{ courtListRoom }}</h3>
          </v-col>
        </v-row>
        <v-row>
          <v-col>
            Total Cases (
            <b>{{ totalCases }}</b>
            )
            <span>
              <b> {{ totalTime }}</b> {{ totalTimeUnit }}
            </span>
          </v-col>
          <v-col>
            Criminal (
            <b>{{ criminalCases }}</b>
            )
          </v-col>
          <v-col>
            Family (
            <b>{{ familyCases }}</b>
            )
          </v-col>
          <v-col>
            Civil (
            <b>{{ civilCases }}</b>
            )
          </v-col>
        </v-row>
      </v-card>
      <v-skeleton-loader type="table" :loading="!isMounted && !isDataReady">
        <court-list-layout v-if="isDataReady" class="pt-6" />
      </v-skeleton-loader>
    </div>
  </v-container>
</template>

<script setup lang="ts">
  import Banner from '@/components/shared/Banner.vue';
  import ActionButtons from '@/components/shared/Form/ActionButtons.vue';
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCourtListStore } from '@/stores';
  import { CourtRoomsJsonInfoType } from '@/types/common';
  import * as _ from 'underscore';
  import { computed, inject, onMounted, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';

  import {
    courtRoomsAndLocationsInfoType,
    locationInfoType,
    roomsInfoType,
  } from '@/types/courtlist';
  import { courtListType } from '@/types/courtlist/jsonTypes';
  import CourtListLayout from './CourtListLayout.vue';

  const commonStore = useCommonStore();
  const courtListStore = useCourtListStore();
  // State variables
  const errorCode = ref(0);
  const errorText = ref('');
  const isDataReady = ref(false);
  const isMounted = ref(false);
  const searchingRequest = ref(false);
  const isLocationDataReady = ref(false);
  const isLocationDataMounted = ref(false);
  const searchAllowed = ref(true);
  const syncFlag = ref(true);
  const totalCases = ref(0);
  const criminalCases = ref(0);
  const familyCases = ref(0);
  const civilCases = ref(0);
  const totalHours = ref(0);
  const totalMins = ref(0);
  const isLoading = ref(true);
  const totalTime = ref('');
  const totalTimeUnit = ref('Hours');
  const courtRoomsAndLocationsJson = ref<CourtRoomsJsonInfoType[]>([]);
  const courtRoomsAndLocations = ref<courtRoomsAndLocationsInfoType[]>([]);
  const selectedDate = ref(new Date());
  const shortHandDate = computed(() =>
    selectedDate.value.toISOString().substring(0, 10)
  );
  const selectedCourtRoom = ref('');
  const selectedCourtRoomState = ref(true);
  const selectedCourtLocation = ref({} as locationInfoType);
  const selectedCourtLocationState = ref(true);
  const courtListLocation = ref('Vancouver');
  const courtListLocationID = ref('4801');
  const courtListRoom = ref('005');
  const httpService = inject<HttpService>('httpService');
  const router = useRouter();
  const route = useRoute();

  if (!httpService) {
    throw new Error('Service is undefined.');
  }

  // Fetch data on mount
  onMounted(() => {
    getListOfAvailableCourts();
    isLoading.value = false;
  });

  const getListOfAvailableCourts = () => {
    errorCode.value = 0;
    httpService
      .get<CourtRoomsJsonInfoType[]>('api/location/court-rooms')
      .then(
        (Response) => Response,
        (err) => {
          errorCode.value = err.status;
          errorText.value = err.statusText;
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          courtRoomsAndLocationsJson.value = data;
          commonStore.updateCourtRoomsAndLocations(data);
          ExtractCourtRoomsAndLocationsInfo();
          if (courtRoomsAndLocations.value.length > 0) {
            isLocationDataReady.value = true;
          }
        }
        isLocationDataMounted.value = true;
      });
  };

  const getCourtListDetails = () => {
    isDataReady.value = false;
    isMounted.value = false;
    searchingRequest.value = true;
    totalCases.value = 0;
    criminalCases.value = 0;
    familyCases.value = 0;
    civilCases.value = 0;
    totalHours.value = 0;
    totalMins.value = 0;
    totalTime.value = '0';
    totalTimeUnit.value = 'Hours';
    errorCode.value = 0;

    httpService
      .get<courtListType>(
        'api/courtlist/court-list?agencyId=' +
          courtListLocationID.value +
          '&roomCode=' +
          courtListRoom.value +
          '&proceeding=' +
          shortHandDate.value
      )
      .then((Response) => Response)
      .then((data) => {
        if (data) {
          courtListStore.courtListInformation.detailsData = data;
          totalCases.value =
            data.civilCourtList.length + data.criminalCourtList.length;
          criminalCases.value = data.criminalCourtList.length;
          for (const civil of data.civilCourtList) {
            if (civil.activityClassCd == 'F' || civil.activityClassCd == 'E')
              familyCases.value++;
            else civilCases.value++;
            setTotalTimeForRoom(
              civil.estimatedTimeHour,
              civil.estimatedTimeMin
            );
          }

          for (const criminal of data.criminalCourtList) {
            setTotalTimeForRoom(
              criminal.estimatedTimeHour,
              criminal.estimatedTimeMin
            );
          }

          courtListStore.updateCourtList(courtListStore.courtListInformation);

          if (totalMins.value > 0 && totalHours.value > 0) {
            totalTime.value = (totalHours.value + totalMins.value / 60).toFixed(
              1
            );
            totalTimeUnit.value = 'Hours';
          } else if (totalMins.value > 0 && totalHours.value == 0) {
            totalTime.value = totalMins.value.toString();
            totalTimeUnit.value = 'Mins';
          } else {
            totalTime.value = totalHours.value.toString();
            totalTimeUnit.value = 'Hours';
          }
        }
        isMounted.value = true;
        searchAllowed.value = true;
        isDataReady.value = true;
      });
  };

  const ExtractCourtRoomsAndLocationsInfo = () => {
    for (const jroomAndLocation of courtRoomsAndLocationsJson.value) {
      if (jroomAndLocation.courtRooms.length > 0) {
        const roomAndLocationInfo = {} as courtRoomsAndLocationsInfoType;
        roomAndLocationInfo['text'] =
          jroomAndLocation.name + ' (' + jroomAndLocation.locationId + ')';

        const rooms: roomsInfoType[] = [];
        for (const jroom of jroomAndLocation.courtRooms) {
          const roomInfo = {} as roomsInfoType;
          roomInfo['value'] = jroom.room;
          roomInfo['text'] = jroom.room;
          rooms.push(roomInfo);
        }
        roomAndLocationInfo.value = {} as locationInfoType;
        roomAndLocationInfo.value['Location'] = jroomAndLocation.name;
        roomAndLocationInfo.value['LocationID'] = jroomAndLocation.locationId;
        roomAndLocationInfo.value['Rooms'] = rooms;

        courtRoomsAndLocations.value.push(roomAndLocationInfo);
      }
    }
    courtRoomsAndLocations.value = [
      ..._.sortBy(courtRoomsAndLocations.value, 'text'),
    ];

    Object.assign(
      selectedCourtLocation.value,
      courtRoomsAndLocations.value[0].value
    );
  };

  const searchForCourtList = () => {
    if (!selectedCourtLocation.value.Location) {
      selectedCourtLocationState.value = false;
      searchAllowed.value = true;
    } else {
      Object.assign(courtListLocation, selectedCourtLocation.value.Location);

      courtListLocationID.value = selectedCourtLocation.value.LocationID;
      if (
        selectedCourtRoom.value == 'null' ||
        selectedCourtRoom.value == undefined
      ) {
        selectedCourtRoomState.value = false;
        searchAllowed.value = true;
      } else {
        courtListRoom.value = selectedCourtRoom.value;

        if (
          route.params.location != courtListLocationID.value ||
          route.params.room != courtListRoom.value ||
          route.params.date != shortHandDate.value
        ) {
          router.push({
            name: 'CourtListResult',
            params: {
              location: courtListLocationID.value,
              room: courtListRoom.value,
              date: shortHandDate.value,
            },
          });
        }

        searchAllowed.value = false;
        setTimeout(() => {
          getCourtListDetails();
        }, 50);
      }
    }
  };

  const LocationChanged = () => {
    searchingRequest.value = false;
    selectedCourtRoom.value = '';
    selectedCourtLocationState.value = true;
    syncFlag.value = false;
    syncFlag.value = true;
  };

  const RoomChanged = () => {
    searchingRequest.value = false;
    selectedCourtRoomState.value = true;
  };

  const setTotalTimeForRoom = (hrs, mins) => {
    if (!mins) mins = '0';
    if (!hrs) hrs = '0';
    totalMins.value += parseInt(mins);
    totalHours.value += Math.floor(totalMins.value / 60) + parseInt(hrs);
    totalMins.value %= 60;
  };
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
