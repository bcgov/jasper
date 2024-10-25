import { Logger as SplunkLogger } from 'splunk-logging';
import Vue from 'vue';

export const SessionManager = {
    getSettings: async function(store) {
        try {
            if (!store.state.userInfo) {
                const response = await Vue.http.get('api/auth/info');
                store.commit("CommonInformation/setEnableArchive", response.body.enableArchive);
                store.commit("CommonInformation/setUserInfo", response.body);
            }
            return true;
        }
        catch (error) {
            console.log(error);  
            return false;
        }
    }
}

export const splunkLog = (message) => {
    const token = process.env["SPLUNK_TOKEN"] || "";
    const url = process.env["SPLUNK_COLLECTOR_URL"] || "";

    if (token && url) {
        const config = {
            token: token,
            url: url
        };
    
        const Logger = new SplunkLogger(config);
        const payload = {
            message: message
        };
    
        Logger.send(payload, (err, resp, body)=> {
            console.log("Response from Splunk", body);
        });
    }
  };
