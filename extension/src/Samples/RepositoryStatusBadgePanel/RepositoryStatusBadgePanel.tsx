import "./RepositoryStatusBadgePanel.scss";

import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";

import { Button } from "azure-devops-ui/Button";
import { ButtonGroup } from "azure-devops-ui/ButtonGroup";
import { showRootComponent } from "../../Common";
import { GitRepository } from 'azure-devops-extension-api/Git/Git';

interface IPanelContentState {
    repository?: GitRepository;
    ready?: boolean;
}

class RepositoryStatusBadgePanel extends React.Component<{}, IPanelContentState> {
    
    constructor(props: {}) {
        super(props);
        this.state = {};
    }

    public componentDidMount() {
        SDK.init();
        
        SDK.ready().then(() => {
            const config = SDK.getConfiguration();
            const repository = config.repository;
            this.setState({ repository, ready: true });

            if (config.dialog) {
                // Give the host frame the size of our dialog content so that the dialog can be sized appropriately.
                // This is the case where we know our content size and can explicitly provide it to SDK.resize. If our
                // size is dynamic, we have to make sure our frame is visible before calling SDK.resize() with no arguments.
                // In that case, we would instead do something like this:
                //
                // SDK.notifyLoadSucceeded().then(() => {
                //    // we are visible in this callback.
                //    SDK.resize();
                // });
                SDK.resize(400, 400);
            }
        });
    }

    public render(): JSX.Element {
        const { repository, ready } = this.state;

        return (
            <div className="sample-panel flex-column flex-grow">
                <div className="flex-grow flex-column flex-center justify-center" style={{ border: "1px solid #eee", margin: "10px 0" }}>
                    Additional content placeholder {repository?.name}
                </div>
                <ButtonGroup className="sample-panel-button-bar">
                    <Button
                        primary={true}
                        text="OK"
                    />
                    <Button
                        text="Cancel"
                    />
                </ButtonGroup>
            </div>
        );
    }
}

showRootComponent(<RepositoryStatusBadgePanel />);
