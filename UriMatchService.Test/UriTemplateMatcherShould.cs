// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

/* This code was partially ported from: https://github.com/tavis-software/Tavis.UriTemplates;
 * From the following class: UriTemplateTableTests.cs
 * Some code refactoring, renaming and updates have been applied.
 */

using System;
using Xunit;

namespace UriMatchingService.Test
{
    public class UriTemplateMatcherShould
    {
        [Theory,
        InlineData("/", "root"),
        InlineData("/baz/fod/burg", ""),
        InlineData("/baz/kit", "kit"),
        InlineData("/games/monopoly/Setup/23", "gamessetup"),
        InlineData("http://www.example.com/baz/fod", "baz"),
        InlineData("/baz/fod/blob", "blob"),
        InlineData("/glah/flid/blob", "goo"),
        InlineData("/settings/12345", "set"),
        InlineData("/organization/54321/settings/iteminsights", "org"),
        InlineData("/games/monopoly/22/State/33", "state"),
        InlineData("/foo?x=1&y=2", "fooxy3")]
        public void MatchPathToUriTemplates(string uri, string key)
        {
            // Arrange
            var table = new UriTemplateMatcher();
            table.Add("root", "/");
            table.Add("foo", "/foo/{bar}");
            table.Add("kit", "/baz/kit");
            table.Add("fooxy3", "/foo?x={x}&y={y}");
            table.Add("baz", "/baz/{bar}");
            table.Add("blob", "/baz/{bar}/blob");
            table.Add("goo", "/{goo}/{bar}/blob");
            table.Add("set", "/settings/{id}");
            table.Add("state", "/games/{gametitle}/{gameid}/State/{stateid}");
            table.Add("org", "/organization/{id}/settings/iteminsights");
            table.Add("gamessetup", "/games/{gametitle}/Setup/{gamesid}");

            // Act
            var result = table.Match(new Uri(uri, UriKind.RelativeOrAbsolute));

            // Assert
            if (string.IsNullOrEmpty(key))
            {
                Assert.Null(result);
            }
            else
            {
                Assert.Equal(key, result?.Key);
            }

            Assert.NotNull(table["goo"]);
            Assert.Null(table["goo1"]);
        }

        [Fact]
        public void ThrowArgumentNullExceptionForEmptyOrNullKeyValueInAdd()
        {
            // Arrange
            var table = new UriTemplateMatcher();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => table.Add("", "/settings/{id}"));
            Assert.Throws<ArgumentNullException>(() => table.Add(null, "/settings/{id}"));
        }

        [Fact]
        public void ThrowArgumentNullExceptionForEmptyOrNullTemplateValueInAdd()
        {
            // Arrange
            var table = new UriTemplateMatcher();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => table.Add("set", ""));
            Assert.Throws<ArgumentNullException>(() => table.Add("set", null));
        }

        [Fact]
        public void ThrowArgumentNullExceptionForNullUriValueInMatch()
        {
            // Arrange
            var table = new UriTemplateMatcher();
            table.Add("goo", "/{goo}/{bar}/blob");
            table.Add("set", "/settings/{id}");
            table.Add("org", "/organization/{id}/settings/iteminsights");

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                table.Match(null));
        }

        [Fact]
        public void ThrowArgumentNullExceptionForEmptyOrNullKeyIndexerInTemplateTable()
        {
            // Arrange
            var table = new UriTemplateMatcher();

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => table[""]);
            Assert.Throws<ArgumentNullException>(() => table[null]);
        }
    }
}
