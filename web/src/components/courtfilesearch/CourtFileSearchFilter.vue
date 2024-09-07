<template>
  <b-row body-class="py-0">
    <b-col>
      <b-card body-class="py-1">
        <b-form @submit.prevent="handleSubmit">
          <!-- Division -->
          <b-form-group label="Division:" label-cols="2" label-align="right">
            <b-button-group>
              <b-button v-for="(button, index) in divisionOptions" :value="button.value" :key="index"
                @click="() => handleDivisionChange(button.value, button.flag)"
                :variant="searchCriteria[button.flag] ? 'primary' : 'outline-primary'">
                {{ button.label }}
              </b-button>
            </b-button-group>
          </b-form-group>
          <!-- CRIMINAL OR CIVIL -->
          <div v-show="searchCriteria.isCriminal || searchCriteria.isCivil">
            <!-- File Number or Party Name -->
            <b-form-group label="File Number or Party Name:" label-cols="2" label-align="right">
              <b-form-radio-group v-model="searchCriteria.selectedFileNoOrParty" name="file-radio-group">
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'file' }">
                  <b-form-radio class=" mt-2" value="file"> File Number </b-form-radio>
                  <b-row class="flex-grow-1" v-if="searchCriteria.selectedFileNoOrParty === 'file'">
                    <b-col md="3">
                      <b-form-input placeholder="e.g. 99999999" v-model="searchCriteria.fileNumber"></b-form-input>
                    </b-col>
                    <b-col md="6" offset-md="3" v-if="searchCriteria.isCriminal">
                      <b-card bg-variant="light" class="ml-1" body-class="p-2">
                        <b-form-group class="mb-0" label="Optional..." label-align="center">
                          <b-form-group label-cols="3" label="Prefix" label-for="prefix">
                            <b-form-input id="prefix" placeholder="AH" v-model="searchCriteria.prefix"></b-form-input>
                          </b-form-group>
                          <b-form-group label-cols="3" label="Seq Num" label-for="seqNum">
                            <b-form-input id="seqNum" placeholder="1" v-model="searchCriteria.seqNum"></b-form-input>
                          </b-form-group>
                          <b-form-group label-cols="3" label="Type Ref" label-for="typeRef">
                            <b-form-input id="typeRef" placeholder="B" v-model="searchCriteria.typeRef"></b-form-input>
                          </b-form-group>
                        </b-form-group>
                      </b-card>
                    </b-col>
                  </b-row>
                </div>
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'surname' }">
                  <b-form-radio class="mt-2" value="surname"> Surname </b-form-radio>
                  <b-row v-if="searchCriteria.selectedFileNoOrParty === 'surname'">
                    <b-col>
                      <b-form-input v-model="searchCriteria.surname"></b-form-input>
                    </b-col>
                    <b-col>
                      <b-form-input placeholder="Given Name" v-model="searchCriteria.givenName"></b-form-input>
                    </b-col>
                  </b-row>
                </div>
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'org' }">
                  <b-form-radio class="mt-2" value="org"> Organisation </b-form-radio>
                  <b-row v-if="searchCriteria.selectedFileNoOrParty === 'org'">
                    <b-col>
                      <b-form-input placeholder="e.g. MegaCorp Inc." v-model="searchCriteria.org"></b-form-input>
                    </b-col>
                  </b-row>
                </div>
              </b-form-radio-group>
            </b-form-group>
            <!-- Criminal Class -->
            <b-form-group v-if="searchCriteria.isCriminal" label="Class:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="2">
                  <b-form-select :options="criminalClassOptions" v-model="searchCriteria.class"></b-form-select>
                </b-col>
              </b-row>
            </b-form-group>
            <!-- Civil Class -->
            <b-form-group v-if="searchCriteria.isCivil" label="Class:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="2">
                  <b-form-select :options="civilClassOptions" v-model="searchCriteria.class"></b-form-select>
                </b-col>
              </b-row>
            </b-form-group>
          </div>
          <!-- OTHER -->
          <div v-show="searchCriteria.isOther">
            <b-form-group label="Type:" label-cols="2" label-align="right">
              <b-form-radio-group v-model="searchCriteria.selectedType" name="other-radio-group">
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedType === 'courtOfAppeal' }">
                  <b-form-radio class="mt-2" value="courtOfAppeal"> Court of Appeal </b-form-radio>
                  <b-row v-if="searchCriteria.selectedType === 'courtOfAppeal'">
                    <b-col>
                      <b-form-input v-model="searchCriteria.courtOfAppeal" placeholder="e.g. 99999999"></b-form-input>
                    </b-col>
                  </b-row>
                </div>
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedType === 'otrOrSealed' }">
                  <b-form-radio class="mt-2" value="otrOrSealed"> OTR / Sealed </b-form-radio>
                  <b-row v-if="searchCriteria.selectedType === 'otrOrSealed'">
                    <b-col>
                      <b-form-input v-model="searchCriteria.otrOrSealed" placeholder="e.g. 99999999"></b-form-input>
                    </b-col>
                  </b-row>
                </div>
                <div class="radio-container p-2 rounded d-flex"
                  :class="{ 'bg-info': searchCriteria.selectedType === 'ceremony' }">
                  <b-form-radio class="mt-2" value="ceremony"> Ceremony </b-form-radio>
                </div>
              </b-form-radio-group>
            </b-form-group>
            <b-form-group label="Style of Cause:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="5">
                  <b-form-input v-model="searchCriteria.styleOfCause" placeholder="eg. R vs. M"></b-form-input>
                </b-col>
              </b-row>
            </b-form-group>
            <b-form-group label="Judge:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="3">
                  <b-form-input v-model="searchCriteria.judge" placeholder="eg. Justice Smith"></b-form-input>
                </b-col>
              </b-row>
            </b-form-group>
            <b-form-group label="Room:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="3">
                  <b-form-input v-model="searchCriteria.room" placeholder="eg. 406"></b-form-input>
                </b-col>
              </b-row>
            </b-form-group>
            <b-form-group label="Date of Proceeding:" label-cols="2" label-align="right">
              <b-row>
                <b-col cols="4">
                  <b-input-group v-for="(date, index) in searchCriteria.proceedingDates" class="mb-3" :key="index">
                    <b-form-input :id="`datepicker + ${index}`" type="text" placeholder="YYYY-MM-DD"
                      v-model="searchCriteria.proceedingDates[index]"></b-form-input>
                    <b-input-group-append>
                      <b-form-datepicker v-model="searchCriteria.proceedingDates[index]" button-only right
                        locale="en-US"></b-form-datepicker>
                      <b-button variant="outline-secondary text-dark" @click="() => handleDeleteDate(index)">
                        <b-icon-x-circle font-scale="1"></b-icon-x-circle>
                      </b-button>
                    </b-input-group-append>
                  </b-input-group>
                  <b-button variant="outline-primary text-dark" @click="handleAddDate">
                    Add Date
                  </b-button>
                </b-col>
              </b-row>
            </b-form-group>
          </div>
          <!-- Registry -->
          <b-form-group label="Initiating Registry:" label-cols="2" label-align="right">
            <b-row>
              <b-col cols="3">
                <b-form-select v-model="searchCriteria.registry" :options="courtRoomsAndLocations"></b-form-select>
              </b-col>
            </b-row>
          </b-form-group>
          <b-row>
            <b-col offset-md="2">
              <b-button variant="primary" type="submit">
                {{ searchCriteria.isOther ? 'Enter Details of Request' : 'Search' }}
              </b-button>
            </b-col>
          </b-row>
        </b-form>
      </b-card>
    </b-col>
  </b-row>
</template>
<script lang="ts">
import { CourtRoomsJsonInfoType } from "@/types/common";
import { CourtFileSearchCriteria } from "@/types/courtFileSearch";
import { roomsInfoType } from "@/types/courtlist";
import { Component, Vue } from "vue-property-decorator";

@Component
export default class CourtFileSearchFilter extends Vue {
  selectedDivision = 'criminal';
  selectedClass = 'adult';
  selectedFileNoOrParty = "file";

  divisionOptions = [
    {
      value: 'criminal',
      label: 'Criminal',
      flag: 'isCriminal'
    },
    {
      value: 'civil',
      label: 'Civil',
      flag: 'isCivil'
    },
    {
      value: 'other',
      label: 'Other',
      flag: 'isOther'
    },
  ];

  criminalClassOptions = [
    { value: "adult", text: "Adult" }
  ];
  civilClassOptions = [
    { value: "family", text: "Family" }
  ];

  errorCode = 0;
  errorText = "";
  courtRoomsAndLocationsJson: CourtRoomsJsonInfoType[] = [];
  courtRoomsAndLocations: roomsInfoType[] = [];
  isLocationDataMounted = false;
  isLocationDataReady = false;
  selectedCourt = "";

  searchCriteria: CourtFileSearchCriteria = {
    division: 'criminal',
    isCriminal: true,
    isCivil: false,
    isOther: false,
    selectedFileNoOrParty: 'file',
    class: "adult",
    selectedType: "courtOfAppeal",
    proceedingDates: [
      new Date().toISOString().substring(0, 10),
      new Date().toISOString().substring(0, 10),
      new Date().toISOString().substring(0, 10),
    ],
    registry: ""
  };

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
          this.errorCode = err.status;
          this.errorText = err.statusText;
          if (this.errorCode != 401) {
            this.$bvToast.toast(`Error - ${this.errorCode} - ${this.errorText}`, {
              title: "An error has occured.",
              variant: "danger",
              autoHideDelay: 10000,
            });
          }
          console.log(this.errorCode);
        }
      )
      .then((courtRooms: CourtRoomsJsonInfoType[]) => {
        this.isLocationDataMounted = true;
        const sortedCourtRooms = courtRooms
          .filter(cr => cr.courtRooms.length > 0)
          .sort((a, b) => a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase()))

        sortedCourtRooms.map(cr => {
          this.courtRoomsAndLocations.push({
            text: cr.name,
            value: cr.code
          })
        })

        this.isLocationDataReady = this.courtRoomsAndLocations.length > 0;
        this.selectedCourt = this.courtRoomsAndLocations[0].value;
      });
  }

  public handleDivisionChange(targetDivision: string, divisionFlag: string) {
    this.searchCriteria.division = targetDivision;
    this.searchCriteria.isCriminal = false;
    this.searchCriteria.isCivil = false;
    this.searchCriteria.isOther = false;
    this.searchCriteria[divisionFlag] = true;

    if (this.searchCriteria.isCriminal) {
      this.searchCriteria.selectedFileNoOrParty = 'file';
      this.searchCriteria.fileNumber = undefined;
      this.searchCriteria.prefix = undefined;
      this.searchCriteria.seqNum = undefined;
      this.searchCriteria.typeRef = undefined;
      this.searchCriteria.surname = undefined;
      this.searchCriteria.givenName = undefined;
      this.searchCriteria.org = undefined;
      this.searchCriteria.class = 'adult'
    }

    if (this.searchCriteria.isCivil) {
      this.searchCriteria.selectedFileNoOrParty = 'file';
      this.searchCriteria.fileNumber = undefined;
      this.searchCriteria.surname = undefined;
      this.searchCriteria.givenName = undefined;
      this.searchCriteria.org = undefined;
      this.searchCriteria.class = 'family'
    }

    if (this.searchCriteria.isOther) {
      this.searchCriteria.selectedType = 'courtOfAppeal';
      this.searchCriteria.courtOfAppeal = undefined;
      this.searchCriteria.otrOrSealed = undefined;
      this.searchCriteria.styleOfCause = undefined;
      this.searchCriteria.judge = undefined;
      this.searchCriteria.room = undefined;
      this.searchCriteria.proceedingDates = [new Date().toISOString().substring(0, 10)];
    }
  }

  public handleDeleteDate(deletedIndex: number) {
    this.searchCriteria.proceedingDates = this.searchCriteria.proceedingDates.filter((d, i) => i !== deletedIndex);
  }

  public handleAddDate() {
    this.searchCriteria.proceedingDates.push(new Date().toISOString().substring(0, 10));
  }

  public handleSubmit() {
    console.log(this.searchCriteria);
  }
}
</script>

<style scoped>
.card {
  border: white;
}

.btn-group.active {
  color: blue
}

.transparent {
  background-color: transparent;
}
</style>