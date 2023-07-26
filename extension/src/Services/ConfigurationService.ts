import * as SDK from 'azure-devops-extension-sdk';
import { CommonServiceIds, IExtensionDataService, IProjectPageService } from 'azure-devops-extension-api';
import React from 'react';
import axios, { RawAxiosRequestConfig } from 'axios';

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
        const response = await fetch(serverUrl + "/token", {
            method: "POST",
            headers: {
                Authorization: 'Bearer ' + appToken,
                'X-AzureDevOps-AccessToken': accessToken,
            }
        });

        if (response.ok) {
            const json: {accessToken: string, expiresInSeconds: number} = await response.json();
            document.cookie = ConfigurationService.AuthenticationCookieName + "=" + json.accessToken + "; Max-age=" + json.expiresInSeconds + ";SameSite=Strict; Secure";

            // TODO: Remove log
            console.log("Authentication success: ", response.status);
            this.isAuthenticated = true;
        }
        else {
            console.log("Authentication failed: ", response.status);
            this.isAuthenticated = false;
        }
    }

    private getJwtBearer(): string {
        return this.getCookie(ConfigurationService.AuthenticationCookieName);
    }

    // Source: https://stackoverflow.com/a/15724300/6316091
    private getCookie(name: string): string {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            return parts.pop().split(';').shift();
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
            headers: {
                Authorization: 'Bearer ' + this.getJwtBearer(),
            }
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
