import axios from 'axios';
import { minimalApiConfig } from '../config.js'

export const minimalApiClient = (() => {
    const getCurrentUser = async (token) => {
        if (!token) {
            return null;
        }

        var response = await axios
            .get(
                minimalApiConfig.apiUrl + 'users/current',
                { headers: { "Authorization" : 'Bearer ' + token } });

        return response.data;
    }; 

    return {
        getCurrentUser
    }
})();
