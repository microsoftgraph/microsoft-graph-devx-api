﻿using Newtonsoft.Json;
using System;
using Xunit;
using System.Collections.Generic;
using TourStepsService.Models;

namespace TourStepsService.Test
{
    public class TourStepsServiceShould
    {

        #region Serialize Tour Steps List Tests
        [Fact]
        public void SerializeListOfTourStepsIntoJsonString()
        {
            /* Arrange */

            // create list of two tour-steps
            var tourSteps = new List<TourStepsModel>()
            {
                new TourStepsModel()
                {
                    Target= ".sign-in-section",
                    Content= "When you're signed in, you can run all queries with all request options. We recommend that you sign in with a sample Azure AD account where you are the tenant admin.",
                    DirectionalHint= 10,
                    SpotlightClicks= true,
                    HideCloseButton= true,
                    AutoNext= false,
                    DisableBeacon= true,
                    Advanced= true,
                    Title= "Sign in",
                    ExpectedActionType= "PROFILE_REQUEST_SUCCESS",
                    Query = {
                        SelectedVerb = TourStepsModel.QueryObject.HttpMethods.GET,
                        SelectedVersion = "v1.0",
                        SampleUrl = "https://graph.microsoft.com/v1.0"
                    },
                    DocsLink= "",
                    Active= true
                },
                new TourStepsModel()
                {
                    Target= ".query-box",
                    Content= "You can type your Graph queries here. It has an autocomplete feature as well as links to the docs for some queries. Add a '/m' at the end then scroll and choose 'me' from the autocomplete suggestions. Click 'Run query' to run this query",
                    DirectionalHint= 10,
                    SpotlightClicks=true,
                    HideCloseButton= true,
                    Title= "Want to run a Graph API query?",
                    AutoNext= false,
                    DisableBeacon= true,
                    ExpectedActionType= "QUERY_GRAPH_SUCCESS",
                    Query = {
                        SelectedVerb = TourStepsModel.QueryObject.HttpMethods.GET,
                        SelectedVersion = "v1.0",
                        SampleUrl = "https://graph.microsoft.com/v1.0"
                    },
                    Advanced= true,
                    DocsLink="",
                    Active= true
                }
            };

            var tourStepsList = new TourStepsList()
            {
                TourSteps = tourSteps
            };

            // Act

            // Get serialized JSON string of the list of tour steps
            var newTourStepsJson = Services.TourStepsService.SerializetourStepsList(tourStepsList);

            // Assert
            Assert.NotNull(newTourStepsJson);
        }

        [Fact]
        public void SerializeTourStepsListIfTourStepsListParameterIsEmptyCollection()
        {
            // Arrange
            var emptyTourStepsList = new TourStepsList();

            // Act
            var tourStepsJson = Services.TourStepsService.SerializetourStepsList(emptyTourStepsList);

            // Assert
            Assert.NotNull(tourStepsJson);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfSerializeTourStepsListParameterisNull()
        {
            // Arrange
            TourStepsList tourStepsList = null;

            // Act and assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.TourStepsService.SerializetourStepsList(tourStepsList));
        }

        #endregion

        #region Deserialize Tour Steps List Tests

        [Fact]
        public void DeserializeValidJsonStringIntoListofTourStepsModelObjects()
        {
            // Arrange - tour steps list
            var validJsonString = @"{
                ""TourSteps"":
                [
                    {
                        ""target"": "".sign-in-section"",
                        ""content"": ""When you're signed in, you can run all queries with all request options. We recommend that you sign in with a sample Azure AD account where you are the tenant admin."",
                        ""directionalHint"": 10,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": true,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""Sign in"",
                        ""expectedActionType"": ""PROFILE_REQUEST_SUCCESS"",
                        ""docsLink"": """",
                        ""query"": {},
                        ""active"": true
                    },
                    {
                        ""target"": "".settings-menu-button"",
                        ""content"": ""An Azure AD sandbox gives you access to sample data that you can use to test all queries."",
                        ""directionalHint"": 1,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": false,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""More actions"",
                        ""expectedActionType"": """",
                        ""docsLink"": ""https://developer.microsoft.com/en-US/office/dev-program"",
                        ""query"": {},
                        ""active"": true
                    },
                    {
                        ""target"": "".request-option"",
                        ""content"": ""You can perform GET, POST, PUT, PATCH and DELETE requests on Microsoft Graph. We will perform a GET request to Microsoft Graph for profile information. Click the drop down menu and select GET"",
                        ""directionalHint"": 9,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": true,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""expectedActionType"": ""SET_SAMPLE_QUERY_SUCCESS"",
                        ""title"": ""HTTP request method option"",
                        ""advanced"": true,
                        ""docsLink"": """",
                        ""query"": {},
                        ""active"": true
                    },
                    {
                        ""target"": "".query-version"",
                        ""content"": ""Microsoft Graph beta endpoint has APIs that are in preview. v1.0 endpoint has APIs that are generally available. Choose v1.0 from the dropdown menu"",
                        ""docsLink"": """",
                        ""directionalHint"": 5,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": false,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""Microsoft Graph API Version option"",
                        ""expectedActionType"": ""SET_SAMPLE_QUERY_SUCCESS"",
                        ""docsLink"": """",
                        ""query"": {},
                        ""active"": true
                    }
                ]
            }";

            // Act
            var tourStepsList = Services.TourStepsService.DeserializeTourStepsList(validJsonString);

            // Assert that the tour steps list are returned as valid TourSteps objects

            Assert.True(tourStepsList.TourSteps.Count == 4);
        }

        [Fact]
        public void ThrowJsonReaderExceptionIfDeserializeTourStepsListJsonStringParameterIsInvalidJsonString()
        {
            var validJsonString = @"{
                ""TourSteps"":
                [
                    {
                        ""target"": "".sign-in-section"",
                        ""content"": ""When you're signed in, you can run all queries with all request options. We recommend that you sign in with a sample Azure AD account where you are the tenant admin."",
                        ""directionalHint"": 10,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": true,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""Sign in"",
                        ""expectedActionType"": ""PROFILE_REQUEST_SUCCESS"",
                        ""docsLink"": """",
                        ""query"": {},
                        ""active"": true

                    {
                        ""target"": "".settings-menu-button"",
                        ""content"": ""An Azure AD sandbox gives you access to sample data that you can use to test all queries."",
                        ""directionalHint"": 1,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": false,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""More actions"",
                        ""expectedActionType"": "",
                        ""docsLink"": ""https://developer.microsoft.com/en-US/office/dev-program"",
                        ""query"": {},
                        ""active"": true

                    {
                        ""target"": "".request-option"",
                        ""content"": ""You can perform GET, POST, PUT, PATCH and DELETE requests on Microsoft Graph. We will perform a GET request to Microsoft Graph for profile information. Click the drop down menu and select GET"",
                        ""docsLink"": """",
                        ""directionalHint"": 9,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": true,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""expectedActionType"": ""SET_SAMPLE_QUERY_SUCCESS"",
                        ""title"": ""HTTP request method option"",
                        ""advanced"": true,
                        ""query"": {},
                        ""active"": true

                    {
                        ""target"": "".query-version"",
                        ""content"": ""Microsoft Graph beta endpoint has APIs that are in preview. v1.0 endpoint has APIs that are generally available. Choose v1.0 from the dropdown menu"",
                        ""directionalHint"": 5,
                        ""spotlightClicks"": true,
                        ""hideCloseButton"": false,
                        ""autoNext"": false,
                        ""disableBeacon"": true,
                        ""advanced"": true,
                        ""title"": ""Microsoft Graph API Version option"",
                        ""expectedActionType"": ""SET_SAMPLE_QUERY_SUCCESS"",
                        ""docsLink"": """",
                        ""query"": {},
                        ""active"": true

                ]
            }";

            // Act and assert
            Assert.Throws<JsonReaderException>(() =>
                Services.TourStepsService.DeserializeTourStepsList(validJsonString));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfDeserializeTourStepsListJsonStringParameterIsNull()
        {
            // Arrange
            var nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.TourStepsService.DeserializeTourStepsList(nullArgument));
        }

        [Fact]
        public void ReturnEmptyCollectionWhenJsonFileIsEmptyInDeserializeTourStepsListJsonStringParameter()
        {
            // Arrange
            var emptyJsonFileContent = "{}";

            // Act
            var tourStepsList = Services.TourStepsService.DeserializeTourStepsList(emptyJsonFileContent);

            // Assert
            Assert.Empty(tourStepsList.TourSteps);
        }

        #endregion
    }
}
