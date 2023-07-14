import "es6-promise/auto";
import * as SDK from "azure-devops-extension-sdk";
import { CommonServiceIds, IHostPageLayoutService } from "azure-devops-extension-api";
import { GitServiceIds, IVersionControlRepositoryService } from 'azure-devops-extension-api/Git/GitServices';

SDK.register("repository-menu-badge", () => {
    return {
        execute: async () => {
            await SDK.init();
            console.log("generate status badge!");

            const repoService = await SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
            const repository = await repoService.getCurrentGitRepository();

            if (!repository) {
                console.log("No repository currently selected");
                return;
            }

            const panelService = await SDK.getService<IHostPageLayoutService>(CommonServiceIds.HostPageLayoutService);
            panelService.openPanel<boolean | undefined>(SDK.getExtensionContext().id + ".repository-status-badge-panel-content", {
                title: "Status badges",
                configuration: {
                    repository,
                }
            });
        }
    };
});

SDK.init();
