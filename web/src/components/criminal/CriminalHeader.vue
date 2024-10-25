<template>
  <div>
    <b-navbar type="white" variant="white" v-if="isMounted" style="height:45px">
      <b-navbar-nav>
        <b-nav-text class="text-primary mr-2" style="margin-top: 6px; font-size: 12px;">
          <b-icon icon="file-earmark-text"></b-icon>
          {{ fileNumberText }}
        </b-nav-text>

        <b-nav-text class="mt-2 ml-1 mr-2" style="font-size: 11px;">
          {{ agencyLocation.name }}
          <span v-if="agencyLocation.code"> ({{ agencyLocation.code }}) </span>
        </b-nav-text>

        <b-nav-text class="text-muted mr-3 mt-2" style="font-size: 11px;">
          {{ agencyLocation.region }}
        </b-nav-text>

        <b-nav-item-dropdown class="mr-3 mt-1" right no-caret size="sm">
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="transform: translate(0,-4px); border:0px; font-size:14px"
              size="sm"
            >
              <b-icon class="mr-2" icon="person-fill"></b-icon>
              <b>{{ getNameOfParticipantTrunc() }}</b> and {{ participantList.length - 1 }} other(s)
              <b-icon class="ml-1" icon="caret-down-fill" font-scale="1"></b-icon>
            </b-button>
          </template>
          <b-dropdown-item-button
            v-for="participant in SortedParticipants"
            :key="participant.index"
            v-on:click="setActiveParticipantIndex(participant.index)"
            >{{ participant.name }}</b-dropdown-item-button
          >
        </b-nav-item-dropdown>

        <b-nav-text style="margin-top: 4px;font-size: 12px;" variant="white">
          <b-badge pill variant="danger">{{ adjudicatorRestrictions.length }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown right no-caret :disabled="adjudicatorRestrictions.length == 0">
          <template v-slot:button-content>
            <b-button
              :variant="adjudicatorRestrictions.length > 0 ? 'outline-primary text-info' : 'white'"
              :disabled="adjudicatorRestrictions.length == 0"
              style="transform: translate(-5px,0); border:0px; font-size:12px;text-overflow: ellipsis;"
              size="sm"
            >
              Adjudicator Restrictions
              <b-icon
                v-if="adjudicatorRestrictions.length > 0"
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button v-for="(restriction, index) in adjudicatorRestrictions" :key="index">
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

        <b-nav-text v-if="bans.length > 0" style="margin-top: 4px;font-size: 12px;" variant="white">
          <b-badge pill variant="danger">{{ bans.length }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown v-if="bans.length > 0" right no-caret>
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="transform: translate(-5px,0); border:0px; font-size:12px;text-overflow: ellipsis;"
              size="sm"
            >
              Ban Details
              <b-icon class="ml-1" icon="caret-down-fill" font-scale="1"></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button>
            <b-card bg-variant="white" no-body border-variant="white">
              <b-table borderless :items="bans" :fields="fields" small responsive="sm">
                <template v-slot:cell()="data">
                  <span style="transform: translate(0, +6px)">{{ data.value }}</span>
                </template>
                <template v-slot:cell(banParticipant)="data">
                  <span style="transform: translate(0, +5px)" v-if="data.item.comment.length == 0">{{
                    data.value
                  }}</span>
                  <b-button
                    class="text-success bg-white border-white"
                    v-else
                    v-b-tooltip.hover.left
                    v-b-tooltip.hover.html="data.item.comment"
                  >
                    {{ data.value }}
                  </b-button>
                </template>
              </b-table>
            </b-card>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>
      </b-navbar-nav>
    </b-navbar>

    <hr class="mx-3 bg-warning" style="border-top: 2px double #FCBA19" />
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import * as _ from "underscore";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CriminalFileInformation";
import "@store/modules/CommonInformation";
import { bansInfoType, participantListInfoType, criminalFileInformationType } from "@/types/criminal";
import { InputNamesType, AdjudicatorRestrictionsInfoType } from "@/types/common";
const criminalState = namespace("CriminalFileInformation");
const commonState = namespace("CommonInformation");

@Component
export default class CriminalHeader extends Vue {
  @criminalState.State
  public activeCriminalParticipantIndex;

  @commonState.State
  public displayName!: string;

  @criminalState.State
  public criminalFileInformation!: criminalFileInformationType;

  @criminalState.Action
  public UpdateActiveCriminalParticipantIndex!: (newActiveCriminalParticipantIndex: string) => void;

  @commonState.Action
  public UpdateDisplayName!: (newInputNames: InputNamesType) => void;

  participantList: participantListInfoType[] = [];
  adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[] = [];
  bans: bansInfoType[] = [];

  maximumFullNameLength = 15;
  numberOfParticipants = 0;
  fileNumberText;
  agencyLocation = { name: "", code: "0", region: "" };
  isMounted = false;

  fields = [
    {
      key: "banParticipant",
      label: "Ban Participant",
      sortable: false,
      tdClass: "border-top",
      headerStyle: "table-borderless text-primary",
    },
    { key: "banType", label: "Ban Type", sortable: false, tdClass: "border-top", headerStyle: "text-primary" },
    { key: "orderDate", label: "Order Date", sortable: false, tdClass: "border-top", headerStyle: "text-primary" },
    { key: "act", label: "Act", sortable: false, tdClass: "border-top", headerStyle: "text-primary" },
    { key: "sub", label: "Sub", sortable: false, tdClass: "border-top", headerStyle: "text-primary" },
    { key: "description", label: "Description", sortable: false, tdClass: "border-top", headerStyle: "text-primary" },
  ];

  mounted() {
    this.getHeaderInfo();
  }

  public getHeaderInfo(): void {
    const data = this.criminalFileInformation.detailsData;
    this.fileNumberText = data.fileNumberTxt;
    this.agencyLocation.name = data.homeLocationAgencyName;
    this.agencyLocation.code = data.homeLocationAgencyCode;
    this.agencyLocation.region = data.homeLocationRegionName;
    this.adjudicatorRestrictions = this.criminalFileInformation.adjudicatorRestrictionsInfo;
    this.participantList = this.criminalFileInformation.participantList;
    this.bans = this.criminalFileInformation.bans;
    this.numberOfParticipants = this.participantList.length - 1;
    this.isMounted = true;
    this.setActiveParticipantIndex(this.SortedParticipants[0].index);
  }

  public setActiveParticipantIndex(index) {
    this.UpdateActiveCriminalParticipantIndex(index);
  }

  public getNameOfParticipant(num) {
    this.UpdateDisplayName({
      lastName: this.participantList[num].lastName,
      givenName: this.participantList[num].firstName,
    });
    return this.displayName;
  }

  public getNameOfParticipantTrunc() {
    const nameOfParticipant = this.getNameOfParticipant(this.activeCriminalParticipantIndex);

    if (nameOfParticipant.length > this.maximumFullNameLength)
      return nameOfParticipant.substr(0, this.maximumFullNameLength) + " ... ";
    else return nameOfParticipant;
  }

  get SortedParticipants() {
    return _.sortBy(this.participantList, (participant) => {
      return participant.lastName ? participant.lastName.toUpperCase() : "";
    });
  }
}
</script>
