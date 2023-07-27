import "./RepositoryStatusBadgePanel.scss";

import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { showRootComponent } from "../../Common";
import { GitRepository } from 'azure-devops-extension-api/Git/Git';
import { Location } from "azure-devops-ui/Utilities/Position";
import { TextField } from 'azure-devops-ui/TextField';
import { ConfigurationService, ConfigurationContext } from '../../Services/ConfigurationService';
import { Observer } from 'azure-devops-ui/Observer';
import { ClipboardButton } from 'azure-devops-ui/Clipboard';
import { ITooltipProps } from 'azure-devops-ui/TooltipEx';
import { IProjectInfo } from 'azure-devops-extension-api';
import { Spinner } from 'azure-devops-ui/Spinner';

interface IPanelContentState {
    project?: IProjectInfo;
    repository?: GitRepository;
    starBadgeSrc?: string;
    starBadgeMarkdown?: string;
    lastCommitBadgeMarkdown?: string;
    lastCommitBadgeMarkdown?: string;
    badgeJwt?: string;
    lastCopied: number;
    isLoading: boolean;
}

class RepositoryStatusBadgePanel extends React.Component<{}, IPanelContentState> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    private copyToClipboardLabel = "Copy to Clipboard";

    constructor(props: {}) {
        super(props);
        this.state = {
            lastCopied: -1,
            isLoading: true,
        };
    }

    private getTooltip = (index: number): ITooltipProps => {
        return {
            text: this.state.lastCopied === index ? "Copied to clipboard!" : "Click to copy",
            anchorOrigin: {
                horizontal: Location.center,
                vertical: Location.end,
            },
            tooltipOrigin: {
                horizontal: Location.center,
                vertical: Location.start,
            },
        };
    };

    public async componentDidMount() {
        await SDK.init();
        
        SDK.ready().then(() => {
            const config = SDK.getConfiguration();
            const project: GitRepository = config.project;
            const repository: GitRepository = config.repository;
            this.setState({ project, repository });

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

        await SDK.ready();
        await this.context.ensureAuthenticated();
        const serverUrl = await this.context.getServerUrl();
        if (!!this.state.repository?.id) {
            const badgeJwt = await this.context.getBadgeJwtToken(this.state.repository?.id!);
            this.setState({
                badgeJwt
            });
        }

        this.setState((previousState, props) => {
            const starSrc = `${serverUrl}/${previousState.project?.name}/repositories/${previousState.repository?.id}/badges/stars?access_token=${previousState.badgeJwt}`;
            const lastCommitSrc = `${serverUrl}/${previousState.project?.name}/repositories/${previousState.repository?.id}/badges/last-commit?access_token=${previousState.badgeJwt}`;
            return {
                starBadgeSrc: starSrc,
                starBadgeMarkdown: `![Repository stars](${starSrc})`,
                lastCommitBadgeSrc: lastCommitSrc,
                lastCommitBadgeMardown: `![Last commit date](${lastCommitSrc})`,
                isLoading: false,
            })
        });
    }

    public render(): JSX.Element {
        const { starBadgeSrc, starBadgeMarkdown, lastCommitBadgeSrc, lastCommitBadgeMarkdown } = this.state;

        return (
            <div className="flex-grow">
                {this.state.isLoading &&
                    <div className="flex-row flex-center">
                        <Spinner label="loading" />
                    </div>
                }
                {!this.state.isLoading &&
                    <>
                        <div className="flex-row justify-center">
                            <Spinner label="loading" />
                        </div>
                <div>
                    {starBadgeSrc && starBadgeMarkdown && (<>
                        <img className="status-badge-image" alt="Stars badge" src={starBadgeSrc} />
                        <div className="status-badge-text-wrapper">
                            <div className="status-badge-url-textfield flex-column">
                                <TextField value={starBadgeMarkdown} onChange={(e, value) => this.setState({
                                    starBadgeMarkdown: value,
                                })} label="Stars badge (markdown)" />
                            </div>
                            <Observer value={starBadgeMarkdown}>
                                {(observerProps: { value: string }) => (
                                    <ClipboardButton
                                        className="status-badge-url-copy-button"
                                        ariaLabel={observerProps.value + " " + this.copyToClipboardLabel}
                                        getContent={() => starBadgeMarkdown || ""}
                                        onCopy={() => (this.setState({lastCopied: 1}))}
                                        tooltipProps={this.getTooltip(1)}
                                        subtle={true}
                                    />
                                )}
                            </Observer>
                        </div>
                    </>)}
                </div>
                <div className="separator-line-top">
                    {lastCommitBadgeSrc && lastCommitBadgeMarkdown && (<>
                        <img className="status-badge-image" alt="Last commit date badge" src={lastCommitBadgeSrc} />
                        <div className="status-badge-text-wrapper">
                            <div className="status-badge-url-textfield flex-column">
                                <TextField value={lastCommitBadgeMarkdown} onChange={(e, value) => this.setState({
                                    lastCommitBadgeMardown: value,
                                })} label="Last commit date badge (markdown)" />
                            </div>
                            <Observer value={lastCommitBadgeMarkdown}>
                                {(observerProps: { value: string }) => (
                                    <ClipboardButton
                                        className="status-badge-url-copy-button"
                                        ariaLabel={observerProps.value + " " + this.copyToClipboardLabel}
                                        getContent={() => lastCommitBadgeMarkdown || ""}
                                        onCopy={() => (this.setState({lastCopied: 2}))}
                                        tooltipProps={this.getTooltip(2)}
                                        subtle={true}
                                    />
                                )}
                            </Observer>
                        </div>
                    </>)}
                </div>
                    </>
                }
                <div className="separator-line-top">
                    <p>Status badges are private and secured with a token that expires in 1 year.</p>
                </div>
            </div>
        );
    }
}

showRootComponent(
    <ConfigurationContext.Provider value={new ConfigurationService()}>
        <RepositoryStatusBadgePanel />
    </ConfigurationContext.Provider>);
