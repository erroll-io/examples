import { defineStore } from 'pinia';
import { useLocalStorage } from "@vueuse/core"

export const useUserStore = defineStore({
    id: 'user',
    state: () => {
        return {
            id_token: useLocalStorage('id_token', ''),
        };
    }
});
