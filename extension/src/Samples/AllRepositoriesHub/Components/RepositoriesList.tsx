import "./RepositoriesList.scss";

import * as React from 'react';
import { useMemo } from 'react';
import { ConfigurationContext, IRepository } from '../../../Services/ConfigurationService';
import * as SDK from 'azure-devops-extension-sdk';
import { Link } from 'azure-devops-ui/Link';
import { RepositoriesSort } from '../RepositoriesSort';
import { Button } from 'azure-devops-ui/Button';

export interface IRepositoriesListProps {
    sort: RepositoriesSort;
}

export interface IRepositoriesListState {
    repositories:  IRepository[];
}

export class RepositoriesList extends React.Component<IRepositoriesListProps, IRepositoriesListState> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    state = {
        repositories: []
    }

    private visibleRepositories = useMemo(() => {
            const sorted = this.sortRepositories(this.state.repositories, this.props.sort);
            console.log("Sorted repositories", sorted);
            return sorted;
        },
        [this.state.repositories, this.props.sort]);

    constructor(props: IRepositoriesListProps) {
        super(props);
    }

    private sortRepositories(repositories: IRepository[], sort: RepositoriesSort): IRepository[] {
        if (sort === RepositoriesSort.Alphabetical) {
            return repositories.sort((a, b) => a.name.localeCompare(b.name));
        }
        if (sort === RepositoriesSort.Stars) {
            return repositories.sort((a, b) => a.stars.count - b.stars.count);
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
                 return a.metadata.lastCommitDate!.localeCompare(b.metadata.lastCommitDate!);
            });
        }
        console.log("Sort mode unknown");
        return repositories.sort();
    }

    public async componentDidMount() {
        await SDK.ready();
        await this.context.ensureAuthenticated();
        const repositories = await this.context.getRepositories();
        console.log("Repositories:", repositories);
        this.setState({
            repositories: repositories
        });
    }

    public render(): JSX.Element {
        const repositories = [];
        for (let i = 0; i < this.visibleRepositories.length; i++) {
            const repo = this.visibleRepositories[i];
            repositories.push(
                <div className="column subtle-border">
                    <h2 style={{ margin: 0, marginBottom: "5px" }}>{repo.name} <Button iconProps={{iconName: "FavoriteStar"}}/></h2>
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
            <div className="repositories-list">
                {rows}

                {/*<table id="repositoriesAggregation" style={{ width: "900px" }}>
                    <tbody>
                    <tr>
                        <td style={{ width: "450px" }}>
                            <h2 style={{ margin: 0, marginBottom: "5px" }}>Kiosoft</h2>
                            <p style={{ marginBottom: "5px" }}><img src="https://innersource.kiosoft.ca/stars/Kiosoft/Kiosoft" alt="Stars"/> <img src="https://innersource.kiosoft.ca/badges/last-commit/f4abd3dc-0616-4115-9148-4fc75090d17c" alt="Last commit"/> </p>
                            <p style={{ marginBottom: "8px" }}></p>
                            <p><a href="https://dev.azure.com/gabrielbourgault/Kiosoft/_git/Kiosoft">Go to project</a></p>
                        </td>
                        <td style={{ width: "450px" }}>
                            <h2 style={{ margin: 0, marginBottom: "5px" }}>InnerSource</h2>
                            <p style={{ marginBottom: "5px" }}><img src="https://innersource.kiosoft.ca/stars/Kiosoft/InnerSource" alt="Stars"/> <img src="https://innersource.kiosoft.ca/badges/last-commit/d14718b2-3f57-490a-bede-b648f02fc405" alt="Last commit"/> <img src="https://img.shields.io/badge/-512BD4?logo=.net" alt=".NET"/></p>
                            <p style={{ marginBottom: "8px" }}></p>
                            <p><a href="https://dev.azure.com/gabrielbourgault/Kiosoft/_git/InnerSource">Go to project</a></p>
                        </td>
                    </tr>
                    <tr>
                        <td style={{ width: "450px" }}>
                            <h2 style={{ margin: 0, marginBottom: "5px" }}>my-csharp-nuget</h2>
                            <p style={{ marginBottom: "5px" }}><img src="https://innersource.kiosoft.ca/stars/Kiosoft/my-csharp-nuget" alt="Stars"/> <img src="https://innersource.kiosoft.ca/badges/last-commit/709dc7c2-e47e-4139-869f-c626b3066325" alt="Last commit"/> <img src="https://img.shields.io/badge/-512BD4?logo=.net" alt=".NET"/></p>
                            <p style={{ marginBottom: "8px" }}>IdentityModel is a .NET standard helper library for claims-based identity, OAuth 2.0 and OpenID Connect.</p>
                            <pre><code>dotnet add package IdentityModel --version 6.1.0</code></pre>
                            <a href="https://dev.azure.com/gabrielbourgault/Kiosoft/_git/my-csharp-nuget">Go to project</a>
                        </td>
                        <td style={{ width: "450px" }}>
                            <h2 style={{ margin: 0, marginBottom: "5px" }}>my-best-node-package</h2>
                            <p style={{ marginBottom: "5px" }}><img src="https://innersource.kiosoft.ca/stars/Kiosoft/my-best-node-package" alt="Stars"/> <img src="https://innersource.kiosoft.ca/badges/last-commit/f8cecd80-069c-4085-9a6d-caf7607bb515" alt="Last commit"/> <img src="https://img.shields.io/badge/javascript-%23323330.svg?logo=javascript&amp;logoColor=%23F7DF1E" alt="JavaScript"/></p>
                            <p style={{ marginBottom: "8px" }}>A smart react component to scroll down automatically</p>
                            <pre><code>npm install --save react-scrollable-feed</code></pre>
                            <a href="https://dev.azure.com/gabrielbourgault/Kiosoft/_git/my-best-node-package">Go to project</a>
                        </td>
                    </tr>
                    <tr>
                        <td style={{ width: "450px" }}>
                            <h2 style={{ margin: 0, marginBottom: "5px" }}>my-best-node-package.fork</h2>
                            <p style={{ marginBottom: "5px" }}><img src="https://innersource.kiosoft.ca/stars/Kiosoft/my-best-node-package.fork" alt="Stars"/> <img src="https://innersource.kiosoft.ca/badges/last-commit/22570a83-ab8b-4c65-8835-f2cc6761a57c" alt="Last commit"/> <img src="https://img.shields.io/badge/TypeScript-007ACC?logo=typescript&amp;logoColor=white" alt="TypeScript"/></p>
                            <p style={{ marginBottom: "8px" }}>A smarter react component to scroll up and down automatically</p>
                            <pre><code>npm install --save react-scrollable-feed-fork</code></pre>
                            <a href="https://dev.azure.com/gabrielbourgault/Kiosoft/_git/my-best-node-package.fork">Go to project</a>
                        </td>
                    </tr>
                    </tbody>
                </table>*/}
            </div>
        );
    }

    static defaultProps = {
        sort: RepositoriesSort.Alphabetical,
    }
}
