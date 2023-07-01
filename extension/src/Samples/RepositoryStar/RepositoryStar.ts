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

            if (!repository) {
                console.log("No repository currently selected");
                return;
            }

            console.log("Current repository", repository);
        }
    };
});

SDK.init();
