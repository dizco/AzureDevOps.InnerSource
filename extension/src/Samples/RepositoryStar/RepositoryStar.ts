import "es6-promise/auto";
import * as SDK from "azure-devops-extension-sdk";
import { GitServiceIds, IVersionControlRepositoryService } from 'azure-devops-extension-api/Git/GitServices';
import { ConfigurationService } from '../../Services/ConfigurationService';
import { CommonServiceIds, IProjectPageService } from 'azure-devops-extension-api';

SDK.register("repository-menu-star", () => {
    return {
        execute: async () => {
            await SDK.init();
            console.log("Star!");

            const projectService = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
            const project = await projectService.getProject();
            if (!project) {
                console.error('Could not identify current project')
                return [];
            }

            const repoService = await SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
            const repository = await repoService.getCurrentGitRepository();

            if (!repository) {
                console.log("No repository currently selected");
                return;
            }
            console.log("Current repository", repository);

            const context = new ConfigurationService();
            await context.ensureAuthenticated();
            await context.starRepository(project.name, repository.name);
        }
    };
});

SDK.init();
