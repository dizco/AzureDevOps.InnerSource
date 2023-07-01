import "es6-promise/auto";
import * as SDK from "azure-devops-extension-sdk";
import { CommonServiceIds, getClient, IHostPageLayoutService, IProjectPageService } from "azure-devops-extension-api";
import { ProjectAnalysisRestClient } from 'azure-devops-extension-api/ProjectAnalysis';

SDK.register("pipeline-menu", () => {
    return {
        execute: async () => {
            await SDK.init();
            const projectService = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
            const project = await projectService.getProject();
            let analytics = {};
            if (project) {
                analytics = await getClient(ProjectAnalysisRestClient).getProjectLanguageAnalytics(project?.id);
                console.log("Analytics", analytics);
            }

            const dialogSvc = await SDK.getService<IHostPageLayoutService>(CommonServiceIds.HostPageLayoutService);
            dialogSvc.openMessageDialog(`Fetched metrics ${project?.id}: ${JSON.stringify(analytics)}`, { showCancel: false });
        }
    }
});

SDK.init();
