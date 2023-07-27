import "./RepositoriesList.scss";

import * as React from 'react';
import { ConfigurationContext, IRepository } from '../../../Services/ConfigurationService';
import * as SDK from 'azure-devops-extension-sdk';
import { Link } from 'azure-devops-ui/Link';
import { RepositoriesSort } from '../RepositoriesSort';
import { Button } from 'azure-devops-ui/Button';
import { Spinner } from 'azure-devops-ui/Spinner';
import { CommonServiceIds, IHostNavigationService } from 'azure-devops-extension-api';

export interface IRepositoriesListProps {
    sort: RepositoriesSort;
}

export interface IRepositoriesListState {
    repositories:  IRepository[];
    isLoading: boolean;
}

export class RepositoriesList extends React.Component<IRepositoriesListProps, IRepositoriesListState> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    constructor(props: IRepositoriesListProps) {
        super(props);

        this.state = {
            repositories: [],
            isLoading: true,
        }
    }

    private sortRepositories(repositories: IRepository[], sort: RepositoriesSort): IRepository[] {
        if (sort === RepositoriesSort.Alphabetical) {
            return repositories.sort((a, b) => a.name.localeCompare(b.name));
        }
        if (sort === RepositoriesSort.Stars) {
            return repositories.sort((a, b) => b.stars.count - a.stars.count);
        }
        if (sort === RepositoriesSort.LastCommitDate) {
            return repositories.sort((a, b) => {
                if (a.metadata.lastCommitDate && !b.metadata.lastCommitDate) {
                    return -1;
                }
                if (!a.metadata.lastCommitDate && b.metadata.lastCommitDate) {
                    return 1;
                }
                if (!a.metadata.lastCommitDate && !b.metadata.lastCommitDate) {
                    return 0;
                }
                 return b.metadata.lastCommitDate!.localeCompare(a.metadata.lastCommitDate!);
            });
        }
        console.log("Sort mode unknown");
        return repositories.sort();
    }

    public async componentDidMount() {
        await SDK.ready();
        await this.context.ensureAuthenticated();

        let repositories = await this.context.getRepositories();
        repositories = this.sortRepositories(repositories, this.props.sort);
        console.log("Repositories:", repositories);
        this.setState({
            repositories: repositories,
            isLoading: false,
        });
    }

    public async componentDidUpdate(previousProps: IRepositoriesListProps, previousState: IRepositoriesListState) {
        if (previousState.repositories !== this.state.repositories || previousProps.sort !== this.props.sort) {
            this.setState({
                repositories: this.sortRepositories(this.state.repositories, this.props.sort),
            });
        }
    }

    public render(): JSX.Element {
        const repositories = [];
        for (let i = 0; i < this.state.repositories.length; i++) {
            const repo = this.state.repositories[i];
            repositories.push(
                <div className="column subtle-border">
                    <h2 className="flex-row justify-space-between" style={{ margin: 0, marginBottom: "5px" }}>{repo.name} <Button iconProps={{iconName: repo.stars.isStarred ? "FavoriteStarFill" : "FavoriteStar"}} subtle={true} ariaLabel={repo.stars.isStarred ? "Unstar this repository" : "Star this repository"} onClick={async () => await this.starRepository(repo)}/></h2>
                    <p style={{ marginBottom: "5px" }}>{repo.badges.map(badge => (<><img key={badge.name} src={badge.url} alt={badge.name} /> </>))}</p>
                    {repo.description && <p style={{ marginBottom: "8px" }}>{this.state.repositories[i].description}</p>}
                    {repo.installation && <pre><code>{repo.installation}</code></pre>}
                    <a className="bolt-link" onClick={(event) => this.navigateToRepository(event, repo)}>Go to repository</a>
                    <Link href={repo.metadata.url}>Go to repository</Link>
                </div>
            );
        }

        const rowSize = 3;
        const rows = [];
        for (let i = 0; i < repositories.length; i += rowSize) {
            let columns = repositories.slice(i, i + rowSize);
            if (columns.length < rowSize) {
                columns = columns.concat(Array(rowSize - columns.length).fill(<div className="column"></div>));
            }
            rows.push(
                <div className="row">
                    {columns}
                </div>
            );
        }

        return (
            <>
                {this.state.isLoading &&
                    <div className="flex-row">
                        <Spinner label="loading" />
                    </div>
                }
                {!this.state.isLoading &&
                    <div className="repositories-list">
                        {rows}
                    </div>
                }
            </>
        );
    }

    private async starRepository(repository: IRepository): Promise<void> {
        const isStarred = repository.stars.isStarred;
        if (isStarred) {
            // TODO: Support unstarring
            console.error("Unstarring a repository is not supported yet");
        }
        else {
            await this.context.starRepository(repository.project, repository.id);
            this.setState((previousState, previousProps) => {
                previousState.repositories.find(x => x.id === repository.id)!.stars.isStarred = true;
                return {
                    repositories: previousState.repositories,
                };
            });
        }
    }

    private async navigateToRepository(event: React.MouseEvent<HTMLAnchorElement> | React.KeyboardEvent<HTMLAnchorElement>, repository: IRepository): Promise<void> {
        event.preventDefault();
        const navigationService = await SDK.getService<IHostNavigationService>(CommonServiceIds.HostNavigationService);
        navigationService.navigate(repository.metadata.url);
    }

    static defaultProps = {
        sort: RepositoriesSort.Alphabetical,
    }
}
