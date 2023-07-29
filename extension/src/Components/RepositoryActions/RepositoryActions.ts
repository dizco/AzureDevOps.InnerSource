import "es6-promise/auto";
import * as SDK from "azure-devops-extension-sdk";
import { CommonServiceIds, IHostNavigationService } from "azure-devops-extension-api";

SDK.register("view-all-repositories-action", () => {
    return {
        execute: async () => {
            const navigationService = await SDK.getService<IHostNavigationService>(CommonServiceIds.HostNavigationService);
            const host = SDK.getHost().name;
            const project = (await navigationService.getPageRoute()).routeValues.project;
            const extensionId = SDK.getExtensionContext().id;
            navigationService.navigate(`https://dev.azure.com/${host}/${project}/_apps/hub/${extensionId}.all-repositories-hub`);
        }
    }
});

SDK.init();
