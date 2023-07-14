import * as SDK from 'azure-devops-extension-sdk';
import { CommonServiceIds, IExtensionDataService, IProjectPageService } from 'azure-devops-extension-api';
import React from 'react';

interface IConfiguration {
    serverUrl: string;
}

export interface IRepositoryBadge {
    name: string;
    url: string;
}
export interface IRepository {
    project: string;
    id: string;
    name: string;
    description: string;
    installation: string;
    webUrl: string;
    badges: IRepositoryBadge[];
}

export class ConfigurationService {
    private static readonly ConfigurationKey = "configuration";
    // TODO: Could keep a local, cached copy of the configuration for a certain amount of time

    private isAuthenticated = false;

    public async ensureAuthenticated(): Promise<void> {
        if (!await this.isReady()) {
            // Application is not properly configured
            return;
        }

        if (this.isAuthenticated) {
            // TODO: Invalidate this after a while
            return;
        }

        const accessToken = await SDK.getAccessToken();
        console.log("SDK access token", accessToken);

        const appToken = await SDK.getAppToken();
        console.log("SDK app token", appToken);

        const serverUrl = await this.getServerUrl();
        const response = await fetch(serverUrl + "/authenticate", {
            headers: {
                Authorization: 'Bearer ' + appToken,
                'X-AzureDevOps-AccessToken': accessToken,
            }
        });

        if (response.ok) {
            // TODO: Remove log
            console.log("Authentication success: ", response.status);
            this.isAuthenticated = true;
        }
        else {
            console.log("Authentication failed: ", response.status);
            this.isAuthenticated = false;
        }
    }

    public async getRepositories(): Promise<IRepository[]> {
        const projectService = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
        const project = await projectService.getProject();
        if (!project) {
            console.error('Could not identify current project')
            return [];
        }

        const serverUrl = await this.getServerUrl();
        const response = await fetch(serverUrl + "/repositories/" + project.id, {
            credentials: "include"
        });
        return (await response.json()).repositories;
    }

    public async isReady(): Promise<boolean> {
        return !!await this.getServerUrl();
    }

    public async setServerUrl(serverUrl: string): Promise<void> {
        await SDK.ready();
        const accessToken = await SDK.getAccessToken();
        const extDataService = await SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService);
        const dataManager = await extDataService.getExtensionDataManager(SDK.getExtensionContext().id, accessToken);

        await dataManager.setValue<IConfiguration>(ConfigurationService.ConfigurationKey, {
            serverUrl
        },{ scopeType: "Default" });
    }

    public async getServerUrl(): Promise<string|undefined> {
        await SDK.ready();
        const accessToken = await SDK.getAccessToken();
        const extDataService = await SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService);
        const dataManager = await extDataService.getExtensionDataManager(SDK.getExtensionContext().id, accessToken);

        let data;
        try {
            data = await dataManager.getValue<IConfiguration|undefined>(ConfigurationService.ConfigurationKey, { scopeType: "Default" });
        }
        catch (e) {
            // Swallow errors
            console.log("Could not find configuration", e);
        }

        return data?.serverUrl;
    }

    public async getBadgeJwtToken(repositoryId: string): Promise<string> {
        const serverUrl = await this.getServerUrl();
        const response = await fetch(serverUrl + `/token`, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json",
                "Accept": "application/json",
            },
            body: JSON.stringify({
                repositoryId
            })
        });
        console.log('Jwt response', response.status);
        return (await response.json()).token;
    }

    public async starRepository(projectName: string, repositoryName: string): Promise<void> {
        const serverUrl = await this.getServerUrl();
        const response = await fetch(`${serverUrl}/star/${projectName}/${repositoryName}`, {
            method: 'POST',
            headers: {
                "Accept": "application/json",
            }
        });
        console.log('Star response', response.status);
    }
}

export const ConfigurationContext = React.createContext(new ConfigurationService());
