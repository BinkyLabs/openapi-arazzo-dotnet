
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace BinkyLabs.OpenApi.Arazzo.Reader
{
    internal class PatternFieldMap<T> : Dictionary<Func<string, bool>, Action<T, string, ParseNode>>
    {
    }
}