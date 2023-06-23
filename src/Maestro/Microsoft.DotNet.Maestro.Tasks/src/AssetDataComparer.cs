// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Maestro.Client.Models;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Maestro.Tasks
{
    /// <summary>
    ///     Compares asset data objects based on name and version.
    /// </summary>
    public class AssetDataComparer : IEqualityComparer<AssetData>
    {
        public bool Equals(AssetData x, AssetData y)
        {
            return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase) &&
                x.Version == y.Version;
        }

        public int GetHashCode(AssetData obj)
        {
            return (obj.Name, obj.Version).GetHashCode();
        }
    }
}
