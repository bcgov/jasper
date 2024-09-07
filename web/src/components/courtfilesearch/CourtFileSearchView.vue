<template>
  <b-card bg-variant="white">
    <b-navbar type="white" variant="white" style="height:45px">
      <h2 class="ml-1 mt-2">Court File Search</h2>
    </b-navbar>
    <court-file-search-filter></court-file-search-filter>
    <court-file-search-result></court-file-search-result>
  </b-card>
</template>
<script lang="ts">
import CourtFileSearchFilter from "@components/courtfilesearch/CourtFileSearchFilter.vue";
import CourtFileSearchResult from "@components/courtfilesearch/CourtFileSearchResult.vue";
import { Component, Vue } from "vue-property-decorator";

@Component({
  components: {
    CourtFileSearchFilter,
    CourtFileSearchResult
  }
})
export default class CourtFileSearchView extends Vue {

}
</script>

<style scoped>
.card {
  border: white;
}
</style>