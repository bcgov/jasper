import CivilFileInformation from '@/store/modules/CivilFileInformation'
import CommonInformation from '@/store/modules/CommonInformation'
import CourtListInformation from '@/store/modules/CourtListInformation'
import CriminalFileInformation from '@/store/modules/CriminalFileInformation'
import Vue, { createPinia } from 'vue'
//import Vuex from 'vuex'
import Pinia from 'pinia'
import CourtFileSearchInformation from './modules/CourtFileSearchInformation'

createPinia(Pinia).mount('#pinia')
//Vue.use(Vuex)
Vue.use(Pinia)

const store = new Pinia.Store({
  modules: {
    CivilFileInformation,
    CriminalFileInformation,
    CommonInformation,
    CourtListInformation,
    CourtFileSearchInformation
  }
})

export default store
