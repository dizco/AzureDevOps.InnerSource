import "./RepositoriesList.scss";

import * as React from 'react';
import { ConfigurationContext, IRepository } from '../../../Services/ConfigurationService';
import * as SDK from 'azure-devops-extension-sdk';
import { Link } from 'azure-devops-ui/Link';
import { RepositoriesSort } from '../RepositoriesSort';
import { Button } from 'azure-devops-ui/Button';
import { Spinner } from 'azure-devops-ui/Spinner';

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
                    <h2 className="flex-row justify-space-between" style={{ margin: 0, marginBottom: "5px" }}>{repo.name} <Button iconProps={{iconName: repo.stars.isStarred ? "FavoriteStarFill" : "FavoriteStar"}} subtle={true} ariaLabel="Star this repository" onClick={() => this.starRepository(repo)}/></h2>
                    <p style={{ marginBottom: "5px" }}>{repo.badges.map(badge => (<><img key={badge.name} src={badge.url} alt={badge.name} /> </>))}</p>
                    {repo.description && <p style={{ marginBottom: "8px" }}>{this.state.repositories[i].description}</p>}
                    {repo.installation && <pre><code>{this.state.repositories[i].installation}</code></pre>}
                    <Link href={repo.metadata.url}>Go to project</Link>
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

    private starRepository(repository: IRepository): void {
        console.log("Star repo! (to be implemented)", repository);
        // TODO: Implement
    }

    static defaultProps = {
        sort: RepositoriesSort.Alphabetical,
    }
}
