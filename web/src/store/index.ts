import CivilFileInformation from '@/store/modules/CivilFileInformation'
import CommonInformation from '@/store/modules/CommonInformation'
import CourtListInformation from '@/store/modules/CourtListInformation'
import CriminalFileInformation from '@/store/modules/CriminalFileInformation'
import Vue from 'vue'
import Vuex from 'vuex'
import SelectedCourtFiles from './modules/SelectedCourtFiles'

Vue.use(Vuex)

const store = new Vuex.Store({
  modules: {
    CivilFileInformation,
    CriminalFileInformation,
    CommonInformation,
    CourtListInformation,
    SelectedCourtFiles
  }
})

export default store
