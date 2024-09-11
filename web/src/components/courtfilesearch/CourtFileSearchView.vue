<template>
  <b-card no-body bg-variant="white">
    <b-card bg-variant="light" v-if="!isLookupDataMounted && !isLookupDataReady">
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
    <b-card bg-variant="light" v-else-if="isLookupDataMounted && !isLookupDataReady">
      <b-card style="min-height: 40px;">
        <span v-if="errorCode > 0">
          <span v-if="errorCode == 403"> You are not authorized to access this page. </span>
          <span v-else>
            Server is not responding. <b>({{ errorText }} "{{ errorCode }}")</b></span>
        </span>
        <span v-else> No Court File Search Found. </span>
      </b-card>
    </b-card>
    <b-card v-else>
      <b-navbar type="white" variant="white">
        <h2 class="mb-0">Court File Search</h2>
      </b-navbar>
      <b-row class="mb-2" body-class="py-0">
        <b-col>
          <b-card v-if="isLookupDataMounted && isLookupDataReady" body-class="py-1">
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
                        <b-col md="3" class="text-right">
                          <b-form-input placeholder="e.g. 99999999" v-model="searchCriteria.fileNumber"></b-form-input>
                          <span class="text-danger" v-show="errors.isMissingFileNoOrParty">Field required</span>
                        </b-col>
                        <b-col md="6" offset-md="3" v-if="searchCriteria.isCriminal">
                          <b-card bg-variant="light" class="ml-1" body-class="p-2">
                            <b-form-group class="mb-0" label="Optional..." label-align="center">
                              <b-form-group label-cols="3" label="Prefix" label-for="prefix">
                                <b-form-input id="prefix" placeholder="AH"
                                  v-model="searchCriteria.prefix"></b-form-input>
                              </b-form-group>
                              <b-form-group label-cols="3" label="Seq Num" label-for="seqNum">
                                <b-form-input id="seqNum" placeholder="1"
                                  v-model="searchCriteria.seqNum"></b-form-input>
                              </b-form-group>
                              <b-form-group label-cols="3" label="Type Ref" label-for="typeRef">
                                <b-form-input id="typeRef" placeholder="B"
                                  v-model="searchCriteria.typeRef"></b-form-input>
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
                        <b-col class="text-right">
                          <b-form-input v-model="searchCriteria.surname"></b-form-input>
                          <span class="text-danger" v-show="errors.isMissingSurname">Field required</span>
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
                        <b-col class="text-right">
                          <b-form-input placeholder="e.g. MegaCorp Inc." v-model="searchCriteria.org"></b-form-input>
                          <span class="text-danger" v-show="errors.isMissingOrg">Field required</span>
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
                          <b-form-input v-model="searchCriteria.courtOfAppeal"
                            placeholder="e.g. 99999999"></b-form-input>
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
                    <b-form-select v-model="searchCriteria.registry" :options="courtRooms"></b-form-select>
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
      <court-file-search-result :isLookupDataMounted="isLookupDataMounted" :isLookupDataReady="isLookupDataReady"
        :courtRooms="courtRooms" :levels="levels" :classes="classes">
      </court-file-search-result>
    </b-card>
  </b-card>
</template>
<script lang="ts">
import { CourtFileSearchCriteria } from "@/types/courtFileSearch";
import CourtFileSearchResult from "@components/courtfilesearch/CourtFileSearchResult.vue";
import { roomsInfoType } from "@/types/courtlist";
import { Component, Vue } from "vue-property-decorator";
import { CourtRoomsJsonInfoType, LookupCode } from "@/types/common";

@Component({
  components: {
    CourtFileSearchResult
  }
})
export default class CourtFileSearchView extends Vue {
  errorCode = 0;
  errorText = "";
  courtRooms: roomsInfoType[] = [];
  levels: LookupCode[] = [];
  classes: LookupCode[] = [];

  isLookupDataMounted = false;
  isLookupDataReady = false;
  isSearching = false;

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
    ],
    registry: "",
  };

  errors = {
    isMissingFileNoOrParty: false,
    isMissingSurname: false,
    isMissingOrg: false
  };

  mounted() {
    this.loadLookups();
  }

  public async loadLookups(): Promise<void> {
    try {
      const [courtRoomsResp, levelsResp, classesResp] = await Promise.all([
        this.$http.get("api/location/court-rooms"),
        this.$http.get("api/codes/court/levels"),
        this.$http.get("api/codes/court/classes"),
      ]);

      this.loadCourtRooms(courtRoomsResp.data as CourtRoomsJsonInfoType[]);
      this.levels = levelsResp.data as LookupCode[];
      this.classes = classesResp.data as LookupCode[];

      this.isLookupDataReady = true;
    } catch (err) {
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
    } finally {
      this.isLookupDataMounted = true;
    }
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

    this.resetErrors();

    // Validate
    if (this.searchCriteria.isCriminal || this.searchCriteria.isCivil) {
      this.errors.isMissingFileNoOrParty = this.searchCriteria.selectedFileNoOrParty === 'file' && !this.searchCriteria.fileNumber;
      this.errors.isMissingSurname = this.searchCriteria.selectedFileNoOrParty === 'surname' && !this.searchCriteria.surname;
      this.errors.isMissingOrg = this.searchCriteria.selectedFileNoOrParty === 'org' && !this.searchCriteria.org;
    }

    if (this.errors.length > 0) {
      return;
    }
  }

  resetErrors(): void {
    this.errors.isMissingFileNoOrParty = false;
    this.errors.isMissingSurname = false;
  }

  loadCourtRooms(courtRooms: CourtRoomsJsonInfoType[]): void {
    const sortedCourtRooms = courtRooms
      .sort((a, b) => a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase()))

    sortedCourtRooms.map(cr => {
      this.courtRooms.push({
        text: cr.name,
        value: cr.code
      })
    })
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