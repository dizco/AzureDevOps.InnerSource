import "./AllRepositoriesHub.scss";
import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { Header, TitleSize } from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";
import { showRootComponent } from "../../Common";
import { Dropdown, DropdownExpandableButton } from 'azure-devops-ui/Dropdown';
import { IHeaderCommandBarItem } from 'azure-devops-ui/HeaderCommandBar';
import { IListBoxItem } from 'azure-devops-ui/ListBox';
import { ConfigurationContext, ConfigurationService } from '../../Services/ConfigurationService';
import { Settings } from './Components/Settings';
import { RepositoriesList } from './Components/RepositoriesList';
import { DropdownSelection } from 'azure-devops-ui/Utilities/DropdownSelection';
import { RepositoriesSort } from './RepositoriesSort';

interface IAllRepositoriesHubContent {
    sort: RepositoriesSort;
    sortSelection: DropdownSelection;
}

class AllRepositoriesHubContent extends React.Component<{}, IAllRepositoriesHubContent> {
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

        const selection = new DropdownSelection();
        selection.select(0, 1);

        this.state = {
            sort: RepositoriesSort.Alphabetical,
            sortSelection: selection,
        };

    }

    public async componentWillMount() {
        await SDK.init();
    }

    public async componentDidMount() {
        await SDK.ready();
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
                        <label htmlFor="message-level-picker">Sort by: </label>
                        <Dropdown<RepositoriesSort>
                            className="repository-sort margin-left-8"
                            placeholder="Sort by"
                            items={this.sortItems}
                            onSelect={this.onSortChanged}
                            selection={this.state.sortSelection}
                            renderExpandable={props => <DropdownExpandableButton {...props} className="repository-sort" />}
                        />
                    </div>
                    <RepositoriesList sort={this.state.sort} />
                    <Settings/>
                </div>
            </Page>
        );
    }

    private onSortChanged = (event: React.SyntheticEvent<HTMLElement>, item: IListBoxItem<RepositoriesSort>): void => {
        console.log("Sort changed", item.data);
        this.setState({ sort: item.data ?? RepositoriesSort.Alphabetical });
        // TODO: Could set this as a preference for the user in devops data storage
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
                id: "customDialog",
                text: "Custom Dialog",
                tooltipProps: {
                    text: "Open a dialog with custom extension content"
                }
            }
        ];
    }
}

showRootComponent(
    <ConfigurationContext.Provider value={new ConfigurationService()}>
        <AllRepositoriesHubContent />
    </ConfigurationContext.Provider>
);
