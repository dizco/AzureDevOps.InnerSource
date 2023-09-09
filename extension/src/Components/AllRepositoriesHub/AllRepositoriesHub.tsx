import "./AllRepositoriesHub.scss";
import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { Header, TitleSize } from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";
import { showRootComponent } from "../../Common";
import { Dropdown, DropdownExpandableButton } from 'azure-devops-ui/Dropdown';
import { IListBoxItem } from 'azure-devops-ui/ListBox';
import { ConfigurationContext, ConfigurationService } from '../../Services/ConfigurationService';
import { RepositoriesList } from './Components/RepositoriesList';
import { DropdownSelection } from 'azure-devops-ui/Utilities/DropdownSelection';
import { RepositoriesSort } from './RepositoriesSort';
import { IHeaderCommandBarItem } from 'azure-devops-ui/HeaderCommandBar';
import { CommonServiceIds, IHostNavigationService } from 'azure-devops-extension-api';

interface IAllRepositoriesHubContent {
    sort: RepositoriesSort;
    sortSelection: DropdownSelection;
    repositoriesListKey: number; // Used to force a rerender when user clicks "refresh"
}

export class AllRepositoriesHub extends React.Component<{}, IAllRepositoriesHubContent> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    private sortItems: IListBoxItem<RepositoriesSort>[] = [
        { id: RepositoriesSort.Alphabetical.toString(), data: RepositoriesSort.Alphabetical, text: "Alphabetical"},
        { id: RepositoriesSort.Stars.toString(), data: RepositoriesSort.Stars, text: "Most stars"},
        { id: RepositoriesSort.LastCommitDate.toString(), data: RepositoriesSort.LastCommitDate, text: "Most recent commit"},
        { id: RepositoriesSort.MyStars.toString(), data: RepositoriesSort.MyStars, text: "My stars"},
    ];

    constructor(props: {}) {
        super(props);

        const sortSelection = new DropdownSelection();
        sortSelection.select(0);

        this.state = {
            sort: RepositoriesSort.Alphabetical,
            sortSelection,
        };
    }

    public async componentWillMount() {
        await SDK.init();
    }

    public async componentDidMount() {
        await SDK.ready();

        const sort = await this.context.getUserPreferrence<RepositoriesSort>("hubsort");
        if (sort) {
            this.setState((previousState: IAllRepositoriesHubContent, previousProps) => {
                let index = this.sortItems.findIndex(x => x.data === sort);
                if (index < 0) { // If item is not found
                    index = 0;
                }
                previousState.sortSelection.select(index);
                return { sort: sort };
            });
        }
    }

    public render(): JSX.Element {
        return (
            /*<ZeroData imageAltText={}/>*/
            <Page className="sample-hub flex-grow">

                <Header title="Repositories"
                        commandBarItems={this.getCommandBarItems()}
                        titleSize={TitleSize.Large} />

                <div className="page-content">
                    <div className="flex-row flex-center">
                        <label htmlFor="message-level-picker" style={{marginRight: "5px"}}>Sort by:</label>
                        <Dropdown<RepositoriesSort>
                            className="repository-sort margin-left-8"
                            placeholder="Sort by"
                            items={this.sortItems}
                            onSelect={this.onSortChanged}
                            selection={this.state.sortSelection}
                            renderExpandable={props => <DropdownExpandableButton {...props} className="repository-sort" />}
                        />
                    </div>
                    <RepositoriesList sort={this.state.sort} key={this.state.repositoriesListKey} />
                </div>
            </Page>
        );
    }

    private getCommandBarItems(): IHeaderCommandBarItem[] {
        return [
            {
                id: "refresh",
                onActivate: () => {
                    this.refresh();
                },
                iconProps: {
                    iconName: 'Refresh'
                },
                subtle: true
            },
            {
                id: "settings",
                text: "Settings",
                onActivate: () => {
                    this.navigateToSettings();
                },
                iconProps: {
                    iconName: 'Settings'
                },
                tooltipProps: {
                    text: "Open the extension settings"
                }
            },
        ];
    }

    private async refresh(): Promise<void> {
        this.setState((previousState, previousProps) => {
            return {
                repositoriesListKey: previousState.repositoriesListKey + 1,
            };
        });
    }

    private async navigateToSettings(): Promise<void> {
        const navigationService = await SDK.getService<IHostNavigationService>(CommonServiceIds.HostNavigationService);
        const host = SDK.getHost().name;
        const extensionId = SDK.getExtensionContext().id;
        navigationService.navigate(`https://dev.azure.com/${host}/_settings/${extensionId}.extension-settings-hub`);
    }

    private onSortChanged = async (event: React.SyntheticEvent<HTMLElement>, item: IListBoxItem<RepositoriesSort>): Promise<void> => {
        const sort = item.data ?? RepositoriesSort.Alphabetical;
        await this.context.setUserPreferrence<RepositoriesSort>("hubsort", sort);
        this.setState({ sort: sort });
        console.log("Sort changed", sort);
    }
}

showRootComponent(
    <ConfigurationContext.Provider value={new ConfigurationService()}>
        <AllRepositoriesHub />
    </ConfigurationContext.Provider>
);
