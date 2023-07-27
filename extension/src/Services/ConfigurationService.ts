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
    stars: {
        count: number;
        isStarred: boolean;
    };
    metadata: {
        url: string;
        lastCommitDate: string | null;
    };
    badges: IRepositoryBadge[];
}

export class ConfigurationService {
    private static readonly AuthenticationCookieName = "ado.innersource.authentication";
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

        if (this.getJwtBearer()) {
            console.log("Authentication session still active.");
            return;
        }

        const accessToken = await SDK.getAccessToken();
        console.log("SDK access token", accessToken);

        const appToken = await SDK.getAppToken();
        console.log("SDK app token", appToken);

        const serverUrl = await this.getServerUrl();
        const response = await fetch(`${serverUrl}/token`, {
            method: "POST",
            headers: {
                Authorization: 'Bearer ' + appToken,
                'X-AzureDevOps-AccessToken': accessToken,
            }
        });

        if (response.ok) {
            const json: {accessToken: string, expiresInSeconds: number} = await response.json();
            console.log("Received access token", json);
            document.cookie = "adocookie0=potato; SameSite=Strict; Secure";
            document.cookie = "adocookie1=" + json.accessToken + "; SameSite=Strict; Secure";
            document.cookie = "adocookie2=" + json.accessToken + "; Max-Age=" + json.expiresInSeconds + "; SameSite=Strict; Secure";
            document.cookie = ConfigurationService.AuthenticationCookieName + "=" + json.accessToken + "; SameSite=Strict; Secure";
            console.log("Set auth cookie", ConfigurationService.AuthenticationCookieName + "=" + json.accessToken + "; Max-age=" + json.expiresInSeconds + ";SameSite=Strict; Secure");
            console.log("Auth cookies", document.cookie);

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
        const response = await fetch(`${serverUrl}/${project.id}/repositories`, {
            headers: {
                Authorization: 'Bearer ' + this.getJwtBearer(),
            }
        });
        if (response.ok) {
            return (await response.json()).repositories;
        }
        else {
            console.error("Could not get repositories", response.status);
            return [];
        }
    }

    public async getBadgeJwtToken(repositoryId: string): Promise<string> {
        const projectService = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
        const project = await projectService.getProject();
        if (!project) {
            console.error('Could not identify current project')
            return "";
        }

        const serverUrl = await this.getServerUrl();
        const response = await fetch(`${serverUrl}/${project.name}/repositories/${repositoryId}/badges/token`, {
            method: 'POST',
            headers: {
                "Accept": "application/json",
            }
        });
        console.log('Jwt response', response.status);
        return (await response.json()).accessToken;
    }

    public async starRepository(projectName: string, repositoryName: string): Promise<void> {
        const serverUrl = await this.getServerUrl();
        const response = await fetch(`${serverUrl}/${projectName}/${repositoryName}/stars`, {
            method: 'POST',
            headers: {
                "Accept": "application/json",
            }
        });
        console.log('Star response', response.status);
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

    private getJwtBearer(): string | undefined {
        return this.getCookie(ConfigurationService.AuthenticationCookieName);
    }

    // Source: https://stackoverflow.com/a/15724300/6316091
    private getCookie(name: string): string | undefined {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            return parts.pop()
                ?.split(';')
                .shift();
        }
        return undefined;
    }
}

export const ConfigurationContext = React.createContext(new ConfigurationService());
