<!-- Currently unused. -->
<template>
    <b-card bg-variant="white" no-body class="mb-5">
        <div>
            <h3 class="mx-4 font-weight-normal"> Adjudicator Restrictions </h3>
            <hr class="mx-3 bg-light" style="height: 5px;"/> 
        </div>

        <b-card v-if="!(adjudicatorRestrictionsInfo.length>0)">
            <span class="text-muted ml-4"> No adjudicator restrictions. </span>
        </b-card>

        <b-card bg-variant="white" v-if="isMounted && (adjudicatorRestrictionsInfo.length>0)" no-body class="mx-3">           
            <b-table        
            borderless
            :items="adjudicatorRestrictionsInfo"
            :fields="fields"
            :sort-by.sync="sortBy"
            :sort-desc.sync="sortDesc"
            sort-icon-left
            small
            responsive="sm"
            >   
                <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                    <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                </template>
                <template v-slot:cell(status)="data" >                   
                    <b-badge 
                        variant="primary" 
                        :style="data.field.cellStyle"> 
                        {{ data.value }}
                    </b-badge>
                </template>
            </b-table>
        </b-card>
    </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CivilFileInformation";
import {civilFileInformationType} from '@/types/civil';
import {AdjudicatorRestrictionsInfoType } from '@/types/common';
const civilState = namespace("CivilFileInformation");

@Component
export default class  CivilAdjudicatorRestrictions extends Vue {

  @civilState.State
  public civilFileInformation!: civilFileInformationType;
    
  adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[] = []; 
  sortBy = 'adjudicator';
  sortDesc = false;
  isMounted = false;

  fields = [
      {key:'adjudicator',   label:'Adjudicator',    sortable:true, tdClass: 'border-top',  headerStyle:'text-primary'},       
      {key:'status',        label:'Status',         sortable:true, tdClass: 'border-top',  headerStyle:'text-primary', cellStyle: 'font-weight: normal; font-size: 16px;'},
      {key:'appliesTo',     label:'Applies to',     sortable:true, tdClass: 'border-top',  headerStyle:'text-primary'}           
  ];
  
  mounted() {
    this.getAdjudicatorRestrictions();
  }

  public getAdjudicatorRestrictions(): void {      
      this.adjudicatorRestrictionsInfo = this.civilFileInformation.adjudicatorRestrictionsInfo;     
      this.isMounted = true;          
  } 
  
}
</script>

<style scoped>
 .card {
        border: white;
    }
</style>