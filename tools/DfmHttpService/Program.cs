// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var service = new DfmHttpService();
            var urlPrefix = DfmHttpService.GetAvailablePrefix();
            service.StartService(urlPrefix);
        }
    }
}
