import "es6-promise/auto";
import * as SDK from "azure-devops-extension-sdk";
import { GitServiceIds, IVersionControlRepositoryService } from 'azure-devops-extension-api/Git/GitServices';

SDK.register("repository-menu-star", () => {
    return {
        execute: async () => {
            await SDK.init();
            console.log("Star!");

            const repoService = await SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
            const repository = await repoService.getCurrentGitRepository();


            const accessToken = await SDK.getAccessToken();
            console.log("SDK access token", accessToken);

            const appToken = await SDK.getAppToken();
            console.log("SDK app token", appToken);

            // TODO: Call server with appToken as bearer token, but also sending the access token in the payload
            // TODO: Server validates appToken signature from extension certificate https://marketplace.visualstudio.com/manage/publishers/gabrielbourgault?auth_redirect=True
            // TODO: Server calls https://dev.azure.com/gabrielbourgault/_apis/connectionData with the access token to get authenticated user id

            if (!repository) {
                console.log("No repository currently selected");
                return;
            }

            console.log("Current repository", repository);
        }
    };
});

SDK.init();
