import "./RepositoriesList.scss";

import * as React from 'react';
import { ConfigurationContext, IRepository } from '../../../Services/ConfigurationService';
import * as SDK from 'azure-devops-extension-sdk';

export interface IRepositoriesListState {
    repositories:  IRepository[];
}

export class RepositoriesList extends React.Component<{}, IRepositoriesListState> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    constructor(props: {}) {
        super(props);
        this.state = {
            repositories: [],
        };
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
        // TODO: Use navigation service to go to the project repo, otherwise it loads the page within the iframe.
        for (let i = 0; i < this.state.repositories.length; i++) {
            repositories.push(
                <div className="column subtle-border">
                    <h2 style={{ margin: 0, marginBottom: "5px" }}>{this.state.repositories[i].name}</h2>
                    <p style={{ marginBottom: "5px" }}>{this.state.repositories[i].badges.map(badge => (<><img key={badge.name} src={badge.url} alt={badge.name} /> </>))}</p>
                    {this.state.repositories[i].description && <p style={{ marginBottom: "8px" }}>{this.state.repositories[i].description}</p>}
                    {this.state.repositories[i].installation && <pre><code>{this.state.repositories[i].installation}</code></pre>}
                    <a href={this.state.repositories[i].webUrl}>Go to project</a>
                </div>
            );
        }

        const rowSize = 3;
        const rows = [];
        for (let i = 0; i <= repositories.length; i += rowSize) {
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
                <h2>Repositories</h2>

                {rows}

                <table id="repositoriesAggregation" style={{ width: "900px" }}>
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
                </table>
            </div>
        );
    }
}
