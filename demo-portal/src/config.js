import { SSMClient, GetParameterCommand } from "@aws-sdk/client-ssm";

export const minimalApiConfig = {
    apiUrl: process.env.VUE_APP_MINIMAL_API_URL
};

export const googleConfig = {
    clientId: process.env.VUE_APP_GOOGLE_CLIENT_ID
};

const params = [
    {
        paramName: '/minimal-api/googleConfig/portalClientId',
        envKey: 'VUE_APP_GOOGLE_CLIENT_ID'
    }
];

export const hydrateEnvFromSsm = async (env) => {
    const client = new SSMClient();

    for (const entry of params) {
        var resp = await client.send(
            new GetParameterCommand({
                Name: entry.paramName,
                WithDecryption: false,
            }));

        env[entry.envKey] = resp.Parameter.Value;
    }
};
