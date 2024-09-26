<template>
  <b-card class="main-container px-0" no-body bg-variant="white">
    <b-card body-class="px-0" bg-variant="light" v-if="!isLookupDataMounted && !isLookupDataReady">
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
    <b-card body-class="px-0" bg-variant="light" v-else-if="isLookupDataMounted && !isLookupDataReady">
      <b-card style="min-height: 40px;">
        <span v-if="errorCode > 0">
          <span v-if="errorCode == 403"> You are not authorized to access this page. </span>
          <span v-else>
            Server is not responding. <b>({{ errorText }} "{{ errorCode }}")</b></span>
        </span>
        <span v-else> No Court File Search Found. </span>
      </b-card>
    </b-card>
    <b-card body-class="px-0" v-else>
      <h2>Court File Search</h2>
      <b-form class="search-form" v-if="isLookupDataMounted && isLookupDataReady" @submit.prevent="handleSubmit">
        <div class="d-flex">
          <!-- Location -->
          <b-form-group label="Location:" class="mr-3">
            <b-form-select v-model="searchCriteria.fileHomeAgencyId">
              <option value=""></option>
              <option v-for="option in courtRooms" :key="option.value" :value="option.value">
                {{ option.text }}
              </option>
            </b-form-select>
          </b-form-group>

          <!-- Division -->
          <b-form-group label="Division*" class="mr-3">
            <b-button-group>
              <b-button value="isCriminal" @click="handleDivisionChange"
                :variant="searchCriteria.isCriminal ? 'primary' : 'outline-primary'">
                Criminal
              </b-button>
              <b-button value="isFamily" @click="handleDivisionChange"
                :variant="searchCriteria.isFamily ? 'primary' : 'outline-primary'">
                Family
              </b-button>
              <b-button value="isSmallClaims" @click="handleDivisionChange"
                :variant="searchCriteria.isSmallClaims ? 'primary' : 'outline-primary'">
                Small Claims
              </b-button>
            </b-button-group>
          </b-form-group>

          <!-- Class -->
          <b-form-group label="Class:" class="mr-3">
            <b-form-select v-model="searchCriteria.class">
              <option value=""></option>
              <option v-for="option in classOptions" :key="option.code" :value="option.code">
                {{ option.shortDesc }}
              </option>
            </b-form-select>
          </b-form-group>

        </div>

        <!-- Search -->
        <div class="d-flex align-items-end">
          <b-form-group label="Search*" horizontal>
            <b-form-select class="search" v-model="searchCriteria.searchBy">
              <option value="fileNumber">File number</option>
              <option value="lastName">Surname</option>
              <option value="orgName">Organization</option>
            </b-form-select>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'fileNumber'">
            <b-form-input class="search-input" :class="{ 'is-invalid': errors.isMissingFileNo }"
              placeholder="e.g. 99999999" v-model="searchCriteria.fileNumberTxt"></b-form-input>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'lastName'">
            <b-form-input class="search-input" placeholder="MacDonald"
              :class="{ 'is-invalid': errors.isMissingSurname }" v-model="searchCriteria.lastName"></b-form-input>
          </b-form-group>

          <b-form-group v-if="searchCriteria.searchBy === 'orgName'">
            <b-form-input class="search-input" :class="{ 'is-invalid': errors.isMissingOrg }"
              placeholder="e.g. MegaCorp Inc." v-model="searchCriteria.orgName"></b-form-input>
          </b-form-group>
          <!-- Submit -->
          <b-button variant="primary" class="search-btn mb-3" type="submit" :disabled="isSearching">
            <b-icon icon="search"></b-icon>
            Search
          </b-button>
          <!-- Reset -->
          <b-button variant="link" class="reset mb-3" @click="() => handleReset(true)">
            Reset search
          </b-button>
        </div>
        <div
          v-if="(searchCriteria.isCriminal && searchCriteria.searchBy === 'fileNumber') || searchCriteria.searchBy === 'lastName'">
          <span>Optional...</span>
          <div class="d-flex p-3 bg-light">
            <div class="d-flex" v-if="searchCriteria.searchBy === 'fileNumber'">
              <b-form-group class="mr-3" label="Prefix" label-for="filePrefixTxt">
                <b-form-input id="filePrefixTxt" placeholder="AH" v-model="searchCriteria.filePrefixTxt"></b-form-input>
              </b-form-group>

              <b-form-group class="mr-3" label="Seq Num" label-for="fileSuffixNo">
                <b-form-input id="fileSuffixNo" placeholder="1" v-model="searchCriteria.fileSuffixNo"></b-form-input>
              </b-form-group>

              <b-form-group label="Type Ref" label-for="mDocRefTypeCode">
                <b-form-input id="mDocRefTypeCode" placeholder="B"
                  v-model="searchCriteria.mDocRefTypeCode"></b-form-input>
              </b-form-group>
            </div>

            <b-form-group label="Given Name" label-for="givenName" v-if="searchCriteria.searchBy === 'lastName'">
              <b-form-input id="givenName" placeholder="e.g. John" v-model="searchCriteria.givenName"></b-form-input>
            </b-form-group>
          </div>
        </div>
      </b-form>
      <court-file-search-result class="mb-5" v-if="isSearching || hasSearched"
        :isLookupDataMounted="isLookupDataMounted" :isLookupDataReady="isLookupDataReady" :courtRooms="courtRooms"
        :classes="classes" :isCriminal="searchCriteria.isCriminal" :searchResults="searchResults"
        :isSearching="isSearching" @files-viewed="viewFiles" :selectedFiles="selectedFiles"
        @add-selected="addSelectedFile" @remove-selected="removeSelectedFile" @clear-selected="clearSelectedFiles"
        :isSearchResultsOver="isSearchResultsOver">
      </court-file-search-result>
    </b-card>
  </b-card>
</template>
<script lang="ts">
import { ADD_FILES_FOR_VIEWING, CLEAR_SELECTED_FILES, GET_CURRENT_SEARCH_CRITERIA, GET_CURRENT_SEARCH_RESULTS, GET_SELECTED_FILES } from "@/store/modules/CourtFileSearchInformation";
import { CourtRoomsJsonInfoType, KeyValueInfo, LookupCode } from "@/types/common";
import { CourtFileSearchCriteria, FileDetail, SearchModeEnum, CourtClassEnum } from "@/types/courtFileSearch";
import { roomsInfoType } from "@/types/courtlist";
import CourtFileSearchResult from "@components/courtfilesearch/CourtFileSearchResult.vue";
import { Component, Vue } from "vue-property-decorator";

const CRIMINAL_CODE = "R";
const SMALL_CLAIMS_CODE = "I";
const SEARCH_RESULT_LIMIT = 100;

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
  selectedFiles: FileDetail[] = [];

  isLookupDataMounted = false;
  isLookupDataReady = false;
  hasSearched = false;
  isSearching = false;
  isSearchResultsOver = false;
  defaultLocation = this.$store.state.CommonInformation.userInfo.agencyCode;

  classOptions: LookupCode[] = [];

  searchCriteria: CourtFileSearchCriteria = {
    isCriminal: true,
    isFamily: false,
    isSmallClaims: false,
    searchBy: 'fileNumber',
    fileHomeAgencyId: this.defaultLocation,
  };

  errors = {
    isMissingFileNo: false,
    isMissingSurname: false,
    isMissingOrg: false
  };

  async mounted() {
    this.loadData();
  }

  public async loadData(): Promise<void> {
    try {
      const [courtRooms, courtClasses] = await Promise.all([
        this.$locationService.getCourtRooms(),
        this.$lookupService.getCourtClasses(),
      ]);

      this.courtRooms = courtRooms;
      this.classes = courtClasses;
      this.loadDataFromState();
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

  public handleDivisionChange(event: Event) {
    this.searchCriteria.isCriminal = false;
    this.searchCriteria.isFamily = false;
    this.searchCriteria.isSmallClaims = false;

    const target = event.target as HTMLInputElement;
    this.searchCriteria[target.value] = true;

    this.handleReset();
  }

  public async handleSubmit() {
    this.$store.commit(CLEAR_SELECTED_FILES);

    this.sanitizeTextInputs();
    this.resetErrors();

    this.errors.isMissingFileNo = this.searchCriteria.searchBy === 'fileNumber' && !this.searchCriteria.fileNumberTxt;
    this.errors.isMissingSurname = this.searchCriteria.searchBy === 'lastName' && !this.searchCriteria.lastName;
    this.errors.isMissingOrg = this.searchCriteria.searchBy === 'orgName' && !this.searchCriteria.orgName;

    // Don't proceed if any of the validation flag is set to true
    const hasNoErrors = Object.values(this.errors).every(value => value === false);
    if (!hasNoErrors) {
      return;
    }

    try {
      this.isSearching = true;
      this.searchResults.length = 0;
      this.isSearchResultsOver = false;

      const { recCount, fileDetail } = this.searchCriteria.isCriminal
        ? (await this.$filesService.searchCriminalFiles(this.buildQueryParams()))
        : (await this.$filesService.searchCivilFiles(this.buildQueryParams()));

      // Make sure that to only show up to 100 results
      this.isSearchResultsOver = recCount > SEARCH_RESULT_LIMIT;
      this.searchResults = fileDetail.slice(0, SEARCH_RESULT_LIMIT);
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
    this.$store.commit(CLEAR_SELECTED_FILES);
    this.searchCriteria.isCriminal = resetDivision ? true : this.searchCriteria.isCriminal;
    this.searchCriteria.isFamily = resetDivision ? false : this.searchCriteria.isFamily;
    this.searchCriteria.isSmallClaims = resetDivision ? false : this.searchCriteria.isSmallClaims;
    this.searchCriteria.fileHomeAgencyId = this.defaultLocation;
    this.searchCriteria.searchBy = 'fileNumber';
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
    this.errors.isMissingFileNo = false;
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
    if (this.searchCriteria.isCriminal) {
      this.classOptions = this.classes.filter(c => c.longDesc === CRIMINAL_CODE);
    }
    else if (this.searchCriteria.isFamily) {
      this.classOptions = [];
    }
    else if (this.searchCriteria.isSmallClaims) {
      this.classOptions = this.classes.filter(c => c.longDesc === SMALL_CLAIMS_CODE && c.code !== CourtClassEnum[CourtClassEnum.C]);
    }
  }

  buildQueryParams() {
    const queryParams = this.buildSearchByQueryParams();

    // Class
    if (this.searchCriteria.isFamily) {
      queryParams['courtClass'] = CourtClassEnum.F;
    }
    else if (this.searchCriteria.class) {
      queryParams['courtClass'] = CourtClassEnum[this.searchCriteria.class];
    }
    else if (this.searchCriteria.isSmallClaims) {
      queryParams['courtClass'] = CourtClassEnum.C;
    }

    if (this.searchCriteria.fileHomeAgencyId) {
      queryParams['fileHomeAgencyId'] = this.searchCriteria.fileHomeAgencyId;
    }

    return queryParams;
  }

  buildSearchByQueryParams() {
    const queryParams = {};

    if (this.searchCriteria.searchBy === 'fileNumber') {
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
    else if (this.searchCriteria.searchBy === 'lastName') {
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

    return queryParams;
  }

  loadDataFromState(): void {
    const searchCriteria = this.$store.getters[GET_CURRENT_SEARCH_CRITERIA];
    const searchResults = this.$store.getters[GET_CURRENT_SEARCH_RESULTS];
    const selectedFiles = this.$store.getters[GET_SELECTED_FILES];

    if (searchCriteria && searchResults) {
      this.searchCriteria = searchCriteria;
      this.searchResults = [...searchResults];

      const idSelector = this.searchCriteria.isCriminal ? 'mdocJustinNo' : 'physicalFileId';
      const files = this.searchResults.filter(c => selectedFiles.some(f => f.key === c[idSelector]));
      this.selectedFiles = [...files];

      this.hasSearched = true;
    }
  }

  viewFiles(selectedFiles: KeyValueInfo[]): void {
    this.$store.commit(
      ADD_FILES_FOR_VIEWING,
      {
        searchCriteria: this.searchCriteria,
        searchResults: this.searchResults,
        files: selectedFiles
      }
    );
    const caseDetailUrl = `/${this.searchCriteria.isCriminal ? 'criminal-file' : 'civil-file'}/${selectedFiles[0].key}`;
    this.$router.push(caseDetailUrl);
  }

  addSelectedFile(file: FileDetail): void {
    this.selectedFiles.push(file);
  }

  removeSelectedFile(idSelector: string, fileId: string): void {
    this.selectedFiles = this.selectedFiles.filter(c => c[idSelector] !== fileId);
  }

  clearSelectedFiles(): void {
    this.selectedFiles = [];
    this.$store.commit(CLEAR_SELECTED_FILES);
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

.reset,
.reset:hover,
.reset:focus {
  text-decoration: none !important;
  color: $primary !important;
  box-shadow: none;
}

.search {
  border-bottom-right-radius: 0;
  border-top-right-radius: 0;
}

.search-input {
  border-radius: 0;
}

.search-btn {
  border-bottom-left-radius: 0;
  border-top-left-radius: 0;
}
</style>