﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData.Edm;
using Microsoft.OpenApi.OData.Vocabulary.Capabilities;

namespace Microsoft.OpenApi.OData.PathItem
{
    /// <summary>
    /// Create a <see cref="OpenApiPathItem"/> for <see cref="IEdmSingleton"/>.
    /// </summary>
    internal class SingletonPathItemHandler : PathItemHandler
    {
        /// <inheritdoc/>
        protected override ODataPathKind HandleKind => ODataPathKind.Singleton;

        /// <summary>
        /// Gets the singleton.
        /// </summary>
        protected IEdmSingleton Singleton { get; private set; }

        /// <inheritdoc/>
        protected override void SetOperations(OpenApiPathItem item)
        {
            // Retrieve a singleton.
            ReadRestrictionsType read = Context.Model.GetRecord<ReadRestrictionsType>(Singleton);
            if (read == null || read.IsReadable)
            {
                AddOperation(item, OperationType.Get);
            }

            // Update a singleton
            UpdateRestrictionsType update = Context.Model.GetRecord<UpdateRestrictionsType>(Singleton);
            if (update == null || update.IsUpdatable)
            {
                AddOperation(item, OperationType.Patch);
            }
        }

        /// <inheritdoc/>
        protected override void Initialize(ODataContext context, ODataPath path)
        {
            base.Initialize(context, path);

            ODataNavigationSourceSegment navigationSourceSegment = path.FirstSegment as ODataNavigationSourceSegment;
            Singleton = navigationSourceSegment.NavigationSource as IEdmSingleton;
        }
    }
}
