<template>
  <div>
    <b-card bg-variant="light" v-if="!isLocationDataMounted && !isLocationDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px;" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card bg-variant="light" v-else-if="isLocationDataMounted && !isLocationDataReady">
      <b-card style="min-height: 40px;">
        <span v-if="errorCode > 0">
          <span v-if="errorCode == 403"> You are not authorized to access this page. </span>
          <span v-else>
            Server is not responding. <b>({{ errorText }} "{{ errorCode }}")</b></span
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
      <b-navbar type="white" variant="white" style="height:45px">
        <h2 class="ml-1 mt-2">Court List</h2>
        <b-navbar-nav class="ml-auto">
          <b-button
            size="sm"
            :disabled="!searchAllowed"
            @click="BackToPreviouDay"
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
            <label for="locationSelect">Location<span class="text-danger">*</span></label>
            <b-form-select
              v-model="selectedCourtLocation"
              id="locationSelect"
              :disabled="!searchAllowed"
              :state="selectedCourtLocationState ? null : false"
              @change="LocationChanged"
              :options="courtRoomsAndLocations"
              style="height:39px"
            >
            </b-form-select>
          </b-form-group>
        </b-col>
        <b-col md="3">
          <label for="datepicker">Date<span class="text-danger">*</span> (YYYY-MM-DD)</label>

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
            <label for="roomSelect">Room<span class="text-danger">*</span></label>
            <b-form-select
              v-if="syncFlag"
              v-model="selectedCourtRoom"
              id="roomSelect"
              :disabled="!searchAllowed"
              @change="RoomChanged"
              :options="selectedCourtLocation.Rooms"
              :state="selectedCourtRoomState ? null : false"
              style="height:39px"
            >
            </b-form-select>
          </b-form-group>
        </b-col>
      </b-row>

      <b-row class="ml-2 mt-2">
        <b-col md="4">
          <b-button @click="searchForCourtList" :disabled="!searchAllowed" variant="primary" class="mb-2">
            <b-icon icon="search"></b-icon>
            Search
          </b-button>
        </b-col>
      </b-row>

      <b-card bg-variant="light" v-if="searchingRequest">
        <b-card class="mb-2">
          <b-navbar type="white" variant="white" style="height:40px;">
            <b-navbar-nav>
              <b-nav-text class="text-primary mt-3">
                <h2>{{ fullSelectedDate }}</h2>
              </b-nav-text>

              <b-nav-text class="text-muted ml-4" style="padding-top:28px">
                <h4>{{ courtListLocation }}</h4>
              </b-nav-text>
              <b-nav-text class="text-muted ml-1" style="padding-top:29px">
                <h5>({{ courtListLocationID }})</h5>
              </b-nav-text>

              <b-nav-text class=" ml-4" style="padding-top:25px">
                <h3>CourtRoom: {{ courtListRoom }}</h3>
              </b-nav-text>
            </b-navbar-nav>
            <b-navbar-nav class="ml-auto">
              <b-nav-text class=" mr-1" style="font-size:12px; line-height: 1.4;">
                <b-row class="text-primary">
                  Total Cases (<b>{{ totalCases }}</b
                  >)
                  <span style="transform: translate(0,-1px);" class="border text-muted ml-3">
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
            <b-card style="min-height: 100px;" />
            <template v-slot:overlay>
              <div>
                <loading-spinner />
                <p id="loading-label">Loading ...</p>
              </div>
            </template>
          </b-overlay>
        </b-card>

        <b-card class="mt-1" v-if="isMounted && !isDataReady">
          <b-card class="ml-3" style="min-height: 40px;">
            <span v-if="errorCode > 0">
              Server is not responding. <b>({{ errorText }} "{{ errorCode }}")</b>
            </span>
            <span v-else> No appearances. </span>
          </b-card>
        </b-card>

        <b-card no-body v-if="isDataReady">
          <b-row cols="1" class="mx-2 mt-2 mb-4">
            <court-list-layout />
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
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import * as _ from "underscore";

import CourtListLayout from "@components/courtlist/CourtListLayout.vue";
import {
  courtListInformationInfoType,
  roomsInfoType,
  courtRoomsAndLocationsInfoType,
  locationInfoType,
} from "@/types/courtlist";
import "@store/modules/CourtListInformation";
import { CourtRoomsJsonInfoType } from "@/types/common";
const courtListState = namespace("CourtListInformation");

const commonState = namespace("CommonInformation");
import "@store/modules/CourtListInformation";

@Component({
  components: {
    CourtListLayout,
  },
})
export default class CourtList extends Vue {
  @courtListState.State
  public courtListInformation!: courtListInformationInfoType;

  @courtListState.Action
  public UpdateCourtList!: (newCourtListInformation: courtListInformationInfoType) => void;

  @commonState.Action
  public UpdateCourtRoomsAndLocations!: (newCourtRoomsAndLocations) => void;

  errorCode = 0;
  errorText = "";
  isDataReady = false;
  isMounted = false;
  searchingRequest = false;
  isLocationDataReady = false;
  isLocationDataMounted = false;
  searchAllowed = true;
  syncFlag = true;
  totalCases = 0;
  criminalCases = 0;
  familyCases = 0;
  civilCases = 0;
  totalHours = 0;
  totalMins = 0;
  totalTime = "";
  totalTimeUnit = "Hours";
  courtRoomsAndLocationsJson: CourtRoomsJsonInfoType[] = [];
  courtRoomsAndLocations: courtRoomsAndLocationsInfoType[] = [];

  selectedDate = new Date().toISOString().substring(0, 10);
  validSelectedDate = this.selectedDate;
  fullSelectedDate = "";
  selectedDateState = true;
  selectedCourtRoom = "null";
  selectedCourtRoomState = true;
  selectedCourtLocation = {} as locationInfoType;
  selectedCourtLocationState = true;
  courtListLocation = "Vancouver";
  courtListLocationID = "4801";
  courtListRoom = "005";

  mounted() {
    this.getListOfAvailableCourts();
  }

  public getListOfAvailableCourts(): void {
    this.errorCode = 0;
    this.$http
      .get("api/location/court-rooms")
      .then(
        (Response) => Response.json(),
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          this.errorCode = err.status;
          this.errorText = err.statusText;
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          this.courtRoomsAndLocationsJson = data;
          this.UpdateCourtRoomsAndLocations(data);
          this.ExtractCourtRoomsAndLocationsInfo();
          if (this.courtRoomsAndLocations.length > 0) {
            this.isLocationDataReady = true;
            this.searchByRouterParams();
          }
        }
        this.isLocationDataMounted = true;
      });
  }

  public getCourtListDetails(): void {
    this.isDataReady = false;
    this.isMounted = false;
    this.searchingRequest = true;
    this.totalCases = 0;
    this.criminalCases = 0;
    this.familyCases = 0;
    this.civilCases = 0;
    this.totalHours = 0;
    this.totalMins = 0;
    this.totalTime = "0";
    this.totalTimeUnit = "Hours";
    this.errorCode = 0;

    this.$http
      .get(
        "api/courtlist/court-list?agencyId=" +
          this.courtListLocationID +
          "&roomCode=" +
          this.courtListRoom +
          "&proceeding=" +
          this.validSelectedDate
      )
      .then(
        (Response) => Response.json(),
        (err) => {
          this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
            title: "An error has occured.",
            variant: "danger",
            autoHideDelay: 10000,
          });
          this.errorCode = err.status;
          this.errorText = err.statusText;
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          this.courtListInformation.detailsData = data;
          this.totalCases = data.civilCourtList.length + data.criminalCourtList.length;
          this.criminalCases = data.criminalCourtList.length;
          for (const civil of data.civilCourtList) {
            if (civil.activityClassCd == "F" || civil.activityClassCd == "E") this.familyCases++;
            else this.civilCases++;
            this.setTotalTimeForRoom(civil.estimatedTimeHour, civil.estimatedTimeMin);
          }

          for (const criminal of data.criminalCourtList) {
            this.setTotalTimeForRoom(criminal.estimatedTimeHour, criminal.estimatedTimeMin);
          }

          this.UpdateCourtList(this.courtListInformation);

          if (data.civilCourtList.length > 0 || data.criminalCourtList.length > 0) {
            this.isDataReady = true;
          }

          if (this.totalMins > 0 && this.totalHours > 0) {
            this.totalTime = (this.totalHours + this.totalMins / 60).toFixed(1);
            this.totalTimeUnit = "Hours";
          } else if (this.totalMins > 0 && this.totalHours == 0) {
            this.totalTime = this.totalMins.toString();
            this.totalTimeUnit = "Mins";
          } else {
            this.totalTime = this.totalHours.toString();
            this.totalTimeUnit = "Hours";
          }
        }
        this.isMounted = true;
        this.searchAllowed = true;
      });
  }

  public ExtractCourtRoomsAndLocationsInfo() {
    for (const jroomAndLocation of this.courtRoomsAndLocationsJson) {
      if (jroomAndLocation.courtRooms.length > 0) {
        const roomAndLocationInfo = {} as courtRoomsAndLocationsInfoType;
        roomAndLocationInfo["text"] = jroomAndLocation.name + " (" + jroomAndLocation.locationId + ")";

        const rooms: roomsInfoType[] = [];
        for (const jroom of jroomAndLocation.courtRooms) {
          const roomInfo = {} as roomsInfoType;
          roomInfo["value"] = jroom.room;
          roomInfo["text"] = jroom.room;
          rooms.push(roomInfo);
        }
        roomAndLocationInfo.value = {} as locationInfoType;
        roomAndLocationInfo.value["Location"] = jroomAndLocation.name;
        roomAndLocationInfo.value["LocationID"] = jroomAndLocation.locationId;
        roomAndLocationInfo.value["Rooms"] = rooms;

        this.courtRoomsAndLocations.push(roomAndLocationInfo);
      }
    }
    this.courtRoomsAndLocations = _.sortBy(this.courtRoomsAndLocations, "text");
    this.selectedCourtLocation = this.courtRoomsAndLocations[0].value;
  }

  public getCourtNameById(locationId) {
    return this.courtRoomsAndLocations.filter((location) => {
      return location.value["LocationID"] == locationId;
    });
  }

  public getRoomInLocationByRoomNo(location, roomNo) {
    return location.value["Rooms"].filter((room) => {
      return room.value == roomNo;
    });
  }

  public searchByRouterParams() {
    if (this.$route.params.location && this.$route.params.room && this.$route.params.date) {
      const location = this.getCourtNameById(this.$route.params.location)[0];
      if (location) {
        this.selectedCourtLocation = location.value;
        this.selectedCourtLocationState = true;
        this.selectedDate = this.$route.params.date;
        const room = true; //this.getRoomInLocationByRoomNo(location, this.$route.params.room)[0];
        if (room) {
          this.selectedCourtRoom = this.$route.params.room;
          this.selectedCourtRoomState = true;
          Vue.nextTick().then(() => {
            this.searchForCourtList();
          });
        } else {
          this.selectedCourtRoom = "null";
          this.selectedCourtRoomState = false;
          this.searchAllowed = true;
        }
      } else {
        this.selectedCourtLocation = this.courtRoomsAndLocations[0].value;
        this.selectedCourtLocationState = false;
        this.searchAllowed = true;
      }
    }
  }

  public onCalenderContext(datePicked) {
    this.searchingRequest = false;
    if (datePicked.selectedYMD) {
      this.validSelectedDate = datePicked.selectedYMD;
      this.fullSelectedDate = datePicked.selectedFormatted;
    }
  }

  public BackToPreviouDay() {
    if (!this.checkDateInValid()) {
      this.searchAllowed = false;
      const olddate = this.seperateIsoDate(this.selectedDate);
      const date = new Date(olddate.year, olddate.month - 1, olddate.day, 0, 0, 0, 0);
      date.setDate(date.getDate() - 1);
      this.selectedDate = date.toISOString().substring(0, 10);
      Vue.nextTick().then(() => {
        this.searchForCourtList();
      });
    }
  }

  public JumpToNextDay() {
    if (!this.checkDateInValid()) {
      this.searchAllowed = false;
      const olddate = this.seperateIsoDate(this.selectedDate);
      const date = new Date(olddate.year, olddate.month - 1, olddate.day, 0, 0, 0, 0);
      date.setDate(date.getDate() + 1);
      this.selectedDate = date.toISOString().substring(0, 10);
      Vue.nextTick().then(() => {
        this.searchForCourtList();
      });
    }
  }

  public checkDateInValid() {
    if (this.isValidDate(this.selectedDate)) {
      this.selectedDateState = true;
      return false;
    } else {
      this.selectedDateState = false;
      return true;
    }
  }

  public isValidDate(dateString) {
    if (!/^\d{4}-\d{1,2}-\d{1,2}$/.test(dateString)) return false;

    const seperatedDate = this.seperateIsoDate(dateString);
    const day = seperatedDate.day;
    const month = seperatedDate.month;
    const year = seperatedDate.year;

    if (year < 1800 || year > 3000 || month == 0 || month > 12) return false;

    const monthLength = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    // Adjust for leap years
    if (year % 400 == 0 || (year % 100 != 0 && year % 4 == 0)) monthLength[1] = 29;

    return day > 0 && day <= monthLength[month - 1];
  }

  public seperateIsoDate(dateString) {
    const seperatedDate = { day: 0, month: 0, year: 0 };
    const parts = dateString.split("-");
    seperatedDate.day = parseInt(parts[2], 10);
    seperatedDate.month = parseInt(parts[1], 10);
    seperatedDate.year = parseInt(parts[0], 10);
    return seperatedDate;
  }

  public searchForCourtList() {
    if (!this.selectedCourtLocation.Location) {
      this.selectedCourtLocationState = false;
      this.searchAllowed = true;
    } else {
      if (this.checkDateInValid()) {
        this.searchAllowed = true;
      } else {
        this.courtListLocation = this.selectedCourtLocation.Location;
        this.courtListLocationID = this.selectedCourtLocation.LocationID;

        if (this.selectedCourtRoom == "null" || this.selectedCourtRoom == undefined) {
          this.selectedCourtRoomState = false;
          this.searchAllowed = true;
        } else {
          this.courtListRoom = this.selectedCourtRoom;

          if (
            this.$route.params.location != this.courtListLocationID ||
            this.$route.params.room != this.courtListRoom ||
            this.$route.params.date != this.validSelectedDate
          ) {
            this.$route.params.location = this.courtListLocationID;
            this.$route.params.room = this.courtListRoom;
            this.$route.params.date = this.validSelectedDate;
            this.$router.push({ name: "CourtListResult" });
          }
          this.searchAllowed = false;
          setTimeout(() => {
            this.getCourtListDetails();
          }, 50);
        }
      }
    }
  }

  public LocationChanged() {
    this.searchingRequest = false;
    this.selectedCourtRoom = "null";
    this.selectedCourtLocationState = true;
    this.syncFlag = false;
    this.syncFlag = true;
  }

  public RoomChanged() {
    this.searchingRequest = false;
    this.selectedCourtRoomState = true;
  }

  public setTotalTimeForRoom(hrs, mins) {
    if (!mins) mins = "0";
    if (!hrs) hrs = "0";
    this.totalMins += parseInt(mins);
    this.totalHours += Math.floor(this.totalMins / 60) + parseInt(hrs);
    this.totalMins %= 60;
  }

  public navigateToLandingPage() {
    this.$router.push({ name: "Home" });
  }
}
</script>

<style scoped>
.card {
  border: white;
}
</style>
