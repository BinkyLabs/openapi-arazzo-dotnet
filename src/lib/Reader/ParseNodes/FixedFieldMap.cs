
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace BinkyLabs.OpenApi.Arazzo.Reader
{
    internal class FixedFieldMap<T> : Dictionary<string, Action<T, ParseNode>>
    {
    }
}