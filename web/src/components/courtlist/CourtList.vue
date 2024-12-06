<template>
  <div>
    <b-card
      bg-variant="light"
      v-if="!isLocationDataMounted && !isLocationDataReady"
    >
      <b-overlay :show="true">
        <b-card style="min-height: 100px" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card
      bg-variant="light"
      v-else-if="isLocationDataMounted && !isLocationDataReady"
    >
      <b-card style="min-height: 40px">
        <span v-if="errorCode > 0">
          <span v-if="errorCode == 403">
            You are not authorized to access this page.
          </span>
          <span v-else>
            Server is not responding.
            <b>({{ errorText }} "{{ errorCode }}")</b></span
          >
        </span>
        <span v-else> No Court Location Found. </span>
      </b-card>
      <!-- <b-card>
            <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                Return to Main Page
            </b-button>         
        </b-card>       -->
    </b-card>

    <b-card v-else>
      <b-navbar type="white" variant="white" style="height: 45px">
        <h2 class="ml-1 mt-2">Court List</h2>
        <b-navbar-nav class="ml-auto">
          <b-button
            size="sm"
            :disabled="!searchAllowed"
            @click="BackToPreviousDay"
            variant="primary"
            class="my-2 my-sm-0"
          >
            <b-icon icon="chevron-left"></b-icon>
            Back to Previous Day
          </b-button>
          <b-button
            size="sm"
            :disabled="!searchAllowed"
            @click="JumpToNextDay"
            variant="primary"
            class="ml-2 my-2 my-sm-0"
          >
            Jump to Next Day
            <b-icon icon="chevron-right"></b-icon>
          </b-button>
        </b-navbar-nav>
      </b-navbar>

      <b-row class="mt-2 ml-2">
        <b-col md="4">
          <b-form-group>
            <label for="locationSelect"
              >Location<span class="text-danger">*</span></label
            >
            <b-form-select
              v-model="selectedCourtLocation"
              id="locationSelect"
              :disabled="!searchAllowed"
              :state="selectedCourtLocationState ? null : false"
              @change="LocationChanged"
              :options="courtRoomsAndLocations"
              style="height: 39px"
            >
            </b-form-select>
          </b-form-group>
        </b-col>
        <b-col md="3">
          <label for="datepicker"
            >Date<span class="text-danger">*</span> (YYYY-MM-DD)</label
          >

          <b-input-group class="mb-3">
            <b-form-input
              id="datepicker"
              v-model="selectedDate"
              type="text"
              :disabled="!searchAllowed"
              placeholder="YYYY-MM-DD"
              autocomplete="off"
              :state="selectedDateState ? null : false"
            ></b-form-input>
            <b-input-group-append>
              <b-form-datepicker
                v-model="selectedDate"
                button-only
                :disabled="!searchAllowed"
                right
                locale="en-US"
                @context="onCalenderContext"
              ></b-form-datepicker>
            </b-input-group-append>
          </b-input-group>
        </b-col>
        <b-col md="2">
          <b-form-group class="mr-3">
            <label for="roomSelect"
              >Room<span class="text-danger">*</span></label
            >
            <b-form-select
              v-if="syncFlag"
              v-model="selectedCourtRoom"
              id="roomSelect"
              :disabled="!searchAllowed"
              @change="RoomChanged"
              :options="selectedCourtLocation.Rooms"
              :state="selectedCourtRoomState ? null : false"
              style="height: 39px"
            >
            </b-form-select>
          </b-form-group>
        </b-col>
      </b-row>

      <b-row class="ml-2 mt-2">
        <b-col md="4">
          <b-button
            @click="searchForCourtList"
            :disabled="!searchAllowed"
            variant="primary"
            class="mb-2"
          >
            <b-icon icon="search"></b-icon>
            Search
          </b-button>
        </b-col>
      </b-row>

      <b-card bg-variant="light" v-if="searchingRequest">
        <b-card class="mb-2">
          <b-navbar type="white" variant="white" style="height: 40px">
            <b-navbar-nav>
              <b-nav-text class="text-primary mt-3">
                <h2>{{ fullSelectedDate }}</h2>
              </b-nav-text>

              <b-nav-text class="text-muted ml-4" style="padding-top: 28px">
                <h4>{{ courtListLocation }}</h4>
              </b-nav-text>
              <b-nav-text class="text-muted ml-1" style="padding-top: 29px">
                <h5>({{ courtListLocationID }})</h5>
              </b-nav-text>

              <b-nav-text class="ml-4" style="padding-top: 25px">
                <h3>CourtRoom: {{ courtListRoom }}</h3>
              </b-nav-text>
            </b-navbar-nav>
            <b-navbar-nav class="ml-auto">
              <b-nav-text
                class="mr-1"
                style="font-size: 12px; line-height: 1.4"
              >
                <b-row class="text-primary">
                  Total Cases (<b>{{ totalCases }}</b
                  >)
                  <span
                    style="transform: translate(0, -1px)"
                    class="border text-muted ml-3"
                  >
                    <b> {{ totalTime }}</b> {{ totalTimeUnit }}
                  </span>
                </b-row>
                <b-row class="text-criminal">
                  Criminal (<b>{{ criminalCases }}</b
                  >)
                </b-row>
                <b-row class="text-family">
                  Family (<b>{{ familyCases }}</b
                  >)
                </b-row>
                <b-row class="text-civil">
                  Civil (<b>{{ civilCases }}</b
                  >)
                </b-row>
              </b-nav-text>
            </b-navbar-nav>
          </b-navbar>
        </b-card>

        <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
          <b-overlay :show="true">
            <b-card style="min-height: 100px" />
            <template v-slot:overlay>
              <div>
                <loading-spinner />
                <p id="loading-label">Loading ...</p>
              </div>
            </template>
          </b-overlay>
        </b-card>

        <b-card class="mt-1" v-if="isMounted && !isDataReady">
          <b-card class="ml-3" style="min-height: 40px">
            <span v-if="errorCode > 0">
              Server is not responding.
              <b>({{ errorText }} "{{ errorCode }}")</b>
            </span>
            <span v-else> No appearances. </span>
          </b-card>
        </b-card>

        <b-card no-body v-if="isDataReady">
          <b-row cols="1" class="mx-2 mt-2 mb-4">
            <court-list-layout></court-list-layout>
            <b-card class="my-0" />
          </b-row>
        </b-card>
      </b-card>

      <!-- <b-card class="mb-5" align="right">         
            <b-button id="backToLandingPage" variant="outline-primary text-dark bg-warning" @click="navigateToLandingPage">
                <b-icon-house-door class="mr-1 ml-0" variant="dark" scale="1" ></b-icon-house-door>
                Return to Main Page
            </b-button>
        </b-card> -->
    </b-card>
  </div>
</template>

<script lang="ts">
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCourtListStore } from '@/stores';
  import { CourtRoomsJsonInfoType } from '@/types/common';
  import * as _ from 'underscore';
  import { defineComponent, inject, nextTick, onMounted, ref } from 'vue';
  import { useRoute, useRouter } from 'vue-router';

  import {
    courtRoomsAndLocationsInfoType,
    locationInfoType,
    roomsInfoType,
  } from '@/types/courtlist';
  import { courtListType } from '@/types/courtlist/jsonTypes';
  import { getSingleValue } from '@/utils/utils';
  import { reactive } from 'vue';
  import CourtListLayout from './CourtListLayout.vue';

  export default defineComponent({
    components: {
      CourtListLayout,
    },
    props: ['location', 'room', 'date'],
    setup(props) {
      console.log({ props });

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
      const totalTime = ref('');
      const totalTimeUnit = ref('Hours');
      const courtRoomsAndLocationsJson = ref<CourtRoomsJsonInfoType[]>([]);
      const courtRoomsAndLocations = ref<courtRoomsAndLocationsInfoType[]>([]);
      const selectedDate = ref(new Date().toISOString().substring(0, 10));
      let validSelectedDate = selectedDate.value;
      const fullSelectedDate = ref('');
      const selectedDateState = ref(true);
      const selectedCourtRoom = ref('null');
      const selectedCourtRoomState = ref(true);
      let selectedCourtLocation = reactive({} as locationInfoType);
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
      });

      const getListOfAvailableCourts = () => {
        errorCode.value = 0;
        httpService
          .get<CourtRoomsJsonInfoType[]>('api/location/court-rooms')
          .then(
            (Response) => Response,
            (err) => {
              // $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
              //   title: "An error has occured.",
              //   variant: "danger",
              //   autoHideDelay: 10000,
              // });
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
                searchByRouterParams();
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
              validSelectedDate
          )
          .then(
            (Response) => Response
            // (err) => {
            //   $bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            //     title: "An error has occured.",
            //     variant: "danger",
            //     autoHideDelay: 10000,
            //   });
            //   errorCode.value = err.status;
            //   errorText.value = err.statusText;
            //   console.log(err);
            // }
          )
          .then((data) => {
            if (data) {
              courtListStore.courtListInformation.detailsData = data;
              totalCases.value =
                data.civilCourtList.length + data.criminalCourtList.length;
              criminalCases.value = data.criminalCourtList.length;
              for (const civil of data.civilCourtList) {
                if (
                  civil.activityClassCd == 'F' ||
                  civil.activityClassCd == 'E'
                )
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

              courtListStore.updateCourtList(
                courtListStore.courtListInformation
              );

              if (
                data.civilCourtList.length > 0 ||
                data.criminalCourtList.length > 0
              ) {
                isDataReady.value = true;
              }

              if (totalMins.value > 0 && totalHours.value > 0) {
                totalTime.value = (
                  totalHours.value +
                  totalMins.value / 60
                ).toFixed(1);
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
            roomAndLocationInfo.value['LocationID'] =
              jroomAndLocation.locationId;
            roomAndLocationInfo.value['Rooms'] = rooms;

            courtRoomsAndLocations.value.push(roomAndLocationInfo);
          }
        }
        courtRoomsAndLocations.value = [
          ..._.sortBy(courtRoomsAndLocations.value, 'text'),
        ];

        Object.assign(
          selectedCourtLocation,
          courtRoomsAndLocations.value[0].value
        );
      };

      const getCourtNameById = (locationId) => {
        return courtRoomsAndLocations.value.filter((location) => {
          return location.value['LocationID'] == locationId;
        });
      };

      const getRoomInLocationByRoomNo = (location, roomNo) => {
        return location.value['Rooms'].filter((room) => {
          return room.value == roomNo;
        });
      };

      const searchByRouterParams = () => {
        if (route.params.location && route.params.room && route.params.date) {
          const location = getCourtNameById(route.params.location)[0];
          if (location) {
            Object.assign(selectedCourtLocation, location.value);
            selectedCourtLocationState.value = true;
            selectedDate.value = getSingleValue(route.params.date);
            validSelectedDate = selectedDate.value;
            const room = true; //getRoomInLocationByRoomNo(location, route.params.room)[0];
            if (room) {
              selectedCourtRoom.value = getSingleValue(route.params.room);
              selectedCourtRoomState.value = true;
              nextTick().then(() => {
                searchForCourtList();
              });
            } else {
              selectedCourtRoom.value = 'null';
              selectedCourtRoomState.value = false;
              searchAllowed.value = true;
            }
          } else {
            Object.assign(
              selectedCourtLocation,
              courtRoomsAndLocations.value[0].value
            );
            selectedCourtLocationState.value = false;
            searchAllowed.value = true;
          }
        }
      };

      const onCalenderContext = (datePicked) => {
        searchingRequest.value = false;
        if (datePicked.selectedYMD) {
          validSelectedDate = datePicked.selectedYMD;
          fullSelectedDate.value = datePicked.selectedFormatted;
        }
      };

      const BackToPreviousDay = () => {
        if (!checkDateInValid()) {
          searchAllowed.value = false;
          const olddate = seperateIsoDate(selectedDate.value);
          const date = new Date(
            olddate.year,
            olddate.month - 1,
            olddate.day,
            0,
            0,
            0,
            0
          );
          date.setDate(date.getDate() - 1);
          selectedDate.value = date.toISOString().substring(0, 10);
          nextTick().then(() => {
            searchForCourtList();
          });
        }
      };

      const JumpToNextDay = () => {
        if (!checkDateInValid()) {
          searchAllowed.value = false;
          const olddate = seperateIsoDate(selectedDate.value);
          const date = new Date(
            olddate.year,
            olddate.month - 1,
            olddate.day,
            0,
            0,
            0,
            0
          );
          date.setDate(date.getDate() + 1);
          selectedDate.value = date.toISOString().substring(0, 10);
          nextTick().then(() => {
            searchForCourtList();
          });
        }
      };

      const checkDateInValid = () => {
        if (isValidDate(selectedDate.value)) {
          selectedDateState.value = true;
          return false;
        } else {
          selectedDateState.value = false;
          return true;
        }
      };

      const isValidDate = (dateString) => {
        if (!/^\d{4}-\d{1,2}-\d{1,2}$/.test(dateString)) return false;

        const seperatedDate = seperateIsoDate(dateString);
        const day = seperatedDate.day;
        const month = seperatedDate.month;
        const year = seperatedDate.year;

        if (year < 1800 || year > 3000 || month == 0 || month > 12)
          return false;

        const monthLength = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

        // Adjust for leap years
        if (year % 400 == 0 || (year % 100 != 0 && year % 4 == 0))
          monthLength[1] = 29;

        return day > 0 && day <= monthLength[month - 1];
      };

      const seperateIsoDate = (dateString) => {
        const seperatedDate = { day: 0, month: 0, year: 0 };
        const parts = dateString.split('-');
        seperatedDate.day = parseInt(parts[2], 10);
        seperatedDate.month = parseInt(parts[1], 10);
        seperatedDate.year = parseInt(parts[0], 10);
        return seperatedDate;
      };

      const searchForCourtList = () => {
        if (!selectedCourtLocation.Location) {
          selectedCourtLocationState.value = false;
          searchAllowed.value = true;
        } else {
          if (checkDateInValid()) {
            searchAllowed.value = true;
          } else {
            Object.assign(courtListLocation, selectedCourtLocation.Location);
            courtListLocationID.value = selectedCourtLocation.LocationID;

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
                route.params.date != validSelectedDate
              ) {
                // route.params.location = courtListLocationID.value;
                // route.params.room = courtListRoom.value;
                // route.params.date = validSelectedDate.value;
                router.push({
                  name: 'CourtListResult',
                  params: {
                    location: courtListLocationID.value,
                    room: courtListRoom.value,
                    date: validSelectedDate,
                  },
                });
              }
              searchAllowed.value = false;
              setTimeout(() => {
                getCourtListDetails();
              }, 50);
            }
          }
        }
      };

      const LocationChanged = () => {
        searchingRequest.value = false;
        selectedCourtRoom.value = 'null';
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

      const navigateToLandingPage = () => {
        router.push({ name: 'Home' });
      };

      // Return the reactive variables to the template
      return {
        errorCode,
        errorText,
        isDataReady,
        isMounted,
        searchingRequest,
        isLocationDataReady,
        isLocationDataMounted,
        searchAllowed,
        syncFlag,
        totalCases,
        criminalCases,
        familyCases,
        civilCases,
        totalHours,
        totalMins,
        totalTime,
        totalTimeUnit,
        courtRoomsAndLocationsJson,
        courtRoomsAndLocations,
        selectedDate,
        validSelectedDate,
        fullSelectedDate,
        selectedDateState,
        selectedCourtRoom,
        selectedCourtRoomState,
        selectedCourtLocation,
        selectedCourtLocationState,
        courtListLocation,
        courtListLocationID,
        courtListRoom,
        BackToPreviousDay,
        onCalenderContext,
        JumpToNextDay,
        LocationChanged,
        RoomChanged,
        navigateToLandingPage,
        getRoomInLocationByRoomNo,
        searchForCourtList,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
