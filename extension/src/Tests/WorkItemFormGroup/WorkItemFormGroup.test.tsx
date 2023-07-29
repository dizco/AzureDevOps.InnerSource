// Imports
import '@testing-library/jest-dom/extend-expect'
import {
    fireEvent,
    render,
    screen,
    waitFor,
    waitForElementToBeRemoved
} from '@testing-library/react';
import React from 'react';
//import WorkItemFormGroupComponent from '../../Samples/WorkItemFormGroup/WorkItemFormGroup'
//import { AllRepositoriesHub } from '../../Components/AllRepositoriesHub/AllRepositoriesHub';
import { RepositoryStatusBadgePanel } from '../../Components/RepositoryStatusBadgePanel/RepositoryStatusBadgePanel';
import { mockSetFieldValue, spyWorkItemCallBackAccessor } from '../../__mocks__/azure-devops-extension-sdk'

// AzDO related Mocks are loaded automatically (implementations /src/__mocks__)

// Loading samples related mocks
jest.mock('../../Common');

describe('WorkItemFormGroup', () => {

    test('WorkItemFormGroupComponent - rendering', () => {

        render(<RepositoryStatusBadgePanel />);
        const textElement = screen.getByText(/Repositories/i);
        expect(textElement).toBeDefined();
    });

    /*test('WorkItemFormGroupComponent - click button', async() => {

        render(<WorkItemFormGroupComponent />);
        const linkElement = screen.getByText(/Click me to change title!/i);
        // Screen debug is helpful to see the current DOM tree while creating tests
        // screen.debug();
        
        // Get the text of the button and click that area
        const clickableButton = screen.getByText(/Click me to change title!/i);
        fireEvent.click(clickableButton);

        // Alternative all button objects in the screen 
        // const buttons = screen.getAllByRole('button');
        // Click 'Click me to change title!' button which is the first(only) one
        // fireEvent.click(buttons[0]);

        // await re-rendering to insure setFieldValue SDK method was called before expect
        await waitFor(() => screen.getAllByText('Click me to change title!'));

        expect(mockSetFieldValue.mock.calls[0]).toEqual([ 
            "System.Title",
            "Title set from your group extension!"
        ]
        );

    });

    test('WorkItemFormGroupComponent - onFieldChanged', async() => {

        render(<WorkItemFormGroupComponent />);
       
        await delay(1);

        // Trigger onFieldChanged event
        const args = {id: 42, changedFields: {"Some.Field" : "changedValue"}};
        spyWorkItemCallBackAccessor().onFieldChanged(args);

        await delay(1);

        expect(screen.getByText(/onFieldChanged/i).textContent).toEqual(`onFieldChanged - ${JSON.stringify(args)}`);

    });

    test('WorkItemFormGroupComponent - onLoaded', async() => {

        render(<WorkItemFormGroupComponent />);
       
        await delay(1);

        // Trigger onLoaded event
        const args = {id: 42, isNew: false, isReadOnly: false};
        spyWorkItemCallBackAccessor().onLoaded(args);

        await delay(1);

        expect(screen.getByText(/onLoaded/i).textContent).toEqual(`onLoaded - ${JSON.stringify(args)}`);

    });*/
});


/**
 * Helper Function to delay execution
 */
function delay(ms: number) {
    return new Promise( resolve => setTimeout(resolve, ms) );
}
