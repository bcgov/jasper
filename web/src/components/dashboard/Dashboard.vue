<template>
    <div class="judges-dashboard">
        <header class="header">
            <div class="top-line">Court Today</div>
            <div class="bot-line">
                <div class="left">
                    Criminal
                </div>
                <div class="center">Scheduled:</div>
                <div class="right">Today's court list</div>
            </div>
        </header>
        <section class="dashboard-container">
            <div class="dashboard-calendar"><Calendar :events='ar' @monthChange="getMonthlySchedule"/></div>
            <div class="right-menu">
                <div>&nbsp;</div>
                <div>&nbsp;</div>
            </div>
        </section>
        <section class="dashboard-collapse-section">
            <div class="dashboard-collapse">Reserved Judgement (4)</div>
            <div class="dashboard-collapse">Reserved Judgement (5)</div>
        </section>
    </div>
</template>
<style>
header {
    width: 100%;
    max-width: 1200px;
    margin: 0 auto;
}

.top-line {
    background-color: rgba(157, 146, 146, 0.19);
    padding: 8px 15px;
    color: #fff;
}

.bot-line {
    display: flex;
    background-color: #8e8d8d;
    padding: 12px 15px;
    align-items: center;
    justify-content: space-between;
}

.dashboard-container {
    display: Flex;
    max-width: 1200px;
    margin: 20px auto;
    align-items: flex-start;
}

.dashboard-calendar {
    width: 80%;
}

.right-menu {
    width: 19%;
    margin-left: 10px;
    background-color: white;
    /* uncomment when ready to develop:gray; */
    padding: 12px;
    box-sizing: border-box;
    height: 300px;
}

.dashboard-collapse-section {
    margin: 0 auto;
    max-width: 1200px;
    width: 100%;
    padding-bottom: 100px;
}

.dashboard-collapse {
    border-bottom: 1px solid #000;
    color: #000;
    padding: 10px;
    max-width: 500px;
}
</style>
<script lang="ts">
import NavigationTopbar from "@components/NavigationTopbar.vue";
import Calendar from '@components/calendar/Calendar.vue';
import { Component, Vue } from 'vue-property-decorator';
@Component({
    components: {
        NavigationTopbar,
        Calendar
    }
})
export default class Dashboard extends Vue { 
 
 public ar = [{ title: 'Today Meeting', start: new Date() }];
  public getMonthlySchedule(currentMonth): void {
    this.ar = [];
     this.$http
      .get(
        "api/assignments/monthly-schedule/" +
        `${currentMonth.getFullYear()}/${String(currentMonth.getMonth() + 1).padStart(2, '0')}`
      )
      .then(
        (Response) => Response.json(),
        (err) => {
            this.$bvToast.toast(`Error - ${err.url} - ${err.status} - ${err.statusText}`, {
                title: "An error has occured.",
                variant: "danger",
                autoHideDelay: 10000,
            });
          console.log(err);
        }
      )
      .then((data) => {
        if (data) {
          this.ar = data;
        } else {
          window.alert("bad data!");
        }
        
      });
  }
}
</script>