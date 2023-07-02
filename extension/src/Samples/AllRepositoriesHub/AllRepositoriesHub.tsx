import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { GitServiceIds, IVersionControlRepositoryService } from "azure-devops-extension-api/Git/GitServices";

import { Header, TitleSize } from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";

import { showRootComponent } from "../../Common";
import { GitRepository } from "azure-devops-extension-api/Git/Git";
import { Dropdown } from 'azure-devops-ui/Dropdown';
import { IHeaderCommandBarItem } from 'azure-devops-ui/HeaderCommandBar';
import { IListBoxItem } from 'azure-devops-ui/ListBox';
import { ListSelection } from 'azure-devops-ui/List';
import { IMenuButtonProps } from 'azure-devops-ui/Menu';
import { Button, IButtonProps } from 'azure-devops-ui/Button';
import { CommonServiceIds, IExtensionDataManager, IExtensionDataService } from 'azure-devops-extension-api';
import { ProjectAnalysisRestClient } from 'azure-devops-extension-api/ProjectAnalysis';

enum RepositoriesSort {
    Alphabetical = 0,
    Stars = 1,
    LastCommitDate = 2,
}
interface IAllRepositoriesHubContent {
    repository: GitRepository | null;
    sort: RepositoriesSort;
    sortSelection: ListSelection;
}

class AllRepositoriesHubContent extends React.Component<{}, IAllRepositoriesHubContent> {
    private _dataManager?: IExtensionDataManager;

    constructor(props: {}) {
        super(props);

        const selection = new ListSelection();
        selection.select(0, 1);

        this.state = {
            repository: null,
            sort: RepositoriesSort.Alphabetical,
            sortSelection: selection,
        };
    }

    public async componentWillMount() {
        SDK.init();
        //const repoSvc = await SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
        //const repository = await repoSvc.getCurrentGitRepository();

        const accessToken = await SDK.getAccessToken();
        console.log("SDK access token", accessToken);

        const appToken = await SDK.getAppToken();
        console.log("SDK app token", appToken);

        const response = await fetch("https://localhost:44400/testauth", {
            headers: {
                Authorization: 'Bearer ' + appToken,
                "X-AzureDevOps-AccessToken": accessToken,
            }
        });
        console.log("Fetched: ", response.status);

        /*this.setState({
            repository
        });*/
    }

    public async componentDidMount() {
        await SDK.ready();
        const accessToken = await SDK.getAccessToken();
        const extDataService = await SDK.getService<IExtensionDataService>(CommonServiceIds.ExtensionDataService);
        this._dataManager = await extDataService.getExtensionDataManager(SDK.getExtensionContext().id, accessToken);

        this._dataManager.getValue<string>("test-id").then((data) => {
            console.log("Set ext data", data);
        }, () => {
            console.error("Couldnt set ext data");
        });
    }

    public render(): JSX.Element {
        return (
            <Page className="sample-hub flex-grow">

                <Header title="Repository Information Sample Hub"
                        commandBarItems={this.getCommandBarItems()}
                    titleSize={TitleSize.Medium} />

                <div style={{marginLeft: 32}}>
                    <div className="flex-row flex-center">
                        <label htmlFor="message-level-picker">Message level: </label>
                        <Dropdown<RepositoriesSort>
                            className="margin-left-8"
                            items={[
                                { id: "info", data: RepositoriesSort.Alphabetical, text: "Alphabetical"},
                                { id: "error", data: RepositoriesSort.Stars, text: "Stars"},
                                { id: "Warning", data: RepositoriesSort.LastCommitDate, text: "Last commit"},
                            ]}
                            onSelect={this.onSortChanged}
                            selection={this.state.sortSelection}
                        />
                    </div>

                    <h3>ID</h3>
                    {
                        this.state.repository &&
                        <p>{this.state.repository.id}</p>
                    }
                    <h3>Name</h3>
                    {
                        this.state.repository &&
                        <p>{this.state.repository.name}</p>
                    }
                    <h3>URL</h3>
                    {
                        this.state.repository &&
                        <p>{this.state.repository.url}</p>
                    }
                </div>
            </Page>
        );
    }

    private onSortChanged = (event: React.SyntheticEvent<HTMLElement>, item: IListBoxItem<RepositoriesSort>): void => {
        console.log("Sort changed", item.data);
        this.setState({ sort: item.data ?? RepositoriesSort.Alphabetical });
    }

    private getCommandBarItems(): IHeaderCommandBarItem[] {
        return [
            {
                id: "panel",
                text: "Panel",
                iconProps: {
                    iconName: 'Add'
                },
                isPrimary: true,
                tooltipProps: {
                    text: "Open a panel with custom extension content"
                }
            },
            {
                id: "label-sort",
                text: "Sort",
            },
            {
                id: "sort",
                renderButton: (props: IButtonProps | IMenuButtonProps): JSX.Element => {
                    // TODO: https://developer.microsoft.com/en-us/azure-devops/components/menu
                    return (
                        <Dropdown<RepositoriesSort>
                            key="something"
                            className="margin-left-8"
                            items={[
                                { id: "alphabetical", data: RepositoriesSort.Alphabetical, text: "Alphabetical"},
                                { id: "stars", data: RepositoriesSort.Stars, text: "Stars"},
                                { id: "last-commit", data: RepositoriesSort.LastCommitDate, text: "Last commit"},
                            ]}
                            onSelect={this.onSortChanged}
                            selection={this.state.sortSelection}
                        />
                    );
                }
            },
            {
                id: "customDialog",
                text: "Custom Dialog",
                tooltipProps: {
                    text: "Open a dialog with custom extension content"
                }
            }
        ];
    }
}

showRootComponent(<AllRepositoriesHubContent />);
