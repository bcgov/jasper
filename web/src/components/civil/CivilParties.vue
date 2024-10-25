<template>
    <b-card bg-variant="white" no-body>
        <b-row cols="2" >
            <b-col class="mt-2" md="6" cols="6">
                <div>
                    <h3 class="mx-4 font-weight-normal"> {{leftRole}} ({{numberOfLeftParties}}) </h3>
                    <hr class="mx-3 bg-light" style="height: 5px;"/> 
                </div>

                <b-card bg-variant="white" style="max-height: 400px; overflow-y: auto;" no-body class="mx-3 mb-5">           
                    <b-table
                    :items="leftPartiesInfo"
                    :fields="fields"                    
                    :no-sort-reset="true" 
                    sort-icon-left
                    borderless
                    striped
                    small 
                    responsive="sm"
                    >   
                        <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                            <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                        </template>                
                        <template v-for="(field,index) in fields" v-slot:[`cell(${field.key})`]="data" >
                            <span 
                                v-bind:key="index" 
                                :style="field.cellStyle" 
                                v-if="data.field.key != 'counsel'">  {{ data.value }} </span>
                            <span 
                                v-bind:key="index" 
                                :style="field.cellStyle" 
                                v-if="data.field.key == 'counsel'">
                                <span 
                                    v-for="(counsel, counselIndex) in data.value"  
                                    v-bind:key="counselIndex" 
                                    style= "white-space: pre-line" >
                                    CEIS: {{ counsel }}
                                </span>
                            </span>
                        </template>
                        
                    </b-table>
                </b-card>
            </b-col>
            <b-col class="mt-2" md="6" cols="6" >
                <div>
                    <h3 class="mx-4 font-weight-normal"> {{rightRole}} ({{numberOfRightParties}}) </h3>
                    <hr class="mx-3 bg-light" style="height: 5px;"/> 
                </div>

                <b-card bg-variant="white" style="max-height: 400px; overflow-y: auto;" no-body class="mx-3 mb-5">           
                    <b-table
                    :items="rightPartiesInfo"
                    :fields="fields"                    
                    :no-sort-reset="true"
                    borderless
                    striped
                    small
                    sort-icon-left
                    responsive="sm"
                    >   
                        <template v-for="(field,index) in fields" v-slot:[`head(${field.key})`]="data">
                            <b v-bind:key="index" :class="field.headerStyle" > {{ data.label }}</b>
                        </template>                
                        <template v-for="(field,index) in fields" v-slot:[`cell(${field.key})`]="data" >
                            <span v-bind:key="index" :style="field.cellStyle" v-if="data.field.key != 'counsel'">  {{ data.value }} </span>
                            <span 
                                v-bind:key="index" 
                                :style="field.cellStyle" 
                                v-if="data.field.key == 'counsel'">
                                <span 
                                    v-for="(counsel, counselIndex) in data.value"  
                                    v-bind:key="counselIndex" 
                                    style= "white-space: pre-line" >
                                    CEIS: {{ counsel }}<br>
                                </span>
                            </span>
                        </template>
                        
                    </b-table>
                </b-card>

            </b-col>    

        </b-row>
    </b-card>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import "@store/modules/CivilFileInformation";
import {civilFileInformationType, partiesInfoType} from '@/types/civil';
const civilState = namespace("CivilFileInformation");

@Component
export default class CivilParties extends Vue {
    
    @civilState.State
    public civilFileInformation!: civilFileInformationType;        
  
    isMounted = false;
    leftPartiesInfo: partiesInfoType[] = [];
    rightPartiesInfo: partiesInfoType[] = [];
    leftRole = '';
    rightRole = '';
    numberOfLeftParties = 0
    numberOfRightParties = 0;
    sortBy = 'name';
    sortDesc = false;

    fields =  
    [
        {key:'name',    label:'Name',      sortable:true,  tdClass: 'border-top',  headerStyle:'text-primary',   cellStyle:'font-weight: bold; font-size: 14px;'},
        {key:'role',    label:'Role',      sortable:false, tdClass: 'border-top',  headerStyle:'text',           cellStyle:'font-size: 14px;'},
        {key:'counsel', label:'Counsel',   sortable:false, tdClass: 'border-top',  headerStyle:'text',           cellStyle:'font-size: 14px;'}
    ];
    
    mounted() {
        this.getParties();
    }

    public getParties(): void {
        this.leftRole = this.civilFileInformation.detailsData.leftRoleDsc;
        this.rightRole = this.civilFileInformation.detailsData.rightRoleDsc;       
        this.leftPartiesInfo = this.civilFileInformation.leftPartiesInfo
        this.rightPartiesInfo = this.civilFileInformation.rightPartiesInfo
        this.numberOfLeftParties = this.leftPartiesInfo.length;  
        this.numberOfRightParties = this.rightPartiesInfo.length;

        this.isMounted = true;          
    } 

}
</script>

<style scoped>
 .card {
        border: white;
    }

</style>