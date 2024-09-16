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
        <b-col md="8">
          <b-card v-if="isLookupDataMounted && isLookupDataReady" body-class="py-1">
            <b-form @submit.prevent="handleSubmit">
              <!-- Division -->
              <b-form-group label="Division:" label-cols="3" label-align="right">
                <b-button-group>
                  <b-button value="criminal" @click="handleDivisionChange"
                    :variant="searchCriteria.isCriminal ? 'primary' : 'outline-primary'">
                    Criminal
                  </b-button>
                  <b-button value="civil" @click="handleDivisionChange"
                    :variant="!searchCriteria.isCriminal ? 'primary' : 'outline-primary'">
                    Civil
                  </b-button>
                </b-button-group>
              </b-form-group>
              <!-- File Number or Party Name -->
              <b-form-group label="File Number or Party Name:" label-cols="3" label-align="right">
                <b-form-radio-group v-model="searchCriteria.selectedFileNoOrParty" name="file-radio-group">
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'file' }">
                    <b-form-radio class=" mt-2" value="file"> File Number </b-form-radio>
                    <b-row class="flex-grow-1" v-if="searchCriteria.selectedFileNoOrParty === 'file'">
                      <b-col md="5" class="text-right">
                        <b-form-input placeholder="e.g. 99999999" v-model="searchCriteria.fileNumberTxt"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingFileNoOrParty">Field required</span>
                      </b-col>
                      <b-col md="6" offset-md="1" v-if="searchCriteria.isCriminal">
                        <b-card bg-variant="light" class="ml-1" body-class="p-2">
                          <b-form-group class="mb-0" label="Optional..." label-align="center">
                            <b-form-group label-cols="4" label="Prefix" label-for="filePrefixTxt" label-align="right">
                              <b-form-input id="filePrefixTxt" placeholder="AH"
                                v-model="searchCriteria.filePrefixTxt"></b-form-input>
                            </b-form-group>
                            <b-form-group label-cols="4" label="Seq Num" label-for="fileSuffixNo" label-align="right">
                              <b-form-input id="fileSuffixNo" placeholder="1"
                                v-model="searchCriteria.fileSuffixNo"></b-form-input>
                            </b-form-group>
                            <b-form-group label-cols="4" label="Type Ref" label-for="mDocRefTypeCode"
                              label-align="right">
                              <b-form-input id="mDocRefTypeCode" placeholder="B"
                                v-model="searchCriteria.mDocRefTypeCode"></b-form-input>
                            </b-form-group>
                          </b-form-group>
                        </b-card>
                      </b-col>
                    </b-row>
                  </div>
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'lastName' }">
                    <b-form-radio class="mt-2" value="lastName"> Surname </b-form-radio>
                    <b-row v-if="searchCriteria.selectedFileNoOrParty === 'lastName'">
                      <b-col class="text-right">
                        <b-form-input v-model="searchCriteria.lastName"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingSurname">Field required</span>
                      </b-col>
                      <b-col>
                        <b-form-input placeholder="Given Name" v-model="searchCriteria.givenName"></b-form-input>
                      </b-col>
                    </b-row>
                  </div>
                  <div class="radio-container p-2 rounded d-flex"
                    :class="{ 'bg-info': searchCriteria.selectedFileNoOrParty === 'orgName' }">
                    <b-form-radio class="mt-2" value="orgName"> Organisation </b-form-radio>
                    <b-row v-if="searchCriteria.selectedFileNoOrParty === 'orgName'">
                      <b-col class="text-right">
                        <b-form-input placeholder="e.g. MegaCorp Inc." v-model="searchCriteria.orgName"></b-form-input>
                        <span class="text-danger" v-show="errors.isMissingOrg">Field required</span>
                      </b-col>
                    </b-row>
                  </div>
                </b-form-radio-group>
              </b-form-group>
              <b-form-group label="Class:" label-cols="3" label-align="right">
                <b-row>
                  <b-col cols="3">
                    <b-form-select v-model="searchCriteria.class">
                      <option value=""></option>
                      <option v-for="option in classOptions" :key="option.code" :value="option.code">
                        {{ option.shortDesc }}
                      </option>
                    </b-form-select>
                  </b-col>
                </b-row>
              </b-form-group>
              <!-- fileHomeAgencyId -->
              <b-form-group label="Location:" label-cols="3" label-align="right">
                <b-row>
                  <b-col cols="5">
                    <b-form-select v-model="searchCriteria.fileHomeAgencyId" :options="courtRooms"></b-form-select>
                  </b-col>
                </b-row>
              </b-form-group>
              <b-row>
                <b-col offset-md="3">
                  <b-button variant="primary" type="submit" :disabled="isSearching">
                    <b-icon icon="search"></b-icon>
                    Search
                  </b-button>
                  <b-button variant="outline-primary" class="ml-3" type="button" @click="() => handleReset(true)">
                    Reset Search
                  </b-button>
                </b-col>
              </b-row>
            </b-form>
          </b-card>
        </b-col>
      </b-row>
      <court-file-search-result v-if="isSearching || hasSearched" :isLookupDataMounted="isLookupDataMounted"
        :isLookupDataReady="isLookupDataReady" :courtRooms="courtRooms" :classes="classes"
        :isCriminal="searchCriteria.isCriminal" :searchResults="searchResults" :isSearching="isSearching">
      </court-file-search-result>
    </b-card>
  </b-card>
</template>
<script lang="ts">
import { CourtRoomsJsonInfoType, LookupCode } from "@/types/common";
import { CourtFileSearchCriteria, FileDetail, SearchModeEnum, CourtClassEnum } from "@/types/courtFileSearch";
import { roomsInfoType } from "@/types/courtlist";
import CourtFileSearchResult from "@components/courtfilesearch/CourtFileSearchResult.vue";
import { Component, Vue } from "vue-property-decorator";

const CRIMINAL_CODE = "R";
const CIVIL_CODE = ["I", "F"];

@Component({
  components: {
    CourtFileSearchResult
  }
})
export default class CourtFileSearchView extends Vue {
  errorCode = 0;
  errorText = "";
  courtRooms: roomsInfoType[] = [];
  classes: LookupCode[] = [];
  searchResults: FileDetail[] = [];

  isLookupDataMounted = false;
  isLookupDataReady = false;
  hasSearched = false;
  isSearching = false;
  defaultLocation = this.$store.state.CommonInformation.userInfo.agencyCode;

  classOptions: LookupCode[] = [];

  searchCriteria: CourtFileSearchCriteria = {
    isCriminal: true,
    selectedFileNoOrParty: 'file',
    fileHomeAgencyId: this.defaultLocation,
  };

  errors = {
    isMissingFileNoOrParty: false,
    isMissingSurname: false,
    isMissingOrg: false
  };

  async mounted() {
    this.loadLookups();
  }

  public async loadLookups(): Promise<void> {
    try {
      const [courtRooms, courtClasses] = await Promise.all([
        this.$locationService.getCourtRooms(),
        this.$lookupService.getCourtClasses(),
      ]);

      this.courtRooms = courtRooms;
      this.classes = courtClasses;
      this.loadClasses();
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

  public handleDivisionChange() {
    this.searchCriteria.isCriminal = !this.searchCriteria.isCriminal;
    this.handleReset();
  }

  public async handleSubmit() {
    this.sanitizeTextInputs();
    this.resetErrors();

    this.errors.isMissingFileNoOrParty = this.searchCriteria.selectedFileNoOrParty === 'file' && !this.searchCriteria.fileNumberTxt;
    this.errors.isMissingSurname = this.searchCriteria.selectedFileNoOrParty === 'lastName' && !this.searchCriteria.lastName;
    this.errors.isMissingOrg = this.searchCriteria.selectedFileNoOrParty === 'orgName' && !this.searchCriteria.orgName;

    // Don't proceed if any of the validation flag is set to true
    const hasNoErrors = Object.values(this.errors).every(value => value === false);
    if (!hasNoErrors) {
      return;
    }

    try {
      this.isSearching = true;
      this.searchResults.length = 0;
      this.searchResults = this.searchCriteria.isCriminal
        ? (await this.$filesService.searchCriminalFiles(this.buildQueryParams())).fileDetail
        : (await this.$filesService.searchCivilFiles(this.buildQueryParams())).fileDetail;
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
      this.hasSearched = true;
      this.isSearching = false;
    }
  }

  public handleReset(resetDivision = false): void {
    this.searchCriteria.isCriminal = resetDivision ? true : this.searchCriteria.isCriminal;
    this.searchCriteria.fileHomeAgencyId = this.defaultLocation;
    this.searchCriteria.selectedFileNoOrParty = 'file';
    this.searchCriteria.fileNumberTxt = undefined;
    this.searchCriteria.lastName = undefined;
    this.searchCriteria.givenName = undefined;
    this.searchCriteria.orgName = undefined;
    this.searchCriteria.class = undefined

    if (this.searchCriteria.isCriminal) {
      this.searchCriteria.filePrefixTxt = undefined;
      this.searchCriteria.fileSuffixNo = undefined;
      this.searchCriteria.mDocRefTypeCode = undefined;
    }

    this.loadClasses();
    this.resetErrors();

    this.searchResults.length = 0;
    this.hasSearched = false;
  }

  sanitizeTextInputs(): void {
    this.searchCriteria.fileNumberTxt = this.searchCriteria.fileNumberTxt?.trim();
    this.searchCriteria.filePrefixTxt = this.searchCriteria.filePrefixTxt?.trim();
    this.searchCriteria.fileSuffixNo = this.searchCriteria.fileSuffixNo?.trim();
    this.searchCriteria.mDocRefTypeCode = this.searchCriteria.mDocRefTypeCode?.trim();

    this.searchCriteria.lastName = this.searchCriteria.lastName?.trim();
    this.searchCriteria.givenName = this.searchCriteria.givenName?.trim();

    this.searchCriteria.orgName = this.searchCriteria.orgName?.trim();
  }

  resetErrors(): void {
    this.errors.isMissingFileNoOrParty = false;
    this.errors.isMissingSurname = false;
    this.errors.isMissingOrg = false;
  }

  loadCourtRooms(courtRooms: CourtRoomsJsonInfoType[]): void {
    const sortedCourtRooms = courtRooms
      .sort((a, b) => a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase()))

    sortedCourtRooms.map(cr => {
      this.courtRooms.push({
        text: cr.name,
        value: cr.code
      })
    });
  }

  loadClasses(): void {
    this.classOptions = this.searchCriteria.isCriminal
      ? this.classes.filter(c => c.longDesc === CRIMINAL_CODE)
      : this.classes.filter(c => CIVIL_CODE.includes(c.longDesc));
  }

  buildQueryParams() {
    const queryParams = {
      fileHomeAgencyId: this.searchCriteria.fileHomeAgencyId
    };

    if (this.searchCriteria.selectedFileNoOrParty === 'file') {
      queryParams['searchMode'] = SearchModeEnum.FileNo;
      queryParams['fileNumberTxt'] = this.searchCriteria.fileNumberTxt;

      if (this.searchCriteria.filePrefixTxt) {
        queryParams['filePrefixTxt'] = this.searchCriteria.filePrefixTxt;
      }
      if (this.searchCriteria.fileSuffixNo) {
        queryParams['fileSuffixNo'] = this.searchCriteria.fileSuffixNo;
      }
      if (this.searchCriteria.mDocRefTypeCode) {
        queryParams['mDocRefTypeCode'] = this.searchCriteria.mDocRefTypeCode;
      }
    }
    else if (this.searchCriteria.selectedFileNoOrParty === 'lastName') {
      queryParams['searchMode'] = SearchModeEnum.PartName;
      queryParams['lastName'] = this.searchCriteria.lastName;

      if (this.searchCriteria.givenName) {
        queryParams['givenName'] = this.searchCriteria.givenName;
      }
    }
    else {
      queryParams['searchMode'] = SearchModeEnum.PartName;
      queryParams['orgName'] = this.searchCriteria.orgName;
    }

    if (this.searchCriteria.class) {
      queryParams['courtClass'] = CourtClassEnum[this.searchCriteria.class];
    }

    return queryParams;
  }
}
</script>

<style scoped lang="scss">
@import '../../assets/_custom.scss';

.card {
  border: white;
}

.btn-group.active {
  color: $blue;
}

.transparent {
  background-color: transparent;
}
</style>