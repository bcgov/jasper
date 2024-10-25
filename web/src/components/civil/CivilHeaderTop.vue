<template>
<div>  
    <b-navbar type="white" variant="white" v-if="isMounted" style="height:30px;">
      <b-navbar-nav>
            <b-nav-text style="font-size: 14px;" variant="white">
                <b-badge class="mr-1" variant="primary">{{courtClassDescription}}</b-badge>                                
                <b-badge class="mr-1" variant="secondary">{{courtLevelDescription}}</b-badge>              
            </b-nav-text>
      </b-navbar-nav>
    </b-navbar>
</div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";
import { namespace } from "vuex-facing-decorator";
import '@store/modules/CivilFileInformation';
import {civilFileInformationType} from '@/types/civil';
const civilState = namespace('CivilFileInformation');

@Component
export default class CivilHeaderTop extends Vue {
  
  @civilState.State
  public civilFileInformation!: civilFileInformationType;
  
  courtLevelDescription = '';
  courtClassDescription = '';
  isMounted = false;

  mounted() {
    this.getFileDescription();
  }

  public getFileDescription(): void {      
      const data = this.civilFileInformation.detailsData;
      this.courtClassDescription = data.courtClassDescription;      
      this.courtLevelDescription = data.courtLevelDescription;
      this.isMounted = true;          
  } 
 
}
</script>