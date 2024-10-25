<template>
  <b-card no-body>
    <b-navbar type="white" variant="white" v-if="isMounted" style="height:45px;">
      <b-navbar-nav>
        <b-nav-text class="mr-2" style="margin-top: 6px; font-size: 14px;">
          <b-icon icon="file-earmark-text"></b-icon>
          <span :style="getActivityClass()" class="file-number-txt"> {{ fileNumberText }}</span>
        </b-nav-text>

        <b-nav-text class="mt-2 ml-1 mr-2" style="font-size: 11px;">
          {{ agencyLocation.name }}
          <span v-if="agencyLocation.code"> ({{ agencyLocation.code }}) </span>
        </b-nav-text>

        <b-nav-text class="text-muted mr-3" style="margin-top: 8px; font-size: 11px;">
          {{ agencyLocation.region }}
        </b-nav-text>

        <b-dropdown class="mt-1 mr-2" no-caret right variant="white">
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="transform: translate(0,-4px); border:0px; font-size:14px; text-overflow: ellipsis;"
              v-b-tooltip.hover.bottomleft
              :title="partyDisplayedTxt"
              size="sm"
            >
              <b-icon class="mr-2" icon="person-fill"></b-icon>
              <b style="text-overflow: ellipsis;"> {{ getNameOfPartyTrunc() }} </b>
              <b-icon class="ml-1" icon="caret-down-fill" font-scale="1"></b-icon>
            </b-button>
          </template>
          <b-dropdown-item-button disabled v-for="leftParty in leftPartiesInfo" v-bind:key="leftParty.index">{{
            leftParty.name
          }}</b-dropdown-item-button>
          <b-dropdown-divider></b-dropdown-divider>
          <b-dropdown-item-button disabled v-for="rightParty in rightPartiesInfo" v-bind:key="rightParty.index">{{
            rightParty.name
          }}</b-dropdown-item-button>
        </b-dropdown>

        <b-nav-text style="margin-top: 4px;font-size: 12px;" variant="white">
          <b-badge pill variant="danger">{{ adjudicatorRestrictionsInfo.length }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown
          right
          no-caret
          :disabled="adjudicatorRestrictionsInfo.length == 0"
          v-if="userInfo.userType != 'vc'"
        >
          <template v-slot:button-content>
            <b-button
              :variant="adjudicatorRestrictionsInfo.length > 0 ? 'outline-primary text-info' : 'white'"
              :disabled="adjudicatorRestrictionsInfo.length == 0"
              style="transform: translate(-5px,0); border:0px; font-size:12px;text-overflow: ellipsis;"
              size="sm"
            >
              Adjudicator Restrictions
              <b-icon
                v-if="adjudicatorRestrictionsInfo.length > 0"
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button v-for="(restriction, index) in adjudicatorRestrictionsInfo" :key="index">
            <b-button
              style="font-size: 12px; padding: 5px 5px;"
              variant="secondary"
              v-b-tooltip.hover.left
              :title="restriction.fullName"
            >
              {{ restriction.adjRestriction }}
            </b-button>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>

        <b-nav-text v-if="sheriffComment.length > 0" style="margin-top: 4px;font-size: 12px;" variant="white">
          <b-badge pill variant="danger">1</b-badge>
        </b-nav-text>
        <b-nav-item-dropdown v-if="sheriffComment.length > 0" right no-caret>
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="transform: translate(-5px,0); border:0px; font-size:12px;text-overflow: ellipsis;"
              size="sm"
            >
              Sheriff Comments
              <b-icon class="ml-1" icon="caret-down-fill" font-scale="1"></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button>
            <b-card bg-variant="white" no-body border-variant="white">
              {{ sheriffComment }}
            </b-card>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>

        <b-nav-text style="margin-top: 4px;font-size: 12px;" variant="white">
          <b-badge v-if="isSealed" variant="danger">Sealed</b-badge>
        </b-nav-text>
      </b-navbar-nav>
    </b-navbar>
    <hr class="mx-1 bg-warning" style="border-top: 2px double #FCBA19" />
  </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CivilFileInformation";
import "@store/modules/CommonInformation";
import { civilFileInformationType, partiesInfoType } from "@/types/civil";
import { AdjudicatorRestrictionsInfoType, UserInfo } from "@/types/common";
const civilState = namespace("CivilFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CivilHeader extends Vue {
  @civilState.State
  public civilFileInformation!: civilFileInformationType;

  @commonState.State
  public userInfo!: UserInfo;

  maximumFullNameLength = 15;
  activeParty = 0;
  fileNumberText;
  sheriffComment;
  activityClassCode;
  agencyLocation = { name: "", code: "0", region: "" };
  isMounted = false;
  isSealed = false;
  partyDisplayedTxt;
  leftPartiesInfo: partiesInfoType[] = [];
  rightPartiesInfo: partiesInfoType[] = [];
  adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[] = [];
  activityClassCodeMapping = {
    S: "color: #21B851;",
    R: "color: #17A5E7;",
    M: "color: #DF882A;",
    I: "color: #A22BB9;",
    F: "color: #21B851;",
    SIT: "color: #d33;",
    NS: "color: #999;",
  };

  mounted() {
    this.getHeaderInfo();
  }

  public getHeaderInfo(): void {
    const data = this.civilFileInformation.detailsData;
    this.fileNumberText = data.fileNumberTxt;
    this.activityClassCode = data.activityClassCd;
    this.agencyLocation.name = data.homeLocationAgencyName;
    this.agencyLocation.code = data.homeLocationAgencyCode;
    this.agencyLocation.region = data.homeLocationRegionName;
    this.partyDisplayedTxt = data.socTxt;
    this.sheriffComment = data.sheriffCommentText ? data.sheriffCommentText : "";
    this.isSealed = this.civilFileInformation.isSealed;
    this.leftPartiesInfo = this.civilFileInformation.leftPartiesInfo;
    this.rightPartiesInfo = this.civilFileInformation.rightPartiesInfo;
    this.adjudicatorRestrictionsInfo = this.civilFileInformation.adjudicatorRestrictionsInfo;
    this.isMounted = true;
  }

  public getNameOfPartyTrunc() {
    if (this.partyDisplayedTxt) {
      let firstParty = this.partyDisplayedTxt.split("/")[0] ? this.partyDisplayedTxt.split("/")[0].trim() : "";
      let secondParty = this.partyDisplayedTxt.split("/")[1] ? this.partyDisplayedTxt.split("/")[1].trim() : "";

      if (firstParty.length > this.maximumFullNameLength)
        firstParty = firstParty.substr(0, this.maximumFullNameLength) + " ...";

      if (secondParty.length > this.maximumFullNameLength)
        secondParty = secondParty.substr(0, this.maximumFullNameLength) + " ...";

      return firstParty + " / " + secondParty;
    } else {
      return "";
    }
  }

  public getActivityClass() {
    return this.activityClassCodeMapping[this.activityClassCode];
  }
}
</script>

<style scoped>
.file-number-txt {
  font-weight: bold;
}
</style>
