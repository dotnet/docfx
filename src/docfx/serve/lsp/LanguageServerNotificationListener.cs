// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class LanguageServerNotificationListener : ILanguageServerNotificationListener
{
    public void OnNotificationHandled() { }

    public void OnNotificationSent() { }

    public void OnException(Exception ex) { }

    public void OnInitialized() { }
}
