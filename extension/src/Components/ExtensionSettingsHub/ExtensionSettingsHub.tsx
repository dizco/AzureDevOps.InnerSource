import "./ExtensionSettingsHub.scss";
import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { Header, TitleSize } from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";
import { showRootComponent } from "../../Common";
import { ConfigurationContext, ConfigurationService } from '../../Services/ConfigurationService';
import { ServerSettings } from './Components/ServerSettings';

interface IExtensionSettingsHubContent {}

export class ExtensionSettingsHub extends React.Component<{}, IExtensionSettingsHubContent> {
    static contextType = ConfigurationContext;
    context!: React.ContextType<typeof ConfigurationContext>;

    constructor(props: {}) {
        super(props);

        this.state = {};
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

                <Header title="AzureDevOps.InnerSource" titleSize={TitleSize.Large} />

                <div className="page-content">
                    <h2>Settings</h2>
                    <ServerSettings/>
                </div>
            </Page>
        );
    }
}

showRootComponent(
    <ConfigurationContext.Provider value={new ConfigurationService()}>
        <ExtensionSettingsHub />
    </ConfigurationContext.Provider>
);
