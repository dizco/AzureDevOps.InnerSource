import "./ExtensionSettingsHub.scss";
import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";
import { Header, TitleSize } from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";
import { showRootComponent } from "../../Common";
import { ConfigurationContext, ConfigurationService } from '../../Services/ConfigurationService';
import { ServerSettings } from './Components/ServerSettings';
import { getClient } from 'azure-devops-extension-api';
import { FeatureManagementRestClient } from 'azure-devops-extension-api/FeatureManagement';

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

        const extensionId = SDK.getExtensionContext().id;

        // TODO: Try to query the Rest API to know the status of the feature flag
        // https://learn.microsoft.com/en-us/javascript/api/azure-devops-extension-api/featuremanagementrestclient
        /*const featureClient = await getClient(FeatureManagementRestClient);
        const featureState1 = await featureClient.getFeatureState(`${extensionId}.feature-innersource`, "me");
        console.log("Feature state 1", featureState1);

        const featureState2 = await featureClient.getFeatureState(`${extensionId}.feature-innersource`, "host");
        console.log("Feature state 2", featureState2);

        const featureState3 = featureClient.getFeature(`${extensionId}.feature-innersource`);
        console.log("Feature state 3", featureState3);*/
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
